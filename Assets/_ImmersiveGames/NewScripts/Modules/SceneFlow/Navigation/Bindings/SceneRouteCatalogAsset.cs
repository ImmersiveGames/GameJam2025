using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    /// <summary>
    /// Catálogo configurável de rotas do SceneFlow (SceneRouteId -> cenas).
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneRouteCatalog",
        menuName = "ImmersiveGames/NewScripts/SceneFlow/Scene Route Catalog",
        order = 15)]
    public sealed class SceneRouteCatalogAsset : ScriptableObject, ISceneRouteCatalog
    {
        [Serializable]
        public sealed class RouteEntry
        {
            [Tooltip("Id canônico da rota (SceneRouteId).")]
            public SceneRouteId routeId;

            [Tooltip("Cenas a carregar (por nome).")]
            public string[] scenesToLoad;

            [Tooltip("Cenas a descarregar (por nome).")]
            public string[] scenesToUnload;

            [Tooltip("Cena que deve ficar ativa ao final da transição.")]
            public string targetActiveScene;

            public override string ToString()
                => $"routeId='{routeId}', active='{targetActiveScene}', " +
                   $"load=[{FormatArray(scenesToLoad)}], unload=[{FormatArray(scenesToUnload)}]";

            private static string FormatArray(string[] arr)
                => arr == null ? "" : string.Join(", ", arr.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        [Header("Routes")]
        [SerializeField] private List<RouteEntry> routes = new();

        [Header("Validation")]
        [Tooltip("Quando true, registra warning se houver rotas inválidas/duplicadas.")]
        [SerializeField] private bool warnOnInvalidRoutes = true;

        private readonly Dictionary<SceneRouteId, SceneRouteDefinition> _cache = new();
        private bool _cacheBuilt;

        public bool TryGet(SceneRouteId routeId, out SceneRouteDefinition route)
        {
            route = default;

            if (!routeId.IsValid)
            {
                return false;
            }

            EnsureCache();
            return _cache.TryGetValue(routeId, out route);
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

            if (routes == null || routes.Count == 0)
            {
                return;
            }

            int invalidCount = 0;
            foreach (var entry in routes)
            {
                if (entry == null || !entry.routeId.IsValid)
                {
                    invalidCount++;
                    continue;
                }

                if (_cache.ContainsKey(entry.routeId))
                {
                    invalidCount++;
                    if (warnOnInvalidRoutes)
                    {
                        DebugUtility.LogWarning<SceneRouteCatalogAsset>(
                            $"[SceneFlow] Rota duplicada no SceneRouteCatalog. routeId='{entry.routeId}'. Apenas a primeira será usada.");
                    }
                    continue;
                }

                _cache.Add(entry.routeId, BuildDefinition(entry));
            }

            if (warnOnInvalidRoutes && invalidCount > 0)
            {
                DebugUtility.LogWarning<SceneRouteCatalogAsset>(
                    $"[SceneFlow] SceneRouteCatalog possui entradas inválidas/duplicadas. invalidCount={invalidCount}.");
            }
        }

        private static SceneRouteDefinition BuildDefinition(RouteEntry entry)
        {
            if (entry == null)
            {
                return default;
            }

            var load = Sanitize(entry.scenesToLoad);
            var unload = Sanitize(entry.scenesToUnload);
            var active = string.IsNullOrWhiteSpace(entry.targetActiveScene) ? string.Empty : entry.targetActiveScene.Trim();
            return new SceneRouteDefinition(load, unload, active);
        }

        private static string[] Sanitize(string[] scenes)
        {
            if (scenes == null || scenes.Length == 0)
            {
                return Array.Empty<string>();
            }

            return scenes
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }
}
