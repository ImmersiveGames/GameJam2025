using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Bindings
{
    /// <summary>
    /// Catálogo configurável de níveis (LevelId -> SceneRouteId + payload).
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/LevelFlow/Catalogs/LevelCatalogAsset",
        order = 30)]
    public sealed class LevelCatalogAsset : ScriptableObject, ILevelFlowService
    {
        private readonly struct LevelResolution
        {
            public LevelResolution(LevelDefinition definition, SceneRouteId routeId, SceneTransitionPayload payload)
            {
                Definition = definition;
                RouteId = routeId;
                Payload = payload;
            }

            public LevelDefinition Definition { get; }
            public SceneRouteId RouteId { get; }
            public SceneTransitionPayload Payload { get; }
        }

        [Header("Levels")]
        [SerializeField] private List<LevelDefinition> levels = new();

        [Header("Validation")]
        [Tooltip("Quando true, registra warning se houver níveis inválidos/duplicados.")]
        [SerializeField] private bool warnOnInvalidLevels = true;

        private readonly Dictionary<LevelId, LevelResolution> _cache = new();
        private readonly Dictionary<SceneRouteId, LevelId> _routeToLevelCache = new();
        private readonly HashSet<LevelId> _loggedLevelsThisFrame = new();
        private bool _cacheBuilt;
        private int _lastLoggedFrame = int.MinValue;

        public bool TryResolve(LevelId levelId, out SceneRouteId routeId, out SceneTransitionPayload payload)
        {
            routeId = SceneRouteId.None;
            payload = SceneTransitionPayload.Empty;

            if (!levelId.IsValid)
            {
                return false;
            }

            EnsureCache();

            if (!_cache.TryGetValue(levelId, out var resolution))
            {
                return false;
            }

            routeId = resolution.RouteId;
            payload = resolution.Payload;
            LogResolutionDedupePerFrame(levelId, resolution);
            return true;
        }

        public bool TryResolveLevelId(SceneRouteId routeId, out LevelId levelId)
        {
            levelId = LevelId.None;

            if (!routeId.IsValid)
            {
                return false;
            }

            EnsureCache();
            return _routeToLevelCache.TryGetValue(routeId, out levelId) && levelId.IsValid;
        }

        private void OnEnable()
        {
            _cacheBuilt = false;
            _loggedLevelsThisFrame.Clear();
            _lastLoggedFrame = int.MinValue;
        }

        private void OnValidate()
        {
            _cacheBuilt = false;

            try
            {
                EnsureCache();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(LevelCatalogAsset),
                    $"[FATAL][Config] LevelCatalogAsset inválido durante OnValidate. detail='{ex.Message}'.");
            }
        }

        private void EnsureCache()
        {
            if (_cacheBuilt)
            {
                return;
            }

            _cacheBuilt = true;
            _cache.Clear();
            _routeToLevelCache.Clear();

            if (levels == null || levels.Count == 0)
            {
                FailFast("LevelCatalog sem níveis configurados.");
            }

            for (int i = 0; i < levels.Count; i++)
            {
                var entry = levels[i];
                if (entry == null)
                {
                    FailFast($"LevelDefinition nulo em levels[{i}].");
                }

                if (!entry.levelId.IsValid)
                {
                    FailFast($"LevelDefinition inválido em levels[{i}] (levelId vazio/inválido).");
                }

                if (_cache.ContainsKey(entry.levelId))
                {
                    FailFast($"LevelId duplicado no LevelCatalog. levelId='{entry.levelId}', index={i}.");
                }

                SceneRouteId resolvedRouteId = entry.ResolveRouteId();
                if (!resolvedRouteId.IsValid)
                {
                    FailFast($"Rota inválida ao resolver levelId='{entry.levelId}' em levels[{i}].");
                }

                if (_routeToLevelCache.TryGetValue(resolvedRouteId, out var existingLevelId) && existingLevelId != entry.levelId)
                {
                    HandleDuplicateRouteMappingConfigError(resolvedRouteId, existingLevelId, entry.levelId);
                }

                if (!_routeToLevelCache.ContainsKey(resolvedRouteId))
                {
                    _routeToLevelCache.Add(resolvedRouteId, entry.levelId);
                }

                _cache.Add(entry.levelId, new LevelResolution(entry, resolvedRouteId, entry.ToPayload()));
            }

            if (warnOnInvalidLevels)
            {
                DebugUtility.LogVerbose<LevelCatalogAsset>(
                    $"[OBS][Config] LevelCatalogBuild levelsResolved={_cache.Count} routesMapped={_routeToLevelCache.Count} invalidLevels=0",
                    DebugUtility.Colors.Info);
            }
        }

        private void LogResolutionDedupePerFrame(LevelId levelId, LevelResolution resolution)
        {
            int currentFrame = Time.frameCount;
            if (currentFrame != _lastLoggedFrame)
            {
                _loggedLevelsThisFrame.Clear();
                _lastLoggedFrame = currentFrame;
            }

            if (!_loggedLevelsThisFrame.Add(levelId))
            {
                return;
            }

            LevelDefinition definition = resolution.Definition;
            if (definition.routeRef != null)
            {
                DebugUtility.LogVerbose<LevelCatalogAsset>(
                    $"[OBS][SceneFlow] RouteResolvedVia=AssetRef levelId='{levelId}' routeId='{resolution.RouteId}' asset='{definition.routeRef.name}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.LogVerbose<LevelCatalogAsset>(
                $"[OBS][SceneFlow] RouteResolvedVia=RouteId levelId='{levelId}' routeId='{resolution.RouteId}'.",
                DebugUtility.Colors.Info);
        }

        private static void HandleDuplicateRouteMappingConfigError(SceneRouteId routeId, LevelId firstLevelId, LevelId duplicatedLevelId)
        {
            FailFast(
                $"RouteId duplicado mapeado para múltiplos LevelId no LevelCatalog. routeId='{routeId}', firstLevelId='{firstLevelId}', duplicatedLevelId='{duplicatedLevelId}'.");
        }

        private static void FailFast(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError(typeof(LevelCatalogAsset), message);
            throw new InvalidOperationException(message);
        }
    }
}
