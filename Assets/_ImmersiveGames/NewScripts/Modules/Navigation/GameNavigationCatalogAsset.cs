using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine.Serialization;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Catálogo de intents de navegação (produção).
    ///
    /// F3 (Route como fonte única de Scene Data):
    /// - Este catálogo NÃO define ScenesToLoad/Unload/Active.
    /// - A Scene Data é resolvida via SceneRouteCatalogAsset (SceneFlow.Navigation) pelo GameNavigationService.
    ///
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameNavigationCatalog",
        menuName = "ImmersiveGames/NewScripts/Navigation/GameNavigationCatalog",
        order = 0)]
    public sealed class GameNavigationCatalogAsset : ScriptableObject, IGameNavigationCatalog, ISerializationCallbackReceiver
    {
        [Serializable]
        public sealed class RouteEntry
        {
            [Tooltip("Identificador canônico do intent de navegação (ex.: 'nav.menu', 'nav.gameplay').")]
            public string routeId;

            [Tooltip("SceneRouteId que fornece a Scene Data (ScenesToLoad/Unload/Active) para esta navegação.")]
            public SceneRouteId sceneRouteId;

            [FormerlySerializedAs("transitionStyleId")]
            [Tooltip("TransitionStyleId que define ProfileId/UseFade (SceneFlow) usado nesta navegação.")]
            public TransitionStyleId styleId;

            public void MigrateLegacy()
            {
                routeId = routeId?.Trim();
            }
        }

        [SerializeField]
        [FormerlySerializedAs("_routes")]
        private List<RouteEntry> routes = new();

        private readonly Dictionary<string, GameNavigationEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
        private bool _built;


        public IReadOnlyCollection<string> RouteIds
        {
            get
            {
                EnsureBuilt();
                return _cache.Keys;
            }
        }

        public bool TryGet(string routeId, out GameNavigationEntry entry)
        {
            if (string.IsNullOrWhiteSpace(routeId))
            {
                entry = default;
                return false;
            }

            EnsureBuilt();

            if (!_cache.TryGetValue(routeId.Trim(), out entry))
                return false;

            return true;
        }

        public void GetObservabilitySnapshot(out int rawRoutesCount, out int builtRouteIdsCount, out bool hasToGameplay)
        {
            EnsureBuilt();
            rawRoutesCount = routes?.Count ?? 0;
            builtRouteIdsCount = _cache.Count;
            hasToGameplay = _cache.ContainsKey(GameNavigationIntents.ToGameplay);
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            ApplyRouteMigration();
            _built = false;
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;

            ApplyRouteMigration();

            _built = true;
            _cache.Clear();

            if (routes == null)
                return;

            foreach (var route in routes)
            {
                if (!TryBuildEntry(route, out var entry))
                    continue;

                _cache[route.routeId.Trim()] = entry;
            }
        }

        private void ApplyRouteMigration()
        {
            if (routes == null)
                return;

            foreach (var route in routes)
            {
                route?.MigrateLegacy();
            }
        }

        private static bool TryBuildEntry(RouteEntry route, out GameNavigationEntry entry)
        {
            entry = default;

            if (route == null)
                return false;

            if (string.IsNullOrWhiteSpace(route.routeId))
                return false;

            if (!route.sceneRouteId.IsValid)
                return false;

            if (!route.styleId.IsValid)
                return false;

            var payload = SceneTransitionPayload.Empty;

            entry = new GameNavigationEntry(
                route.sceneRouteId,
                route.styleId,
                payload);

            return true;
        }
    }
}
