using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    /// <summary>
    /// Catalogo configuravel de rotas do SceneFlow (SceneRouteId -> SceneRouteDefinition).
    /// Opera somente com referencias diretas em routeDefinitions.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneRouteCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Catalogs/SceneRouteCatalogAsset",
        order = 30)]
    public sealed class SceneRouteCatalogAsset : ScriptableObject, ISceneRouteCatalog
    {
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

        [Header("Validation")]
        [Tooltip("Quando true, registra warning se houver rotas invalidas/duplicadas.")]
        [SerializeField] private bool warnOnInvalidRoutes = true;

        private readonly Dictionary<SceneRouteId, SceneRouteDefinition> _cache = new();
        private readonly Dictionary<SceneRouteId, SceneRouteDefinitionAsset> _assetCache = new();
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

        public bool TryGetAsset(SceneRouteId routeId, out SceneRouteDefinitionAsset routeAsset)
        {
            routeAsset = null;

            if (!routeId.IsValid)
            {
                return false;
            }

            EnsureCache();
            return _assetCache.TryGetValue(routeId, out routeAsset) && routeAsset != null;
        }

#if UNITY_EDITOR
        public IReadOnlyList<DebugRouteItem> DebugGetRoutesSnapshot()
        {
            EnsureCache();
            List<DebugRouteItem> snapshot = new List<DebugRouteItem>(_cache.Count);
            foreach (KeyValuePair<SceneRouteId, SceneRouteDefinition> pair in _cache)
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

            try
            {
                EnsureCache();
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                string assetPath = AssetDatabase.GetAssetPath(this);
#else
                string assetPath = name;
#endif
                DebugUtility.LogError(typeof(SceneRouteCatalogAsset),
                    $"[FATAL][Config] SceneRouteCatalogAsset invalido durante OnValidate. asset='{assetPath}', detail='{ex.Message}'.");
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
            _assetCache.Clear();

            int viaAssetRefCount = 0;

            if (routeDefinitions != null)
            {
                for (int i = 0; i < routeDefinitions.Count; i++)
                {
                    SceneRouteDefinitionAsset routeAsset = routeDefinitions[i];
                    BuildFromAsset(routeAsset, out SceneRouteId routeId, out SceneRouteDefinition routeDefinition, i);

                    if (_cache.ContainsKey(routeId))
                    {
                        FailFast($"Rota duplicada no SceneRouteCatalog (asset ref). routeId='{routeId}', index={i}.");
                    }

                    viaAssetRefCount++;
                    _cache.Add(routeId, routeDefinition);
                    _assetCache.Add(routeId, routeAsset);

                    DebugUtility.LogVerbose<SceneRouteCatalogAsset>(
                        $"[OBS][Config] RouteResolvedVia=AssetRef routeId='{routeId}' asset='{routeAsset.name}'.",
                        DebugUtility.Colors.Info);
                }
            }

            if (warnOnInvalidRoutes && _cache.Count == 0)
            {
                DebugUtility.LogWarning<SceneRouteCatalogAsset>(
                    "[SceneFlow] SceneRouteCatalog nao contem rotas validas.");
            }

            DebugUtility.LogVerbose<SceneRouteCatalogAsset>(
                "[OBS][Config] SceneRouteCatalogBuild " +
                $"routesResolved={_cache.Count} viaAssetRef={viaAssetRefCount} viaRouteId=0 invalidRoutes=0",
                DebugUtility.Colors.Info);
        }

        private static void BuildFromAsset(
            SceneRouteDefinitionAsset routeAsset,
            out SceneRouteId routeId,
            out SceneRouteDefinition routeDefinition,
            int index)
        {
            routeId = SceneRouteId.None;
            routeDefinition = default;

            if (routeAsset == null)
            {
                FailFast($"RouteDefinitionAsset nulo em routeDefinitions[{index}].");
            }

            routeId = routeAsset.RouteId;
            if (!routeId.IsValid)
            {
                FailFast($"RouteDefinitionAsset invalido em routeDefinitions[{index}] (routeId vazio). asset='{routeAsset.name}'.");
            }

            routeDefinition = routeAsset.ToDefinition();
            EnsureActiveScenePolicy(routeId, routeDefinition.RouteKind, routeDefinition.TargetActiveScene, "assetRef");
            EnsureResetPolicyConsistency(routeId, routeDefinition.RouteKind, routeDefinition.RequiresWorldReset, "assetRef");
        }

        private static void EnsureActiveScenePolicy(SceneRouteId routeId, SceneRouteKind routeKind, string activeScene, string source)
        {
            if (string.IsNullOrWhiteSpace(activeScene) && RequiresActiveScene(routeKind))
            {
                FailFast(
                    $"routeId='{routeId}' resolvida via {source} requer TargetActiveScene para routeKind='{routeKind}', " +
                    "mas 'targetActiveSceneKey' esta ausente/nulo.");
            }
        }

        private static bool RequiresActiveScene(SceneRouteKind routeKind)
        {
            // Comentario: rotas de gameplay devem sempre definir cena ativa alvo.
            return routeKind == SceneRouteKind.Gameplay;
        }

        private static void EnsureResetPolicyConsistency(SceneRouteId routeId, SceneRouteKind routeKind, bool requiresWorldReset, string source)
        {
            if (routeKind == SceneRouteKind.Unspecified)
            {
                FailFast($"routeId='{routeId}' resolvida via {source} possui RouteKind='{SceneRouteKind.Unspecified}' (invalido para policy de reset).");
            }

            if (routeKind == SceneRouteKind.Gameplay && !requiresWorldReset)
            {
                FailFast($"routeId='{routeId}' resolvida via {source} exige requiresWorldReset=true para RouteKind='{SceneRouteKind.Gameplay}'.");
            }

            if (routeKind == SceneRouteKind.Frontend && requiresWorldReset)
            {
                FailFast($"routeId='{routeId}' resolvida via {source} exige requiresWorldReset=false para RouteKind='{SceneRouteKind.Frontend}'.");
            }
        }

        private static void FailFast(string message)
        {
            DebugUtility.LogError(typeof(SceneRouteCatalogAsset), $"[FATAL][Config] {message}");
            throw new InvalidOperationException(message);
        }
    }
}

