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
        [Header("Levels")]
        [SerializeField] private List<LevelDefinition> levels = new();

        [Header("Validation")]
        [Tooltip("Quando true, registra warning se houver níveis inválidos/duplicados.")]
        [SerializeField] private bool warnOnInvalidLevels = true;

        private readonly Dictionary<LevelId, LevelDefinition> _cache = new();
        private readonly Dictionary<SceneRouteId, LevelId> _routeToLevelCache = new();
        private bool _cacheBuilt;

        public bool TryResolve(LevelId levelId, out SceneRouteId routeId, out SceneTransitionPayload payload)
        {
            routeId = SceneRouteId.None;
            payload = SceneTransitionPayload.Empty;

            if (!levelId.IsValid)
            {
                return false;
            }

            EnsureCache();

            if (!_cache.TryGetValue(levelId, out var definition) || definition == null || !definition.IsValid)
            {
                return false;
            }

            routeId = definition.ResolveRouteId();
            if (!routeId.IsValid)
            {
                return false;
            }

            string via = definition.routeRef != null ? "AssetRef" : "RouteId";
            DebugUtility.LogVerbose<LevelCatalogAsset>(
                $"[OBS][Config] RouteResolvedVia={via} levelId='{levelId}', routeId='{routeId}'.",
                DebugUtility.Colors.Info);

            payload = definition.ToPayload();
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
        }

        private void OnValidate()
        {
            _cacheBuilt = false;
            EnsureCache();
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
                return;
            }

            int invalidCount = 0;
            foreach (var entry in levels)
            {
                if (entry == null || !entry.IsValid)
                {
                    invalidCount++;
                    continue;
                }

                if (_cache.ContainsKey(entry.levelId))
                {
                    invalidCount++;
                    if (warnOnInvalidLevels)
                    {
                        DebugUtility.LogWarning<LevelCatalogAsset>(
                            $"[LevelFlow] Level duplicado no catálogo. levelId='{entry.levelId}'. Apenas o primeiro será usado.");
                    }
                    continue;
                }

                _cache.Add(entry.levelId, entry);

                SceneRouteId resolvedRouteId = entry.ResolveRouteId();
                if (resolvedRouteId.IsValid && !_routeToLevelCache.ContainsKey(resolvedRouteId))
                {
                    _routeToLevelCache.Add(resolvedRouteId, entry.levelId);
                }
            }

            if (warnOnInvalidLevels && invalidCount > 0)
            {
                DebugUtility.LogWarning<LevelCatalogAsset>(
                    $"[LevelFlow] LevelCatalog possui entradas inválidas/duplicadas. invalidCount={invalidCount}.");
            }
        }
    }
}
