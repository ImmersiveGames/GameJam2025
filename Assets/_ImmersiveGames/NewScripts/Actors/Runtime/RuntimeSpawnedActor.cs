using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RuntimeSpawnedActor : Actor, IPoolableObject
    {
        public event Action<RuntimeSpawnedActor> PoolReturned;
        public event Action<RuntimeSpawnedActor> PoolDestroyed;

        private ActorId runtimeActorId;
        private ActorRole runtimeActorRole;
        private ActorScope runtimeActorScope;
        private ActorParticipationRecord.ActorParticipationPolicy runtimeParticipationPolicy;
        private RuntimeSpawnOriginMetadata runtimeSpawnOrigin;
        private Transform[] runtimeLayerBaselineTransforms = Array.Empty<Transform>();
        private int[] runtimeLayerBaselineValues = Array.Empty<int>();
        private bool runtimeLayerBaselineCaptured;

        public bool IsRuntimeMetadataBound =>
            runtimeActorId.IsValid &&
            RuntimeActorInstanceId.IsValid &&
            runtimeActorRole != ActorRole.Unknown &&
            runtimeActorScope != ActorScope.Unknown &&
            runtimeSpawnOrigin.IsValid;

        public override ActorId ActorIdValue => runtimeActorId;
        public override ActorRole ActorRoleMetadata => runtimeActorRole;
        public override ActorScope ActorScopeMetadata => runtimeActorScope;
        public override ActorParticipationRecord.ActorParticipationPolicy ActorParticipationPolicy => runtimeParticipationPolicy;
        public RuntimeSpawnOriginMetadata SpawnOrigin => runtimeSpawnOrigin;
        public bool HasSpawnOrigin => runtimeSpawnOrigin.IsValid;
        public ActorId OwnerActorId => runtimeSpawnOrigin.OwnerActorId;
        public ActorInstanceRuntimeId OwnerActorInstanceRuntimeId => runtimeSpawnOrigin.OwnerActorInstanceRuntimeId;
        public RuntimeSpawnProfileId SpawnProfileId => runtimeSpawnOrigin.SpawnProfileId;
        public PoolDefinitionAsset OriginPoolDefinition => runtimeSpawnOrigin.PoolDefinition;

        public bool TryApplyLayerBootstrap(
            ActorProjectileLayerBootstrap layerBootstrap,
            string source,
            string reason,
            out string failureReason,
            out string failureMessage)
        {
            failureReason = string.Empty;
            failureMessage = string.Empty;

            if (layerBootstrap.Mode == ActorProjectileSpawnLayerModeKind.None)
            {
                return true;
            }

            if (!layerBootstrap.IsValid)
            {
                failureReason = "projectile_spawn_layer_bootstrap_invalid";
                failureMessage = "RuntimeSpawnedActor requires a valid layer bootstrap when spawnLayerMode=Override.";
                return false;
            }

            CaptureLayerBaselineIfNeeded(source, reason);
            ApplyLayerToHierarchy(layerBootstrap.LayerIndex, layerBootstrap.ApplyLayerToChildren);

            DebugUtility.LogVerbose(
                typeof(RuntimeSpawnedActor),
                $"event='RuntimeSpawnedActorLayerOverrideApplied' actorId='{runtimeActorId}' actorInstanceRuntimeId='{RuntimeActorInstanceId}' layerMode='{layerBootstrap.Mode}' layerIndex='{layerBootstrap.LayerIndex}' layerName='{layerBootstrap.LayerName}' applyLayerToChildren='{layerBootstrap.ApplyLayerToChildren}' instanceName='{name}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            return true;
        }


        public void BindRuntimeMetadata(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorRole actorRole,
            ActorScope actorScope,
            ActorParticipationRecord.ActorParticipationPolicy participationPolicy,
            RuntimeSpawnOriginMetadata spawnOrigin,
            string source)
        {
            string origin = ResolveOrigin(source, nameof(RuntimeSpawnedActor), name);
            if (!actorId.IsValid)
            {
                throw new InvalidOperationException($"{origin} cannot bind invalid ActorId.");
            }

            if (!actorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException($"{origin} cannot bind invalid ActorInstanceRuntimeId.");
            }

            if (actorRole == ActorRole.Unknown)
            {
                throw new InvalidOperationException($"{origin} cannot bind unknown ActorRole.");
            }

            if (actorScope == ActorScope.Unknown)
            {
                throw new InvalidOperationException($"{origin} cannot bind unknown ActorScope.");
            }

            if (!spawnOrigin.IsValid)
            {
                throw new InvalidOperationException($"{origin} cannot bind invalid RuntimeSpawnOriginMetadata.");
            }

            runtimeActorId = actorId;
            runtimeActorRole = actorRole;
            runtimeActorScope = actorScope;
            runtimeParticipationPolicy = participationPolicy;
            runtimeSpawnOrigin = spawnOrigin;
            SetRuntimeActorInstanceId(actorInstanceRuntimeId);

            DebugUtility.LogVerbose(
                typeof(RuntimeSpawnedActor),
                $"event='RuntimeSpawnedActorMetadataBound' actorId='{runtimeActorId}' actorInstanceRuntimeId='{RuntimeActorInstanceId}' actorRole='{runtimeActorRole}' actorScope='{runtimeActorScope}' ownerActorId='{runtimeSpawnOrigin.OwnerActorId}' ownerActorInstanceRuntimeId='{runtimeSpawnOrigin.OwnerActorInstanceRuntimeId}' spawnProfileId='{runtimeSpawnOrigin.SpawnProfileId}' originPoolDefinition='{runtimeSpawnOrigin.PoolDefinitionName}' commandSequence='{runtimeSpawnOrigin.CommandSequence}' instanceName='{name}' activeSelf='{gameObject.activeSelf}' activeInHierarchy='{gameObject.activeInHierarchy}' source='{Normalize(source)}' reason='runtime_spawned_actor_metadata_bound'.",
                DebugUtility.Colors.Info);
        }

        public override void ValidateLocalConfigurationOrThrow(string source)
        {
            string origin = ResolveOrigin(source, nameof(RuntimeSpawnedActor), name);
            if (CapabilitySurface == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorCapabilitySurface.");
            }
        }

        public void OnPoolCreated()
        {
            ClearLayerState();
            ClearRuntimeMetadata();
        }

        public void OnPoolRent()
        {
            // O pool apenas aluga a instância. A identidade runtime é aplicada pelo adapter de spawn logo após o rent.
        }

        public void OnPoolReturn()
        {
            RestoreLayerBaseline("pool_return", log: true);
            RaisePoolLifecycleEvent(PoolReturned, "pool_return");
            ClearRuntimeMetadata();
        }

        public void OnPoolDestroyed()
        {
            RestoreLayerBaseline("pool_destroyed", log: true);
            RaisePoolLifecycleEvent(PoolDestroyed, "pool_destroyed");
            ClearRuntimeMetadata();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private void ClearRuntimeMetadata()
        {
            runtimeActorId = default;
            runtimeActorRole = ActorRole.Unknown;
            runtimeActorScope = ActorScope.Unknown;
            runtimeParticipationPolicy = ActorParticipationRecord.ActorParticipationPolicy.None;
            runtimeSpawnOrigin = default;
            SetRuntimeActorInstanceId(default);
        }

        private void CaptureLayerBaselineIfNeeded(string source, string reason)
        {
            if (runtimeLayerBaselineCaptured)
            {
                return;
            }

            Transform[] transforms = GetComponentsInChildren<Transform>(includeInactive: true);
            runtimeLayerBaselineTransforms = transforms ?? Array.Empty<Transform>();
            runtimeLayerBaselineValues = new int[runtimeLayerBaselineTransforms.Length];

            for (int index = 0; index < runtimeLayerBaselineTransforms.Length; index++)
            {
                Transform transform = runtimeLayerBaselineTransforms[index];
                runtimeLayerBaselineValues[index] = transform == null ? 0 : transform.gameObject.layer;
            }

            runtimeLayerBaselineCaptured = true;

            DebugUtility.LogVerbose(
                typeof(RuntimeSpawnedActor),
                $"event='RuntimeSpawnedActorLayerBaselineCaptured' actorId='{runtimeActorId}' actorInstanceRuntimeId='{RuntimeActorInstanceId}' baselineCount='{runtimeLayerBaselineTransforms.Length}' instanceName='{name}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        private void RestoreLayerBaseline(string reason, bool log)
        {
            if (!runtimeLayerBaselineCaptured || runtimeLayerBaselineTransforms == null || runtimeLayerBaselineValues == null)
            {
                ClearLayerState();
                return;
            }

            int restoreCount = Math.Min(runtimeLayerBaselineTransforms.Length, runtimeLayerBaselineValues.Length);
            for (int index = 0; index < restoreCount; index++)
            {
                Transform transform = runtimeLayerBaselineTransforms[index];
                if (transform == null)
                {
                    continue;
                }

                transform.gameObject.layer = runtimeLayerBaselineValues[index];
            }

            if (log)
            {
                DebugUtility.LogVerbose(
                    typeof(RuntimeSpawnedActor),
                    $"event='RuntimeSpawnedActorLayerBaselineRestored' actorId='{runtimeActorId}' actorInstanceRuntimeId='{RuntimeActorInstanceId}' baselineCount='{restoreCount}' instanceName='{name}' source='{nameof(RuntimeSpawnedActor)}' reason='{Normalize(reason)}'.",
                    DebugUtility.Colors.Info);
            }

            ClearLayerState();
        }

        private void ClearLayerState()
        {
            runtimeLayerBaselineTransforms = Array.Empty<Transform>();
            runtimeLayerBaselineValues = Array.Empty<int>();
            runtimeLayerBaselineCaptured = false;
        }

        private void ApplyLayerToHierarchy(int layerIndex, bool applyToChildren)
        {
            if (!applyToChildren)
            {
                gameObject.layer = layerIndex;
                return;
            }

            ApplyLayerRecursive(transform, layerIndex);
        }

        private static void ApplyLayerRecursive(Transform target, int layerIndex)
        {
            if (target == null)
            {
                return;
            }

            target.gameObject.layer = layerIndex;
            for (int index = 0; index < target.childCount; index++)
            {
                ApplyLayerRecursive(target.GetChild(index), layerIndex);
            }
        }

        private void RaisePoolLifecycleEvent(Action<RuntimeSpawnedActor> subscribers, string reason)
        {
            if (subscribers == null)
            {
                return;
            }

            Delegate[] invocationList = subscribers.GetInvocationList();
            for (int index = 0; index < invocationList.Length; index++)
            {
                if (invocationList[index] is not Action<RuntimeSpawnedActor> callback)
                {
                    continue;
                }

                try
                {
                    callback(this);
                }
                catch (Exception exception)
                {
                    DebugUtility.LogError(
                        typeof(RuntimeSpawnedActor),
                        $"event='RuntimeSpawnedActorPoolLifecycleCallbackFailed' actorId='{runtimeActorId}' actorInstanceRuntimeId='{RuntimeActorInstanceId}' instanceName='{name}' lifecycleReason='{Normalize(reason)}' message='{Normalize(exception.Message)}'.");
                }
            }
        }
    }
}
