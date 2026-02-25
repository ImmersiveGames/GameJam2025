using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Bindings
{
    /// <summary>
    /// Catálogo configurável de níveis (LevelId -> SceneRouteId + payload).
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/LevelFlow/Catalogs/LevelCatalogAsset",
        order = 30)]
    public sealed class LevelCatalogAsset : ScriptableObject, ILevelFlowService, ILevelContentResolver
    {
        private readonly struct LevelResolution
        {
            public LevelResolution(LevelDefinition definition, SceneRouteId routeId, SceneTransitionPayload payload, string contentId)
            {
                Definition = definition;
                RouteId = routeId;
                Payload = payload;
                ContentId = contentId;
            }

            public LevelDefinition Definition { get; }
            public SceneRouteId RouteId { get; }
            public SceneTransitionPayload Payload { get; }
            public string ContentId { get; }
        }

        [Header("Levels")]
        [SerializeField] private List<LevelDefinition> levels = new();

        [Header("Validation")]
        [Tooltip("Quando true, registra observabilidade para validação de níveis inválidos.")]
        [SerializeField] private bool warnOnInvalidLevels = true;

        private readonly Dictionary<LevelId, LevelResolution> _cache = new();
        private readonly Dictionary<SceneRouteId, LevelId> _routeToLevelCache = new();
        private readonly HashSet<LevelId> _loggedLevelsThisFrame = new();
        private bool _cacheBuilt;
        private int _lastLoggedFrame = int.MinValue;

        public bool TryResolve(LevelId levelId, out SceneRouteId routeId, out SceneTransitionPayload payload)
        {
            routeId = SceneRouteId.None;
            payload = SceneTransitionPayload.Empty;

            if (!levelId.IsValid)
            {
                return false;
            }

            EnsureCache();

            if (!_cache.TryGetValue(levelId, out var resolution))
            {
                return false;
            }

            routeId = resolution.RouteId;
            payload = resolution.Payload;
            LogResolutionDedupePerFrame(levelId, resolution);
            return true;
        }

        public bool TryResolveLevelId(SceneRouteId routeId, out LevelId levelId)
        {
            levelId = LevelId.None;

            if (!routeId.IsValid)
            {
                return false;
            }

            EnsureCache();
            if (!_routeToLevelCache.TryGetValue(routeId, out levelId) || !levelId.IsValid)
            {
                return false;
            }

            return true;
        }


        public bool TryResolveContentId(LevelId levelId, out string contentId)
        {
            contentId = LevelFlowContentDefaults.DefaultContentId;

            if (!levelId.IsValid)
            {
                return false;
            }

            EnsureCache();
            if (!_cache.TryGetValue(levelId, out var resolution))
            {
                return false;
            }

            contentId = LevelFlowContentDefaults.Normalize(resolution.ContentId);
            return true;
        }

        private void OnEnable()
        {
            _cacheBuilt = false;
            _loggedLevelsThisFrame.Clear();
            _lastLoggedFrame = int.MinValue;
        }

        private void OnValidate()
        {
            _cacheBuilt = false;

#if UNITY_EDITOR
            TryMigrateLegacyRouteRefsInEditor();
            TryMigrateMacroRouteRefsInEditor();
            EnsureDefaultContentIdsInEditor();
#endif

            try
            {
                EnsureCache();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(LevelCatalogAsset),
                    $"[FATAL][Config] LevelCatalogAsset inválido durante OnValidate. detail='{ex.Message}'.");
                throw;
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
            _routeToLevelCache.Clear();
            var macroRoutes = new HashSet<SceneRouteId>();

            if (levels == null || levels.Count == 0)
            {
                FailFast("LevelCatalog sem níveis configurados.");
            }

            for (int i = 0; i < levels.Count; i++)
            {
                var entry = levels[i];
                if (entry == null)
                {
                    FailFast($"LevelDefinition nulo em levels[{i}].");
                }

                if (!entry.levelId.IsValid)
                {
                    FailFast($"LevelDefinition inválido em levels[{i}] (levelId vazio/inválido).");
                }

                if (_cache.ContainsKey(entry.levelId))
                {
                    FailFast($"LevelId duplicado no LevelCatalog. levelId='{entry.levelId}', index={i}.");
                }

                SceneRouteId resolvedRouteId = entry.ResolveRouteId();
                if (!resolvedRouteId.IsValid)
                {
                    FailFast($"Rota inválida ao resolver levelId='{entry.levelId}' em levels[{i}].");
                }

                SceneRouteId resolvedMacroRouteId = entry.ResolveMacroRouteId();
                if (!resolvedMacroRouteId.IsValid)
                {
                    FailFast($"MacroRoute inválida ao resolver levelId='{entry.levelId}' em levels[{i}]. routeId='{resolvedRouteId}', macroRouteRef missing/invalid.");
                }

                macroRoutes.Add(resolvedMacroRouteId);

                if (!_routeToLevelCache.ContainsKey(resolvedRouteId))
                {
                    _routeToLevelCache.Add(resolvedRouteId, entry.levelId);
                }

                _cache.Add(entry.levelId, new LevelResolution(entry, resolvedRouteId, entry.ToPayload(), entry.ResolveContentId()));
            }

            if (warnOnInvalidLevels)
            {
                DebugUtility.LogVerbose<LevelCatalogAsset>(
                    $"[OBS][Config] LevelCatalogBuild levelsResolved={_cache.Count} routesMapped={_routeToLevelCache.Count} macroRoutesMapped={macroRoutes.Count} invalidLevels=0",
                    DebugUtility.Colors.Info);
            }
        }

        private void LogResolutionDedupePerFrame(LevelId levelId, LevelResolution resolution)
        {
            int currentFrame = Time.frameCount;
            if (currentFrame != _lastLoggedFrame)
            {
                _loggedLevelsThisFrame.Clear();
                _lastLoggedFrame = currentFrame;
            }

            if (!_loggedLevelsThisFrame.Add(levelId))
            {
                return;
            }

            LevelDefinition definition = resolution.Definition;
            DebugUtility.LogVerbose<LevelCatalogAsset>(
                $"[OBS][SceneFlow] RouteResolvedVia=AssetRef levelId='{levelId}' routeId='{resolution.RouteId}' asset='{definition.routeRef.name}'.",
                DebugUtility.Colors.Info);
        }

#if UNITY_EDITOR
        private void TryMigrateLegacyRouteRefsInEditor()
        {
            if (levels == null || levels.Count == 0)
            {
                return;
            }

            Dictionary<SceneRouteId, SceneRouteDefinitionAsset> routesById = BuildRoutesById();
            int migratedCount = 0;

            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition definition = levels[i];
                if (definition == null || definition.routeRef != null || !definition.routeId.IsValid)
                {
                    continue;
                }

                if (!routesById.TryGetValue(definition.routeId, out SceneRouteDefinitionAsset routeAsset) || routeAsset == null)
                {
                    continue;
                }

                // Migração automática do legado: promove routeId para routeRef.
                definition.routeRef = routeAsset;
                migratedCount++;
            }

            if (migratedCount <= 0)
            {
                return;
            }

            EditorUtility.SetDirty(this);
            DebugUtility.Log(typeof(LevelCatalogAsset),
                $"[OBS][Config] LevelCatalogAsset OnValidate migrou routeRef a partir de routeId legado. migrated={migratedCount}, asset='{name}'.",
                DebugUtility.Colors.Info);
        }

        private void TryMigrateMacroRouteRefsInEditor()
        {
            if (levels == null || levels.Count == 0)
            {
                return;
            }

            int migratedCount = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition definition = levels[i];
                if (definition == null || definition.macroRouteRef != null || definition.routeRef == null)
                {
                    continue;
                }

                if (!definition.routeRef.RouteId.IsValid)
                {
                    continue;
                }

                definition.macroRouteRef = definition.routeRef;
                migratedCount++;

                DebugUtility.Log(typeof(LevelCatalogAsset),
                    $"[OBS][MIGRATION][LevelCatalog] macroRouteRef auto-set a partir de routeRef. levelId='{definition.levelId}', macroRouteId='{definition.routeRef.RouteId}', asset='{name}'.",
                    DebugUtility.Colors.Info);
            }

            if (migratedCount <= 0)
            {
                return;
            }

            EditorUtility.SetDirty(this);
        }

        private void EnsureDefaultContentIdsInEditor()
        {
            if (levels == null || levels.Count == 0)
            {
                return;
            }

            int migratedCount = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition definition = levels[i];
                if (definition == null)
                {
                    continue;
                }

                string normalizedContentId = LevelFlowContentDefaults.Normalize(definition.contentId);
                if (string.Equals(definition.contentId, normalizedContentId, StringComparison.Ordinal))
                {
                    continue;
                }

                definition.contentId = normalizedContentId;
                migratedCount++;
            }

            if (migratedCount <= 0)
            {
                return;
            }

            EditorUtility.SetDirty(this);
            DebugUtility.Log(typeof(LevelCatalogAsset),
                $"[OBS][Compat] LevelCatalogAsset OnValidate contentId default aplicado. migrated={migratedCount}, default='{LevelFlowContentDefaults.DefaultContentId}', asset='{name}'.",
                DebugUtility.Colors.Info);
        }

        private static Dictionary<SceneRouteId, SceneRouteDefinitionAsset> BuildRoutesById()
        {
            var routesById = new Dictionary<SceneRouteId, SceneRouteDefinitionAsset>();
            string[] guids = AssetDatabase.FindAssets("t:SceneRouteDefinitionAsset");

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                SceneRouteDefinitionAsset routeAsset = AssetDatabase.LoadAssetAtPath<SceneRouteDefinitionAsset>(assetPath);
                if (routeAsset == null || !routeAsset.RouteId.IsValid)
                {
                    continue;
                }

                if (!routesById.ContainsKey(routeAsset.RouteId))
                {
                    routesById.Add(routeAsset.RouteId, routeAsset);
                }
            }

            return routesById;
        }
#endif


        private static void FailFast(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError(typeof(LevelCatalogAsset), message);
            throw new InvalidOperationException(message);
        }
    }
}
