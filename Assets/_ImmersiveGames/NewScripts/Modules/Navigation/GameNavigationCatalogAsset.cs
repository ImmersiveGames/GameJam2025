using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
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
    /// Campos LEGACY permanecem apenas para migração e são ignorados (com warning).
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameNavigationCatalog",
        menuName = "ImmersiveGames/NewScripts/Navigation/GameNavigationCatalog",
        order = 0)]
    public sealed class GameNavigationCatalogAsset : ScriptableObject, IGameNavigationCatalog
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

            [Header("LEGACY (ignored) — use SceneRouteCatalogAsset")]
            [Tooltip("LEGACY: cenas a carregar. Ignorado a partir da F3.")]
            [HideInInspector]
            public List<string> scenesToLoad;

            [Tooltip("LEGACY: cenas a descarregar. Ignorado a partir da F3.")]
            [HideInInspector]
            public List<string> scenesToUnload;

            [Tooltip("LEGACY: cena ativa ao final da transição. Ignorado a partir da F3.")]
            [HideInInspector]
            public string targetActiveScene;
        }

        [FormerlySerializedAs("routes")]
        [SerializeField] private List<RouteEntry> _routes = new();

        private readonly Dictionary<string, GameNavigationEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
        private bool _built;

        private static bool _warnedLegacySceneDataIgnored;

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

            if (!_warnedLegacySceneDataIgnored && TryGetLegacySceneData(routeId, out var legacyDetail))
            {
                _warnedLegacySceneDataIgnored = true;
                DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                    "[OBS] GameNavigationCatalogAsset contém Scene Data LEGACY (ScenesToLoad/Unload/Active), " +
                    "mas a política atual (F3) ignora esses campos: a Scene Data deve vir do SceneRouteCatalogAsset. " +
                    legacyDetail);
            }

            return true;
        }

        private void EnsureBuilt()
        {
            if (_built)
                return;

            _built = true;
            _cache.Clear();

            if (_routes == null)
                return;

            foreach (var r in _routes)
            {
                if (!TryBuildEntry(r, out var e))
                    continue;

                _cache[r.routeId.Trim()] = e;
            }
        }

        private static bool TryBuildEntry(RouteEntry r, out GameNavigationEntry entry)
        {
            entry = default;

            if (r == null)
                return false;

            if (string.IsNullOrWhiteSpace(r.routeId))
                return false;

            if (!r.sceneRouteId.IsValid)
                return false;

            if (!r.styleId.IsValid)
                return false;

            var payload = SceneTransitionPayload.Empty;

            entry = new GameNavigationEntry(
                r.sceneRouteId,
                r.styleId,
                payload);

            return true;
        }

        private bool TryGetLegacySceneData(string routeId, out string detail)
        {
            detail = string.Empty;

            if (_routes == null)
                return false;

            for (int i = 0; i < _routes.Count; i++)
            {
                var r = _routes[i];
                if (r == null)
                    continue;

                if (!string.Equals(r.routeId?.Trim(), routeId?.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                bool hasLegacy =
                    (r.scenesToLoad != null && r.scenesToLoad.Count > 0) ||
                    (r.scenesToUnload != null && r.scenesToUnload.Count > 0) ||
                    !string.IsNullOrWhiteSpace(r.targetActiveScene);

                if (!hasLegacy)
                    return false;

                detail =
                    $"(routeId='{r.routeId}', sceneRouteId='{r.sceneRouteId}', styleId='{r.styleId}')";
                return true;
            }

            return false;
        }
    }
}
