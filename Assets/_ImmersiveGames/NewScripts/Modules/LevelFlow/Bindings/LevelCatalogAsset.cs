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
    /// Catálogo configurável de níveis (LevelId -> MacroRouteId(SceneRouteId) + payload).
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/LevelFlow/Catalogs/LevelCatalogAsset",
        order = 30)]
    public sealed class LevelCatalogAsset : ScriptableObject, ILevelFlowService, ILevelContentResolver, ILevelMacroRouteCatalog
    {
        private readonly struct LevelResolution
        {
            public LevelResolution(LevelDefinition definition, SceneRouteId macroRouteId, SceneTransitionPayload payload, string contentId)
            {
                Definition = definition;
                MacroRouteId = macroRouteId;
                Payload = payload;
                ContentId = contentId;
            }

            public LevelDefinition Definition { get; }
            public SceneRouteId MacroRouteId { get; }
            public SceneTransitionPayload Payload { get; }
            public string ContentId { get; }
        }

        [Header("Levels")]
        [SerializeField] private List<LevelDefinition> levels = new();

        [Header("Validation")]
        [Tooltip("Quando true, registra observabilidade para validação de níveis inválidos.")]
        [SerializeField] private bool warnOnInvalidLevels = true;

        private readonly Dictionary<LevelId, LevelResolution> _cache = new();
        private readonly Dictionary<SceneRouteId, LevelId> _macroRouteToLevelCache = new();
        private readonly Dictionary<LevelId, SceneRouteId> _levelToMacroRouteCache = new();
        private readonly Dictionary<SceneRouteId, List<LevelId>> _macroRouteToLevelsCache = new();
        private readonly HashSet<SceneRouteId> _ambiguousMacroRoutes = new();
        private readonly HashSet<LevelId> _loggedLevelsThisFrame = new();
        private bool _cacheBuilt;
        private int _lastLoggedFrame = int.MinValue;

        public bool TryResolve(LevelId levelId, out SceneRouteId macroRouteId, out SceneTransitionPayload payload)
        {
            macroRouteId = SceneRouteId.None;
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

            macroRouteId = resolution.MacroRouteId;
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

            if (_ambiguousMacroRoutes.Contains(routeId))
            {
                levelId = LevelId.None;
                return false;
            }

            if (!_macroRouteToLevelCache.TryGetValue(routeId, out levelId) || !levelId.IsValid)
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

        public bool TryResolveMacroRouteId(LevelId levelId, out SceneRouteId macroRouteId)
        {
            macroRouteId = SceneRouteId.None;

            if (!levelId.IsValid)
            {
                return false;
            }

            EnsureCache();
            return _levelToMacroRouteCache.TryGetValue(levelId, out macroRouteId) && macroRouteId.IsValid;
        }

        public bool TryGetLevelsForMacroRoute(SceneRouteId macroRouteId, out IReadOnlyList<LevelId> levelIds)
        {
            levelIds = null;

            if (!macroRouteId.IsValid)
            {
                return false;
            }

            EnsureCache();
            if (!_macroRouteToLevelsCache.TryGetValue(macroRouteId, out var groupedLevels) || groupedLevels == null || groupedLevels.Count == 0)
            {
                return false;
            }

            levelIds = groupedLevels;
            return true;
        }

        public bool TryGetNextLevelInMacro(LevelId currentLevelId, out LevelId nextLevelId, bool wrapToFirst = true)
        {
            nextLevelId = LevelId.None;

            if (!currentLevelId.IsValid)
            {
                return false;
            }

            EnsureCache();

            if (!_levelToMacroRouteCache.TryGetValue(currentLevelId, out var macroRouteId) || !macroRouteId.IsValid)
            {
                return false;
            }

            if (!_macroRouteToLevelsCache.TryGetValue(macroRouteId, out var groupedLevels) || groupedLevels == null || groupedLevels.Count == 0)
            {
                return false;
            }

            int currentIndex = -1;
            for (int i = 0; i < groupedLevels.Count; i++)
            {
                if (groupedLevels[i] == currentLevelId)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                return false;
            }

            int nextIndex = currentIndex + 1;
            if (nextIndex >= groupedLevels.Count)
            {
                if (!wrapToFirst)
                {
                    return false;
                }

                nextIndex = 0;
            }

            if (nextIndex == currentIndex)
            {
                return false;
            }

            nextLevelId = groupedLevels[nextIndex];
            return nextLevelId.IsValid;
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
            _macroRouteToLevelCache.Clear();
            _levelToMacroRouteCache.Clear();
            _macroRouteToLevelsCache.Clear();
            _ambiguousMacroRoutes.Clear();

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

                SceneRouteId resolvedMacroRouteId = entry.ResolveMacroRouteId();
                if (!resolvedMacroRouteId.IsValid)
                {
                    FailFast($"MacroRoute inválida ao resolver levelId='{entry.levelId}' em levels[{i}]. macroRouteRef missing/invalid.");
                }

                if (_macroRouteToLevelCache.TryGetValue(resolvedMacroRouteId, out LevelId existingLevelId))
                {
                    if (existingLevelId != entry.levelId)
                    {
                        _ambiguousMacroRoutes.Add(resolvedMacroRouteId);
                    }
                }
                else
                {
                    _macroRouteToLevelCache.Add(resolvedMacroRouteId, entry.levelId);
                }

                _levelToMacroRouteCache.Add(entry.levelId, resolvedMacroRouteId);

                if (!_macroRouteToLevelsCache.TryGetValue(resolvedMacroRouteId, out var groupedLevels))
                {
                    groupedLevels = new List<LevelId>();
                    _macroRouteToLevelsCache.Add(resolvedMacroRouteId, groupedLevels);
                }

                groupedLevels.Add(entry.levelId);

                _cache.Add(entry.levelId, new LevelResolution(entry, resolvedMacroRouteId, entry.ToPayload(), entry.ResolveContentId()));
            }

            if (warnOnInvalidLevels)
            {
                int maxLevelsPerMacro = 0;
                foreach (var pair in _macroRouteToLevelsCache)
                {
                    if (pair.Value != null && pair.Value.Count > maxLevelsPerMacro)
                    {
                        maxLevelsPerMacro = pair.Value.Count;
                    }
                }

                DebugUtility.LogVerbose<LevelCatalogAsset>(
                    $"[OBS][Config] LevelCatalogBuild levelsResolved={_cache.Count} macroRoutesUniquelyMapped={_macroRouteToLevelCache.Count} macroRoutesMapped={_macroRouteToLevelsCache.Count} macroRouteGroups={_macroRouteToLevelsCache.Count} maxLevelsPerMacro={maxLevelsPerMacro} ambiguousMacroRoutesCount={_ambiguousMacroRoutes.Count} invalidLevels=0",
                    DebugUtility.Colors.Info);

                if (_ambiguousMacroRoutes.Count > 0)
                {
                    SceneRouteId exampleRouteId = SceneRouteId.None;
                    foreach (SceneRouteId candidate in _ambiguousMacroRoutes)
                    {
                        exampleRouteId = candidate;
                        break;
                    }

                    string routeDetails = BuildAmbiguousMacroRouteDetails(maxLevelIdsPerRoute: 5);

                    DebugUtility.LogVerbose<LevelCatalogAsset>(
                        $"[OBS][Config] LevelCatalogAmbiguousRoutes macroRoutes={_ambiguousMacroRoutes.Count} example='{exampleRouteId}' details='{routeDetails}' note='Esperado quando múltiplos levels compartilham o mesmo macroRouteId; TryResolveLevelId permanece best-effort para compat.'");
                }
            }
        }

        private string BuildAmbiguousMacroRouteDetails(int maxLevelIdsPerRoute)
        {
            if (_ambiguousMacroRoutes.Count == 0)
            {
                return "none";
            }

            int safeLimit = Math.Max(1, maxLevelIdsPerRoute);
            List<string> chunks = new();

            foreach (SceneRouteId routeId in _ambiguousMacroRoutes)
            {
                List<string> levelIds = new();
                bool hasMore = false;

                foreach (KeyValuePair<LevelId, LevelResolution> kv in _cache)
                {
                    if (kv.Value.MacroRouteId != routeId)
                    {
                        continue;
                    }

                    if (levelIds.Count < safeLimit)
                    {
                        levelIds.Add(kv.Key.ToString());
                    }
                    else
                    {
                        hasMore = true;
                        break;
                    }
                }

                string listed = levelIds.Count > 0 ? string.Join(",", levelIds) : "<none>";
                string suffix = hasMore ? ",..." : string.Empty;
                chunks.Add($"{routeId}:[{listed}{suffix}]");
            }

            return chunks.Count > 0 ? string.Join("; ", chunks) : "none";
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

            DebugUtility.LogVerbose<LevelCatalogAsset>(
                $"[OBS][SceneFlow] MacroRouteResolvedVia=LevelCatalog levelId='{levelId}' macroRouteId='{resolution.MacroRouteId}' contentId='{resolution.ContentId}'.",
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
