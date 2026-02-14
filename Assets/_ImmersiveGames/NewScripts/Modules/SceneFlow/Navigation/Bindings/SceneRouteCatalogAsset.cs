using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    /// <summary>
    /// Catálogo configurável de rotas do SceneFlow (SceneRouteId -> SceneRouteDefinition).
    /// Suporta wiring por referência direta de assets de rota e fallback por entrada inline.
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

            [Tooltip("Cenas a carregar via SceneKeyAsset.")]
            public SceneKeyAsset[] scenesToLoadKeys;

            [Tooltip("Cenas a descarregar via SceneKeyAsset.")]
            public SceneKeyAsset[] scenesToUnloadKeys;

            [Tooltip("Cena ativa final via SceneKeyAsset.")]
            public SceneKeyAsset targetActiveSceneKey;

            [Tooltip("Metadado de política para decisões de lifecycle (ex.: reset de mundo por rota).")]
            public SceneRouteKind routeKind = SceneRouteKind.Unspecified;

            [Tooltip("Decisão explícita de reset de mundo para a rota (fonte de verdade em runtime).")]
            public bool requiresWorldReset;
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

        [Header("Routes (Direct References)")]
        [SerializeField] private List<SceneRouteDefinitionAsset> routeDefinitions = new();

        [Header("Routes (Inline Fallback)")]
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

            int invalidCount = 0;
            int viaAssetRefCount = 0;
            int viaInlineIdCount = 0;

            if (routeDefinitions != null)
            {
                for (int i = 0; i < routeDefinitions.Count; i++)
                {
                    var routeAsset = routeDefinitions[i];
                    if (!TryBuildFromAsset(routeAsset, out var routeId, out var routeDefinition))
                    {
                        invalidCount++;
                        continue;
                    }

                    if (_cache.ContainsKey(routeId))
                    {
                        invalidCount++;
                        if (warnOnInvalidRoutes)
                        {
                            DebugUtility.LogWarning<SceneRouteCatalogAsset>(
                                $"[SceneFlow] Rota duplicada no SceneRouteCatalog (asset ref). routeId='{routeId}'. Apenas a primeira será usada.");
                        }
                        continue;
                    }

                    viaAssetRefCount++;
                    _cache.Add(routeId, routeDefinition);

                    DebugUtility.LogVerbose<SceneRouteCatalogAsset>(
                        $"[OBS][Config] RouteResolvedVia=AssetRef routeId='{routeId}' asset='{routeAsset.name}'.",
                        DebugUtility.Colors.Info);
                }
            }

            if (routes != null)
            {
                foreach (var entry in routes)
                {
                    if (!TryBuildFromEntry(entry, out var routeId, out var routeDefinition))
                    {
                        invalidCount++;
                        continue;
                    }

                    if (_cache.ContainsKey(routeId))
                    {
                        invalidCount++;
                        if (warnOnInvalidRoutes)
                        {
                            DebugUtility.LogWarning<SceneRouteCatalogAsset>(
                                $"[SceneFlow] Rota duplicada no SceneRouteCatalog (inline). routeId='{routeId}'. Apenas a primeira será usada.");
                        }
                        continue;
                    }

                    viaInlineIdCount++;
                    _cache.Add(routeId, routeDefinition);

                    DebugUtility.LogVerbose<SceneRouteCatalogAsset>(
                        $"[OBS][Config] RouteResolvedVia=RouteId routeId='{routeId}'.",
                        DebugUtility.Colors.Info);
                }
            }

            if (warnOnInvalidRoutes && invalidCount > 0)
            {
                DebugUtility.LogWarning<SceneRouteCatalogAsset>(
                    $"[SceneFlow] SceneRouteCatalog possui entradas inválidas/duplicadas. invalidCount={invalidCount}.");
            }

            DebugUtility.LogVerbose<SceneRouteCatalogAsset>(
                "[OBS][Config] SceneRouteCatalogBuild " +
                $"routesResolved={_cache.Count} viaAssetRef={viaAssetRefCount} viaRouteId={viaInlineIdCount} invalidRoutes={invalidCount}",
                DebugUtility.Colors.Info);
        }

        private static bool TryBuildFromAsset(
            SceneRouteDefinitionAsset routeAsset,
            out SceneRouteId routeId,
            out SceneRouteDefinition routeDefinition)
        {
            routeId = SceneRouteId.None;
            routeDefinition = default;

            if (routeAsset == null)
            {
                return false;
            }

            routeId = routeAsset.RouteId;
            if (!routeId.IsValid)
            {
                return false;
            }

            routeDefinition = routeAsset.ToDefinition();
            return true;
        }

        private static bool TryBuildFromEntry(
            RouteEntry entry,
            out SceneRouteId routeId,
            out SceneRouteDefinition routeDefinition)
        {
            routeId = SceneRouteId.None;
            routeDefinition = default;

            if (entry == null || !entry.routeId.IsValid)
            {
                return false;
            }

            routeId = entry.routeId;

            var load = ResolveKeys(entry.scenesToLoadKeys, routeId, nameof(RouteEntry.scenesToLoadKeys));
            var unload = ResolveKeys(entry.scenesToUnloadKeys, routeId, nameof(RouteEntry.scenesToUnloadKeys));
            var active = ResolveSingleKey(entry.targetActiveSceneKey, routeId, nameof(RouteEntry.targetActiveSceneKey));

            routeDefinition = new SceneRouteDefinition(load, unload, active, entry.routeKind, entry.requiresWorldReset);
            return true;
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

        private static void FailFast(string message)
        {
            DebugUtility.LogError(typeof(SceneRouteCatalogAsset), $"[FATAL][Config] {message}");
            throw new InvalidOperationException(message);
        }
    }
}
