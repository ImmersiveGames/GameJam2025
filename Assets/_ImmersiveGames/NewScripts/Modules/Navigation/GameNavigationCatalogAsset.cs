using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Catálogo configurável de navegação (setável no Inspector).
    ///
    /// Ideia:
    /// - Remover hardcode de cenas/rotas do código.
    /// - Manter um único arquivo-asset como fonte de verdade das rotas.
    ///
    /// Observação:
    /// - O catálogo hardcoded (<see cref="GameNavigationCatalog"/>) existe apenas para debug/local dev.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameNavigationCatalog",
        menuName = "ImmersiveGames/Navigation/Game Navigation Catalog",
        order = 50)]
    public sealed class GameNavigationCatalogAsset : ScriptableObject, IGameNavigationCatalog
    {
        [Serializable]
        public sealed class RouteEntry
        {
            [Tooltip("Id canônico da rota (ex.: 'to-menu', 'to-gameplay').")]
            public string routeId;

            [Tooltip("SceneRouteId obrigatório. Deve existir no SceneRouteCatalogAsset.")]
            public SceneRouteId sceneRouteId;

            [Tooltip("TransitionStyleId obrigatório. Deve existir no TransitionStyleCatalogAsset.")]
            public TransitionStyleId transitionStyleId;

            [Tooltip("Cenas a carregar (por nome).")]
            public string[] scenesToLoad;

            [Tooltip("Cenas a descarregar (por nome).")]
            public string[] scenesToUnload;

            [Tooltip("Cena que deve ficar ativa ao final da transição.")]
            public string targetActiveScene;

            public override string ToString()
                => $"routeId='{routeId}', sceneRouteId='{sceneRouteId}', styleId='{transitionStyleId}', " +
                   $"active='{targetActiveScene}', " +
                   $"load=[{FormatArray(scenesToLoad)}], unload=[{FormatArray(scenesToUnload)}]";

            private static string FormatArray(string[] arr)
                => arr == null ? "" : string.Join(", ", arr.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        [Header("Routes")]
        [SerializeField] private List<RouteEntry> routes = new();

        [Header("Validation")]
        [Tooltip("Quando true, registra warning se houver rotas inválidas/duplicadas.")]
        [SerializeField] private bool warnOnInvalidRoutes = true;

        private readonly Dictionary<string, RouteEntry> _cache = new(StringComparer.Ordinal);
        private IReadOnlyCollection<string> _routeIds = Array.Empty<string>();
        private bool _cacheBuilt;

        public IReadOnlyCollection<string> RouteIds
        {
            get
            {
                EnsureCache();
                return _routeIds;
            }
        }

        public bool TryGet(string routeId, out GameNavigationEntry entry)
        {
            entry = default;

            if (string.IsNullOrWhiteSpace(routeId))
            {
                return false;
            }

            EnsureCache();

            if (!_cache.TryGetValue(routeId, out var cachedEntry) || cachedEntry == null)
            {
                return false;
            }

            return TryBuildEntry(cachedEntry, out entry);
        }

        private void OnEnable()
        {
            // Reconstrói cache ao (re)carregar asset.
            _cacheBuilt = false;
        }

        private void OnValidate()
        {
            // Ajuda a detectar problemas durante edição no Inspector.
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
                _routeIds = Array.Empty<string>();
                return;
            }

            int invalidCount = 0;
            foreach (var entry in routes)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.routeId))
                {
                    invalidCount++;
                    continue;
                }

                if (_cache.ContainsKey(entry.routeId))
                {
                    invalidCount++;
                    if (warnOnInvalidRoutes)
                    {
                        DebugUtility.LogWarning<GameNavigationCatalogAsset>(
                            $"[Navigation] Rota duplicada no catálogo configurável. routeId='{entry.routeId}'. Apenas a primeira será usada.");
                    }
                    continue;
                }

                _cache.Add(entry.routeId, entry);
            }

            _routeIds = _cache.Keys.ToArray();

            if (warnOnInvalidRoutes && invalidCount > 0)
            {
                DebugUtility.LogWarning<GameNavigationCatalogAsset>(
                    $"[Navigation] Catálogo configurável possui entradas inválidas/duplicadas. invalidCount={invalidCount}.");
            }
        }

        private static bool TryBuildEntry(RouteEntry entry, out GameNavigationEntry resolved)
        {
            resolved = default;

            if (entry == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.targetActiveScene))
            {
                // Comentário: sem cena ativa, a transição pode terminar em estado ambíguo.
                DebugUtility.LogWarning<GameNavigationCatalogAsset>(
                    $"[Navigation] Rota sem targetActiveScene. {entry}");
            }

            var payload = SceneTransitionPayload.CreateSceneData(
                scenesToLoad: Sanitize(entry.scenesToLoad),
                scenesToUnload: Sanitize(entry.scenesToUnload),
                targetActiveScene: entry.targetActiveScene);

            var sceneRouteId = entry.sceneRouteId;

            if (!sceneRouteId.IsValid)
            {
                DebugUtility.LogWarning<GameNavigationCatalogAsset>(
                    $"[Navigation] Rota sem SceneRouteId válido. routeId='{entry.routeId}'.");
                return false;
            }

            var styleId = entry.transitionStyleId;

            if (!styleId.IsValid)
            {
                DebugUtility.LogWarning<GameNavigationCatalogAsset>(
                    $"[Navigation] Rota sem TransitionStyleId válido. routeId='{entry.routeId}'.");
                return false;
            }

            resolved = new GameNavigationEntry(sceneRouteId, styleId, payload);
            return true;
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
