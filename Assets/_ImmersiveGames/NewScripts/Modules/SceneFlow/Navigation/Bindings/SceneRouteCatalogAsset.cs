using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    /// <summary>
    /// Catálogo configurável de rotas do SceneFlow (SceneRouteId -> SceneRouteDefinition).
    /// Suporta wiring por referência direta de assets de rota e fallback por entrada inline.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneRouteCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Catalogs/SceneRouteCatalogAsset",
        order = 30)]
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
                    $"[FATAL][Config] SceneRouteCatalogAsset inválido durante OnValidate. asset='{assetPath}', detail='{ex.Message}'.");
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

            int viaAssetRefCount = 0;
            int viaInlineIdCount = 0;

            if (routeDefinitions != null)
            {
                for (int i = 0; i < routeDefinitions.Count; i++)
                {
                    var routeAsset = routeDefinitions[i];
                    BuildFromAsset(routeAsset, out var routeId, out var routeDefinition, index: i);

                    if (_cache.ContainsKey(routeId))
                    {
                        FailFast($"Rota duplicada no SceneRouteCatalog (asset ref). routeId='{routeId}', index={i}.");
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
                for (int i = 0; i < routes.Count; i++)
                {
                    var entry = routes[i];
                    BuildFromEntry(entry, out var routeId, out var routeDefinition, index: i);

                    if (_cache.ContainsKey(routeId))
                    {
                        FailFast($"Rota duplicada no SceneRouteCatalog (inline). routeId='{routeId}', index={i}.");
                    }

                    viaInlineIdCount++;
                    _cache.Add(routeId, routeDefinition);

                    DebugUtility.LogVerbose<SceneRouteCatalogAsset>(
                        $"[OBS][Config] RouteResolvedVia=RouteId routeId='{routeId}'.",
                        DebugUtility.Colors.Info);
                }
            }

            if (warnOnInvalidRoutes && _cache.Count == 0)
            {
                DebugUtility.LogWarning<SceneRouteCatalogAsset>(
                    "[SceneFlow] SceneRouteCatalog não contém rotas válidas.");
            }

            DebugUtility.LogVerbose<SceneRouteCatalogAsset>(
                "[OBS][Config] SceneRouteCatalogBuild " +
                $"routesResolved={_cache.Count} viaAssetRef={viaAssetRefCount} viaRouteId={viaInlineIdCount} invalidRoutes=0",
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
                FailFast($"RouteDefinitionAsset inválido em routeDefinitions[{index}] (routeId vazio). asset='{routeAsset.name}'.");
            }

            routeDefinition = routeAsset.ToDefinition();
            EnsureActiveScenePolicy(routeId, routeDefinition.RouteKind, routeDefinition.TargetActiveScene, "assetRef");
            EnsureResetPolicyConsistency(routeId, routeDefinition.RouteKind, routeDefinition.RequiresWorldReset, "assetRef");
        }

        private static void BuildFromEntry(
            RouteEntry entry,
            out SceneRouteId routeId,
            out SceneRouteDefinition routeDefinition,
            int index)
        {
            routeId = SceneRouteId.None;
            routeDefinition = default;

            if (entry == null)
            {
                FailFast($"RouteEntry nulo em routes[{index}].");
            }

            if (!entry.routeId.IsValid)
            {
                FailFast($"RouteEntry inválido em routes[{index}] (routeId vazio). field='{nameof(RouteEntry.routeId)}'.");
            }

            routeId = entry.routeId;

            var load = ResolveKeys(entry.scenesToLoadKeys, routeId, nameof(RouteEntry.scenesToLoadKeys));
            var unload = ResolveKeys(entry.scenesToUnloadKeys, routeId, nameof(RouteEntry.scenesToUnloadKeys));
            var active = ResolveSingleKey(entry.targetActiveSceneKey, routeId, nameof(RouteEntry.targetActiveSceneKey));

            LogResolvedRouteSceneList(routeId, nameof(RouteEntry.scenesToUnloadKeys), unload);

            EnsureActiveScenePolicy(routeId, entry.routeKind, active, "routeId");
            EnsureResetPolicyConsistency(routeId, entry.routeKind, entry.requiresWorldReset, "routeId");
            routeDefinition = new SceneRouteDefinition(load, unload, active, entry.routeKind, entry.requiresWorldReset);
        }

        private static void LogResolvedRouteSceneList(SceneRouteId routeId, string fieldName, IReadOnlyList<string> resolved)
        {
            DebugUtility.Log(typeof(SceneRouteCatalogAsset),
                $"[OBS][SceneFlow] RouteSceneListResolved routeId='{routeId}' field='{fieldName}' scenes=[{FormatSceneDetails(resolved)}].",
                DebugUtility.Colors.Info);
        }

        private static void EnsureActiveScenePolicy(SceneRouteId routeId, SceneRouteKind routeKind, string activeScene, string source)
        {
            if (string.IsNullOrWhiteSpace(activeScene) && RequiresActiveScene(routeKind))
            {
                FailFast(
                    $"routeId='{routeId}' resolvida via {source} requer TargetActiveScene para routeKind='{routeKind}', " +
                    $"mas '{nameof(RouteEntry.targetActiveSceneKey)}' está ausente/nulo.");
            }
        }

        private static bool RequiresActiveScene(SceneRouteKind routeKind)
        {
            // Regra explícita: rotas de gameplay devem sempre definir cena ativa alvo.
            return routeKind == SceneRouteKind.Gameplay;
        }


        private static void EnsureResetPolicyConsistency(SceneRouteId routeId, SceneRouteKind routeKind, bool requiresWorldReset, string source)
        {
            if (routeKind == SceneRouteKind.Unspecified)
            {
                FailFast($"routeId='{routeId}' resolvida via {source} possui RouteKind='{SceneRouteKind.Unspecified}' (inválido para policy de reset).");
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

        private static string[] ResolveKeys(SceneKeyAsset[] keys, SceneRouteId routeId, string fieldName)
        {
            if (keys == null || keys.Length == 0)
            {
                DebugUtility.Log(typeof(SceneRouteCatalogAsset),
                    $"[OBS][SceneFlow] ResolveRouteKeys routeId='{routeId}' field='{fieldName}' before=[] after=[].",
                    DebugUtility.Colors.Info);
                return Array.Empty<string>();
            }

            var beforeFilter = new List<string>(keys.Length);
            var resolved = new List<string>(keys.Length);
            var seenNames = new HashSet<string>(StringComparer.Ordinal);

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

                string sceneName = key.SceneName.Trim();
                beforeFilter.Add(sceneName);

                if (!seenNames.Add(sceneName))
                {
                    DebugUtility.LogWarning(typeof(SceneRouteCatalogAsset),
                        $"[OBS][SceneFlow] ResolveRouteKeysFiltered routeId='{routeId}' field='{fieldName}' key='{key.name}' scene='{sceneName}' reason='duplicate_scene_name'.");
                    continue;
                }

                if (!string.Equals(key.name, sceneName, StringComparison.Ordinal))
                {
                    DebugUtility.LogWarning(typeof(SceneRouteCatalogAsset),
                        $"[OBS][SceneFlow] ResolveRouteKeysFiltered routeId='{routeId}' field='{fieldName}' key='{key.name}' scene='{sceneName}' reason='invalid_key_name_mismatch'.");
                }

                resolved.Add(sceneName);
            }

            DebugUtility.Log(typeof(SceneRouteCatalogAsset),
                $"[OBS][SceneFlow] ResolveRouteKeys routeId='{routeId}' field='{fieldName}' before=[{FormatSceneDetails(beforeFilter)}] after=[{FormatSceneDetails(resolved)}].",
                DebugUtility.Colors.Info);

            return resolved.ToArray();
        }

        private static string FormatSceneDetails(IReadOnlyList<string> scenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return "<none>";
            }

            var details = new List<string>(scenes.Count);
            for (int i = 0; i < scenes.Count; i++)
            {
                string sceneName = scenes[i] ?? string.Empty;
                int buildIndex = ResolveBuildIndex(sceneName);
                bool isInBuildSettings = Application.CanStreamedLevelBeLoaded(sceneName);
                details.Add($"name='{sceneName}', buildIndex={buildIndex}, isInBuildSettings={isInBuildSettings}");
            }

            return string.Join(" | ", details);
        }

        private static int ResolveBuildIndex(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return -1;
            }

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid())
            {
                return loadedScene.buildIndex;
            }

#if UNITY_EDITOR
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                string path = scenes[i].path;
                string name = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(name, sceneName, StringComparison.Ordinal))
                {
                    return i;
                }
            }
#endif

            return -1;
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
