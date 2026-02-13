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

            [Tooltip("LEGACY — migrar para scenesToLoadKeys.")]
            public string[] scenesToLoad;

            [Tooltip("LEGACY — migrar para scenesToUnloadKeys.")]
            public string[] scenesToUnload;

            [Tooltip("LEGACY — migrar para targetActiveSceneKey.")]
            public string targetActiveScene;

            [Tooltip("Cenas a carregar via SceneKeyAsset.")]
            public SceneKeyAsset[] scenesToLoadKeys;

            [Tooltip("Cenas a descarregar via SceneKeyAsset.")]
            public SceneKeyAsset[] scenesToUnloadKeys;

            [Tooltip("Cena ativa final via SceneKeyAsset.")]
            public SceneKeyAsset targetActiveSceneKey;

            [Tooltip("Metadado de política para decisões de lifecycle (ex.: reset de mundo por rota).")]
            public SceneRouteKind routeKind = SceneRouteKind.Unspecified;

            public override string ToString()
                => $"routeId='{routeId}', kind='{routeKind}', active='{targetActiveScene}', " +
                   $"load=[{FormatArray(scenesToLoad)}], unload=[{FormatArray(scenesToUnload)}]";

            private static string FormatArray(string[] arr)
                => arr == null ? "" : string.Join(", ", arr.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

#if UNITY_EDITOR
        public readonly struct DebugRouteItem
        {
            public DebugRouteItem(SceneRouteId routeId, SceneRouteDefinition routeDefinition)
            {
                RouteId = routeId;
                RouteDefinition = routeDefinition;
            }

            public SceneRouteId RouteId { get; }
            public SceneRouteDefinition RouteDefinition { get; }
        }
#endif

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

#if UNITY_EDITOR
        public IReadOnlyList<DebugRouteItem> DebugGetRoutesSnapshot()
        {
            EnsureCache();
            var snapshot = new List<DebugRouteItem>(_cache.Count);
            foreach (var pair in _cache)
            {
                snapshot.Add(new DebugRouteItem(pair.Key, pair.Value));
            }

            return snapshot;
        }
#endif

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
                DebugUtility.LogVerbose<SceneRouteCatalogAsset>(
                    "[OBS][Config] SceneRouteCatalogSceneDataVia=SceneKeyAsset routesTotal=0 routesUsingKeys=0 routesUsingLegacy=0 invalidRoutes=0",
                    DebugUtility.Colors.Info);
                return;
            }

            int invalidCount = 0;
            int routesUsingKeys = 0;
            int routesUsingLegacy = 0;

            foreach (var entry in routes)
            {
                if (entry == null || !entry.routeId.IsValid)
                {
                    invalidCount++;
                    continue;
                }

                if (HasLegacyData(entry))
                {
                    routesUsingLegacy++;
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

                if (HasKeys(entry))
                {
                    routesUsingKeys++;
                }

                _cache.Add(entry.routeId, BuildDefinition(entry));
            }

            if (warnOnInvalidRoutes && invalidCount > 0)
            {
                DebugUtility.LogWarning<SceneRouteCatalogAsset>(
                    $"[SceneFlow] SceneRouteCatalog possui entradas inválidas/duplicadas. invalidCount={invalidCount}.");
            }

            DebugUtility.LogVerbose<SceneRouteCatalogAsset>(
                "[OBS][Config] SceneRouteCatalogSceneDataVia=SceneKeyAsset " +
                $"routesTotal={routes.Count} routesUsingKeys={routesUsingKeys} routesUsingLegacy={routesUsingLegacy} invalidRoutes={invalidCount}",
                DebugUtility.Colors.Info);
        }

        private static SceneRouteDefinition BuildDefinition(RouteEntry entry)
        {
            if (entry == null)
            {
                return default;
            }

            if (HasKeys(entry))
            {
                var load = ResolveKeys(
                    entry.scenesToLoadKeys,
                    entry.routeId,
                    nameof(RouteEntry.scenesToLoadKeys));
                var unload = ResolveKeys(
                    entry.scenesToUnloadKeys,
                    entry.routeId,
                    nameof(RouteEntry.scenesToUnloadKeys));
                var active = ResolveSingleKey(
                    entry.targetActiveSceneKey,
                    entry.routeId,
                    nameof(RouteEntry.targetActiveSceneKey));

                if (HasLegacyData(entry))
                {
                    DebugUtility.LogWarning<SceneRouteCatalogAsset>(
                        $"[SceneFlow] routeId='{entry.routeId}': dados LEGACY ignorados porque *Keys estão configuradas.");
                }

                return new SceneRouteDefinition(load, unload, active, entry.routeKind);
            }

            if (HasLegacyData(entry))
            {
                if (entry.scenesToLoad != null && entry.scenesToLoad.Any(s => !string.IsNullOrWhiteSpace(s)))
                {
                    FailFast($"routeId='{entry.routeId}' usa LEGACY '{nameof(RouteEntry.scenesToLoad)}'. Migre para '{nameof(RouteEntry.scenesToLoadKeys)}'.");
                }

                if (entry.scenesToUnload != null && entry.scenesToUnload.Any(s => !string.IsNullOrWhiteSpace(s)))
                {
                    FailFast($"routeId='{entry.routeId}' usa LEGACY '{nameof(RouteEntry.scenesToUnload)}'. Migre para '{nameof(RouteEntry.scenesToUnloadKeys)}'.");
                }

                if (!string.IsNullOrWhiteSpace(entry.targetActiveScene))
                {
                    FailFast($"routeId='{entry.routeId}' usa LEGACY '{nameof(RouteEntry.targetActiveScene)}'. Migre para '{nameof(RouteEntry.targetActiveSceneKey)}'.");
                }
            }

            return new SceneRouteDefinition(Array.Empty<string>(), Array.Empty<string>(), string.Empty, entry.routeKind);
        }

        private static string[] ResolveKeys(SceneKeyAsset[] keys, SceneRouteId routeId, string fieldName)
        {
            if (keys == null || keys.Length == 0)
            {
                return Array.Empty<string>();
            }

            var resolved = new List<string>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (key == null)
                {
                    FailFast($"routeId='{routeId}' possui key null em '{fieldName}[{i}]'.");
                }

                if (string.IsNullOrWhiteSpace(key.SceneName))
                {
                    FailFast($"routeId='{routeId}' possui SceneKeyAsset inválido em '{fieldName}[{i}]' (SceneName vazio). asset='{key.name}'.");
                }

                resolved.Add(key.SceneName.Trim());
            }

            return resolved
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string ResolveSingleKey(SceneKeyAsset key, SceneRouteId routeId, string fieldName)
        {
            if (key == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(key.SceneName))
            {
                FailFast($"routeId='{routeId}' possui SceneKeyAsset inválido em '{fieldName}' (SceneName vazio). asset='{key.name}'.");
            }

            return key.SceneName.Trim();
        }

        private static bool HasKeys(RouteEntry entry)
        {
            return entry != null &&
                   (entry.targetActiveSceneKey != null ||
                    (entry.scenesToLoadKeys != null && entry.scenesToLoadKeys.Length > 0) ||
                    (entry.scenesToUnloadKeys != null && entry.scenesToUnloadKeys.Length > 0));
        }

        private static bool HasLegacyData(RouteEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            bool hasLoad = entry.scenesToLoad != null && entry.scenesToLoad.Any(s => !string.IsNullOrWhiteSpace(s));
            bool hasUnload = entry.scenesToUnload != null && entry.scenesToUnload.Any(s => !string.IsNullOrWhiteSpace(s));
            bool hasActive = !string.IsNullOrWhiteSpace(entry.targetActiveScene);
            return hasLoad || hasUnload || hasActive;
        }

        private static void FailFast(string message)
        {
            DebugUtility.LogError(typeof(SceneRouteCatalogAsset), $"[FATAL][Config] {message}");
            throw new InvalidOperationException(message);
        }
    }
}
