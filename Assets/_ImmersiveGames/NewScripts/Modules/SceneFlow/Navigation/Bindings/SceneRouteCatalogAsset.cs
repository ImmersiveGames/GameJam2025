using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    /// <summary>
    /// Catalogo configuravel de rotas do SceneFlow (SceneRouteId -> SceneRouteDefinition).
    /// Opera somente com referencias diretas em routeDefinitions.
    /// OWNER: fonte de configuracao e cache de rotas do catalogo.
    /// NAO E OWNER: execucao da transicao e gates de runtime.
    /// PUBLISH/CONSUME: sem EventBus; consumido por SceneRouteCatalogResolver.
    /// Fases tocadas: RouteExecutionPlan (fonte de dados).
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneRouteCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Catalogs/SceneRouteCatalogAsset",
        order = 30)]
    public sealed partial class SceneRouteCatalogAsset : ScriptableObject, ISceneRouteCatalog
    {
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

        private void OnEnable()
        {
            _cacheBuilt = false;
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

            routeAsset.ValidateRoutePolicyOrFailFast();
            routeDefinition = routeAsset.ToDefinition();
            EnsureActiveScenePolicy(routeId, routeDefinition.RouteKind, routeDefinition.TargetActiveScene, "assetRef");
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

        private static void FailFast(string message)
        {
            DebugUtility.LogError(typeof(SceneRouteCatalogAsset), $"[FATAL][Config] {message}");
            throw new InvalidOperationException(message);
        }
    }
}




