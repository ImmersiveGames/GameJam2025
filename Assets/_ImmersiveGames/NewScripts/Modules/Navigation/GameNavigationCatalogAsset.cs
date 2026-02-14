using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine.Serialization;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Catálogo de intents de navegação (produção).
    ///
    /// F3/Fase 3:
    /// - Catálogo não define Scene Data.
    /// - Rota pode ser resolvida por referência direta opcional (routeRef) ou por SceneRouteId.
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

            [Tooltip("SceneRouteId associado ao intent (fallback quando routeRef não está setado).")]
            public SceneRouteId sceneRouteId;

            [Tooltip("Referência direta opcional para a rota canônica.")]
            public SceneRouteDefinitionAsset routeRef;

            [FormerlySerializedAs("transitionStyleId")]
            [Tooltip("TransitionStyleId que define ProfileId/UseFade (SceneFlow) usado nesta navegação.")]
            public TransitionStyleId styleId;

            public SceneRouteId ResolveRouteId(string owner)
            {
                if (routeRef != null)
                {
                    var routeRefId = routeRef.RouteId;
                    if (!routeRefId.IsValid)
                    {
                        return SceneRouteId.None;
                    }

                    if (sceneRouteId.IsValid && sceneRouteId != routeRefId)
                    {
                        HandleRouteMismatch(owner, routeId, sceneRouteId, routeRefId);
                    }

                    DebugUtility.LogVerbose(typeof(GameNavigationCatalogAsset),
                        $"[OBS][Config] RouteResolvedVia=AssetRef owner='{owner}', intentId='{routeId}', routeId='{routeRefId}', asset='{routeRef.name}'.",
                        DebugUtility.Colors.Info);

                    return routeRefId;
                }

                if (sceneRouteId.IsValid)
                {
                    DebugUtility.LogVerbose(typeof(GameNavigationCatalogAsset),
                        $"[OBS][Config] RouteResolvedVia=RouteId owner='{owner}', intentId='{routeId}', routeId='{sceneRouteId}'.",
                        DebugUtility.Colors.Info);
                }

                return sceneRouteId;
            }

            public void MigrateLegacy()
            {
                routeId = routeId?.Trim();
            }

            private static void HandleRouteMismatch(string owner, string intentId, SceneRouteId sceneRouteId, SceneRouteId routeRefId)
            {
                string message =
                    $"[FATAL][Config] GameNavigationCatalog routeId divergente de routeRef. " +
                    $"owner='{owner}', intentId='{intentId}', sceneRouteId='{sceneRouteId}', routeRef.routeId='{routeRefId}'.";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                    $"{message} Em editor/dev, routeRef terá prioridade (RouteResolvedVia=AssetRef).");
#else
                DebugUtility.LogError(typeof(GameNavigationCatalogAsset), message);
                throw new InvalidOperationException(message);
#endif
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
            return _cache.TryGetValue(routeId.Trim(), out entry);
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

        private bool TryBuildEntry(RouteEntry route, out GameNavigationEntry entry)
        {
            entry = default;

            if (route == null)
                return false;

            if (string.IsNullOrWhiteSpace(route.routeId))
                return false;

            var resolvedRouteId = route.ResolveRouteId(name);
            if (!resolvedRouteId.IsValid)
                return false;

            if (!route.styleId.IsValid)
                return false;

            entry = new GameNavigationEntry(
                resolvedRouteId,
                route.styleId,
                SceneTransitionPayload.Empty);

            return true;
        }
    }
}
