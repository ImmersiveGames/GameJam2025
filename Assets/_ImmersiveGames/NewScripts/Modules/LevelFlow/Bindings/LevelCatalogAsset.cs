using System;
using System.Collections.Generic;
using System.Linq;
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
        fileName = "LevelCatalog",
        menuName = "ImmersiveGames/NewScripts/LevelFlow/Level Catalog",
        order = 20)]
    public sealed class LevelCatalogAsset : ScriptableObject, ILevelFlowService
    {
        [Header("Levels")]
        [SerializeField] private List<LevelDefinition> levels = new();

        [Header("Validation")]
        [Tooltip("Quando true, registra warning se houver níveis inválidos/duplicados.")]
        [SerializeField] private bool warnOnInvalidLevels = true;

        private readonly Dictionary<LevelId, LevelDefinition> _cache = new();
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

            routeId = definition.routeId;
            payload = definition.ToPayload();
            return true;
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
            }

            if (warnOnInvalidLevels && invalidCount > 0)
            {
                DebugUtility.LogWarning<LevelCatalogAsset>(
                    $"[LevelFlow] LevelCatalog possui entradas inválidas/duplicadas. invalidCount={invalidCount}.");
            }
        }
    }
}
