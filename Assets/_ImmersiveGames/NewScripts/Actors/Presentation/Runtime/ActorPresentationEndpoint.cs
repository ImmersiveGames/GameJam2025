using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Runtime
{
    /// <summary>
    /// Endpoint local de ActorPresentation no root lógico do Actor.
    /// Expõe containers explícitos para stages/adapters futuros.
    /// Não decide lifecycle, não materializa, não reseta e não libera presentation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActorPresentationEndpoint : MonoBehaviour, IPoolableSpawnOriginSurface
    {
        [SerializeField] private string endpointId = "actor.presentation.endpoint";
        [SerializeField] private ActorPresentationProfileAsset profile;
        [SerializeField] private List<ActorPresentationContainer> containers = new List<ActorPresentationContainer>();

        private readonly Dictionary<string, PoolableSpawnOriginAnchor> _poolableSpawnOriginAnchorsById = new(StringComparer.Ordinal);

        public string EndpointId => endpointId.TrimToEmpty();
        public ActorPresentationProfileAsset Profile => profile;
        public IReadOnlyList<ActorPresentationContainer> Containers => (IReadOnlyList<ActorPresentationContainer>)containers ?? Array.Empty<ActorPresentationContainer>();
        public bool HasPoolableSpawnOriginSurface => _poolableSpawnOriginAnchorsById.Count > 0;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(EndpointId) &&
            containers != null;

        public bool TryGetContainer(
            ActorPresentationSlotKind slotKind,
            string slotId,
            out ActorPresentationContainer container)
        {
            container = null;

            if (slotKind == ActorPresentationSlotKind.Unknown)
            {
                return false;
            }

            string normalizedSlotId = slotId.TrimToEmpty();
            if (string.IsNullOrWhiteSpace(normalizedSlotId) || containers == null)
            {
                return false;
            }

            for (int index = 0; index < containers.Count; index++)
            {
                var current = containers[index];
                if (current == null)
                {
                    continue;
                }

                if (current.SlotKind == slotKind && string.Equals(current.SlotId, normalizedSlotId, StringComparison.Ordinal))
                {
                    container = current;
                    return true;
                }
            }

            return false;
        }

        public bool TryResolve(PoolableSpawnOriginId originId, out PoolableSpawnOriginResolved resolved)
        {
            return TryResolve(originId, PoolableSpawnOriginResolutionMode.RequireTypedOrigin, out resolved);
        }

        public bool TryResolve(
            PoolableSpawnOriginId originId,
            PoolableSpawnOriginResolutionMode resolutionMode,
            out PoolableSpawnOriginResolved resolved)
        {
            resolved = default;

            if (!originId.IsValid)
            {
                LogPoolableSpawnOriginMissing(
                    originId,
                    resolutionMode,
                    usedFallback: false,
                    reason: "poolable_spawn_origin_id_missing",
                    message: $"{nameof(ActorPresentationEndpoint)} requires non-empty originId.");
                return false;
            }

            if (_poolableSpawnOriginAnchorsById.TryGetValue(originId.Value, out var anchor) && anchor != null && anchor.IsValid)
            {
                resolved = BuildResolved(
                    originId,
                    anchor.OriginKind,
                    anchor.OriginTransform,
                    resolutionMode,
                    usedFallback: false,
                    reason: "poolable_spawn_origin_resolved");
                LogPoolableSpawnOriginResolved(resolved);
                return true;
            }

            if (resolutionMode == PoolableSpawnOriginResolutionMode.RequireTypedOrigin)
            {
                LogPoolableSpawnOriginMissing(
                    originId,
                    resolutionMode,
                    usedFallback: false,
                    reason: "poolable_spawn_origin_missing",
                    message: $"Typed origin '{originId}' was not found on {nameof(ActorPresentationEndpoint)}.");
                return false;
            }

            if (!TryGetVisualRootFallback(out var fallbackTransform, out var fallbackSource))
            {
                LogPoolableSpawnOriginMissing(
                    originId,
                    resolutionMode,
                    usedFallback: false,
                    reason: "poolable_spawn_origin_fallback_missing",
                    message: $"Typed origin '{originId}' was not found and visual-root fallback is unavailable.");
                return false;
            }

            LogPoolableSpawnOriginMissing(
                originId,
                resolutionMode,
                usedFallback: true,
                reason: "poolable_spawn_origin_typed_missing",
                message: $"Typed origin '{originId}' was not found; visual-root fallback will be applied.");

            resolved = BuildResolved(
                originId,
                PoolableSpawnOriginKind.EmitterRoot,
                fallbackTransform,
                resolutionMode,
                usedFallback: true,
                reason: "poolable_spawn_origin_fallback_applied");

            LogPoolableSpawnOriginFallbackApplied(resolved, fallbackSource);
            LogPoolableSpawnOriginResolved(resolved);
            return true;
        }

        public void RebuildPoolableSpawnOriginSurface(string source)
        {
            string origin = string.IsNullOrWhiteSpace(source)
                ? $"{nameof(ActorPresentationEndpoint)}:{name}"
                : source.Trim();

            _poolableSpawnOriginAnchorsById.Clear();

            PoolableSpawnOriginAnchor[] anchors = GetComponentsInChildren<PoolableSpawnOriginAnchor>(includeInactive: true);
            int anchorCount = 0;

            if (anchors != null)
            {
                for (int index = 0; index < anchors.Length; index++)
                {
                    var anchor = anchors[index];
                    if (anchor == null)
                    {
                        continue;
                    }

                    if (!anchor.IsValid)
                    {
                        LogPoolableSpawnOriginMissing(
                            default,
                            PoolableSpawnOriginResolutionMode.RequireTypedOrigin,
                            usedFallback: false,
                            reason: "poolable_spawn_origin_anchor_invalid",
                            message: $"{origin} found invalid PoolableSpawnOriginAnchor at index '{index}'.");
                        continue;
                    }

                    string key = anchor.OriginId.Value;
                    if (_poolableSpawnOriginAnchorsById.ContainsKey(key))
                    {
                        throw new InvalidOperationException($"{origin} found duplicate PoolableSpawnOriginAnchor originId='{key}'.");
                    }

                    _poolableSpawnOriginAnchorsById.Add(key, anchor);
                    anchorCount++;

                    DebugUtility.Log(
                        typeof(ActorPresentationEndpoint),
                        $"event='PoolableSpawnOriginAnchorRegistered' surfaceOwner='{nameof(ActorPresentationEndpoint)}' originId='{key}' originKind='{anchor.OriginKind}' originTransformName='{anchor.OriginTransform.name}' anchorName='{anchor.name}' source='{origin}' reason='poolable_spawn_origin_anchor_registered'.",
                        DebugUtility.Colors.Info);
                }
            }

            DebugUtility.Log(
                typeof(ActorPresentationEndpoint),
                $"event='PoolableSpawnOriginSurfaceBuilt' surfaceOwner='{nameof(ActorPresentationEndpoint)}' anchorCount='{anchorCount}' hasSurface='{HasPoolableSpawnOriginSurface}' source='{origin}' reason='{(anchorCount == 0 ? "poolable_spawn_origin_surface_built_optional_no_anchors" : "poolable_spawn_origin_surface_built")}'.",
                anchorCount == 0 ? DebugUtility.Colors.Info : DebugUtility.Colors.Success);
        }

        public void ValidateOrThrow(string source)
        {
            string origin = string.IsNullOrWhiteSpace(source)
                ? $"{nameof(ActorPresentationEndpoint)}:{name}"
                : source.Trim();

            if (string.IsNullOrWhiteSpace(EndpointId))
            {
                throw new InvalidOperationException($"{origin} requires non-empty endpointId.");
            }

            if (profile == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorPresentationProfileAsset.");
            }

            if (containers == null)
            {
                throw new InvalidOperationException($"{origin} requires containers list.");
            }

            var observedKeys = new HashSet<string>(StringComparer.Ordinal);
            var observedOriginIds = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < containers.Count; index++)
            {
                var container = containers[index];
                if (container == null)
                {
                    throw new InvalidOperationException($"{origin} has null container at index '{index}'.");
                }

                container.ValidateOrThrow($"{origin}/container[{index}]");

                string key = BuildKey(container.SlotKind, container.SlotId);
                if (!observedKeys.Add(key))
                {
                    throw new InvalidOperationException($"{origin} has duplicate container key '{key}'.");
                }
            }

            PoolableSpawnOriginAnchor[] anchors = GetComponentsInChildren<PoolableSpawnOriginAnchor>(includeInactive: true);
            if (anchors == null)
            {
                return;
            }

            for (int index = 0; index < anchors.Length; index++)
            {
                var anchor = anchors[index];
                if (anchor == null)
                {
                    continue;
                }

                if (!anchor.IsValid)
                {
                    throw new InvalidOperationException($"{origin} has invalid PoolableSpawnOriginAnchor at index '{index}'.");
                }

                if (!observedOriginIds.Add(anchor.OriginId.Value))
                {
                    throw new InvalidOperationException($"{origin} has duplicate PoolableSpawnOriginAnchor originId '{anchor.OriginId.Value}'.");
                }
            }
        }

        private void Awake()
        {
            containers ??= new List<ActorPresentationContainer>();
        }

        private void OnValidate()
        {
            endpointId = endpointId.TrimToEmpty();
            containers ??= new List<ActorPresentationContainer>();
        }

        private bool TryGetVisualRootFallback(out Transform fallbackTransform, out string fallbackSource)
        {
            fallbackTransform = null;
            fallbackSource = string.Empty;

            if (TryGetContainer(ActorPresentationSlotKind.VisualRoot, "visual.root", out var visualRootContainer) &&
                visualRootContainer != null &&
                visualRootContainer.HasContainerTransform)
            {
                fallbackTransform = visualRootContainer.ContainerTransform;
                fallbackSource = "visual_root_container";
                return true;
            }

            return false;
        }

        private PoolableSpawnOriginResolved BuildResolved(
            PoolableSpawnOriginId originId,
            PoolableSpawnOriginKind originKind,
            Transform originTransform,
            PoolableSpawnOriginResolutionMode resolutionMode,
            bool usedFallback,
            string reason)
        {
            return new PoolableSpawnOriginResolved(
                originId,
                originKind,
                originTransform,
                nameof(ActorPresentationEndpoint),
                resolutionMode,
                usedFallback,
                nameof(ActorPresentationEndpoint),
                reason);
        }

        private void LogPoolableSpawnOriginResolved(PoolableSpawnOriginResolved resolved)
        {
            DebugUtility.Log(
                typeof(ActorPresentationEndpoint),
                $"event='PoolableSpawnOriginResolved' surfaceOwner='{resolved.SurfaceOwner}' originId='{resolved.OriginId}' originKind='{resolved.OriginKind}' position='{FormatVector(resolved.Position)}' direction='{FormatVector(resolved.Direction)}' usedFallback='{resolved.UsedFallback}' source='{resolved.Source}' reason='{resolved.Reason}'.",
                DebugUtility.Colors.Success);
        }

        private void LogPoolableSpawnOriginFallbackApplied(PoolableSpawnOriginResolved resolved, string fallbackSource)
        {
            DebugUtility.Log(
                typeof(ActorPresentationEndpoint),
                $"event='PoolableSpawnOriginFallbackApplied' surfaceOwner='{resolved.SurfaceOwner}' originId='{resolved.OriginId}' originKind='{resolved.OriginKind}' fallbackSource='{fallbackSource.TrimToEmpty()}' usedFallback='{resolved.UsedFallback}' source='{resolved.Source}' reason='{resolved.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private void LogPoolableSpawnOriginMissing(
            PoolableSpawnOriginId originId,
            PoolableSpawnOriginResolutionMode resolutionMode,
            bool usedFallback,
            string reason,
            string message)
        {
            DebugUtility.LogWarning(
                typeof(ActorPresentationEndpoint),
                $"event='PoolableSpawnOriginMissing' surfaceOwner='{nameof(ActorPresentationEndpoint)}' originId='{originId.Value.TrimToEmpty()}' resolutionMode='{resolutionMode}' usedFallback='{usedFallback}' source='{nameof(ActorPresentationEndpoint)}' reason='{reason.TrimToEmpty()}' message='{message.TrimToEmpty()}'.");
        }

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }

        private static string BuildKey(ActorPresentationSlotKind slotKind, string slotId)
        {
            return $"{slotKind}:{slotId.TrimToEmpty()}";
        }
}
}
