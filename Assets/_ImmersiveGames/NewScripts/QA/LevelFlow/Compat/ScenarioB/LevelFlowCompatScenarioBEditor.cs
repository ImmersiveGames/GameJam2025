#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Bindings;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.LevelFlow.Compat.ScenarioB
{
    /// <summary>
    /// Ferramentas de QA para validar reverse lookup ambíguo no catálogo de níveis.
    /// </summary>
    public static class LevelFlowCompatScenarioBEditor
    {
        private const string MenuRoot = "ImmersiveGames/NewScripts/QA/LevelFlow/Compat/ScenarioB";
        private const string AssetsFolderPath = "Assets/_ImmersiveGames/NewScripts/QA/LevelFlow/Compat/ScenarioB/Assets";
        private const string LevelAPath = AssetsFolderPath + "/LevelDefinition_CompatScenarioB_A.asset";
        private const string LevelBPath = AssetsFolderPath + "/LevelDefinition_CompatScenarioB_B.asset";
        private const string CatalogPath = AssetsFolderPath + "/LevelCatalog_CompatScenarioB.asset";

        [MenuItem(MenuRoot + "/Create Or Update Assets", priority = 1300)]
        public static void CreateOrUpdateAssets()
        {
            try
            {
                EnsureFolderPath(AssetsFolderPath);

                SceneRouteDefinitionAsset routeAsset = FindScenarioRouteAsset();
                if (routeAsset == null)
                {
                    Debug.LogError("[ERROR][QA] ScenarioB failed: no valid SceneRouteDefinitionAsset found (RouteId.IsValid=false or none exists).");
                    return;
                }

                LevelDefinitionAssetContainer containerA = LoadOrCreateLevelContainer(LevelAPath, "LevelDefinition_CompatScenarioB_A");
                LevelDefinitionAssetContainer containerB = LoadOrCreateLevelContainer(LevelBPath, "LevelDefinition_CompatScenarioB_B");

                LevelDefinition levelA = containerA.Definition;
                LevelDefinition levelB = containerB.Definition;

                ConfigureLevel(levelA, new LevelId("compat.scenariob.a"), routeAsset);
                ConfigureLevel(levelB, new LevelId("compat.scenariob.b"), routeAsset);
                EditorUtility.SetDirty(containerA);
                EditorUtility.SetDirty(containerB);

                LevelCatalogAsset catalog = LoadOrCreateCatalog(CatalogPath, "LevelCatalog_CompatScenarioB");
                ConfigureCatalog(catalog, levelA, levelB);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"[OBS][Compat] ScenarioB assets ready catalog='{CatalogPath}' levelA='{LevelAPath}' levelB='{LevelBPath}' routeId='{routeAsset.RouteId}'.");

                RunInternal(catalog, validateOnly: true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERROR][QA] ScenarioB failed while creating/updating assets. reason='{ex.Message}'.");
            }
        }

        [MenuItem(MenuRoot + "/Run", priority = 1301)]
        public static void Run()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                {
                    Debug.LogWarning("[WARN][QA] ScenarioB running outside Play Mode. Runtime DebugUtility anchors are best validated in Play Mode.");
                }

                LevelCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogAsset>(CatalogPath);
                if (catalog == null)
                {
                    Debug.LogError($"[ERROR][QA] ScenarioB failed: catalog not found at path='{CatalogPath}'. Run 'Create Or Update Assets' first.");
                    return;
                }

                RunInternal(catalog, validateOnly: false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERROR][QA] ScenarioB failed while running. reason='{ex.Message}'.");
            }
        }

        private static void RunInternal(LevelCatalogAsset catalog, bool validateOnly)
        {
            if (!TryGetRouteIdFromFirstLevel(catalog, out SceneRouteId routeId))
            {
                Debug.LogError("[ERROR][QA] ScenarioB failed: could not read routeId from first level routeRef.");
                return;
            }

            if (!catalog.TryResolveLevelId(routeId, out LevelId pickedLevelId))
            {
                Debug.LogError($"[ERROR][QA] ScenarioB failed: could not resolve levelId for routeId='{routeId}'.");
                return;
            }

            // Garante que o catálogo consegue resolver também o fluxo direto do LevelId selecionado.
            catalog.TryResolve(pickedLevelId, out _, out _);

            int duplicatedRoutes = CountDuplicatedRoutes(catalog);
            if (duplicatedRoutes <= 0)
            {
                Debug.LogError(
                    "[ERROR][QA] ScenarioB not ambiguous (duplicatedRoutes=0). Check routeRef validity and confirm both test levels point to the same route.");
                return;
            }

            if (!validateOnly)
            {
                Debug.Log($"[OBS][Compat] ScenarioB run completed routeId='{routeId}' pickedLevelId='{pickedLevelId}' duplicatedRoutes={duplicatedRoutes}.");
            }
        }

        private static int CountDuplicatedRoutes(LevelCatalogAsset catalog)
        {
            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty levelsProp = so.FindProperty("levels");
            if (levelsProp == null || levelsProp.arraySize <= 0)
            {
                return 0;
            }

            var counters = new Dictionary<SceneRouteId, int>();
            int duplicated = 0;

            for (int i = 0; i < levelsProp.arraySize; i++)
            {
                SerializedProperty entry = levelsProp.GetArrayElementAtIndex(i);
                SerializedProperty routeRefProp = entry.FindPropertyRelative("routeRef");
                SceneRouteDefinitionAsset routeRef = routeRefProp?.objectReferenceValue as SceneRouteDefinitionAsset;
                if (routeRef == null || !routeRef.RouteId.IsValid)
                {
                    continue;
                }

                SceneRouteId routeId = routeRef.RouteId;
                counters.TryGetValue(routeId, out int count);
                count++;
                counters[routeId] = count;

                if (count == 2)
                {
                    duplicated++;
                }
            }

            return duplicated;
        }

        private static bool TryGetRouteIdFromFirstLevel(LevelCatalogAsset catalog, out SceneRouteId routeId)
        {
            routeId = SceneRouteId.None;

            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty levelsProp = so.FindProperty("levels");
            if (levelsProp == null || levelsProp.arraySize <= 0)
            {
                return false;
            }

            SerializedProperty firstLevel = levelsProp.GetArrayElementAtIndex(0);
            SerializedProperty routeRefProp = firstLevel.FindPropertyRelative("routeRef");
            SceneRouteDefinitionAsset routeRef = routeRefProp?.objectReferenceValue as SceneRouteDefinitionAsset;
            if (routeRef == null || !routeRef.RouteId.IsValid)
            {
                return false;
            }

            routeId = routeRef.RouteId;
            return true;
        }

        private static void ConfigureCatalog(LevelCatalogAsset catalog, LevelDefinition levelA, LevelDefinition levelB)
        {
            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty levelsProp = so.FindProperty("levels");
            SerializedProperty warnProp = so.FindProperty("warnOnInvalidLevels");

            if (levelsProp == null || warnProp == null)
            {
                Debug.LogError("[ERROR][QA] ScenarioB failed: could not access serialized properties 'levels'/'warnOnInvalidLevels'.");
                return;
            }

            levelsProp.arraySize = 2;
            CopyLevelDefinitionToSerialized(levelA, levelsProp.GetArrayElementAtIndex(0));
            CopyLevelDefinitionToSerialized(levelB, levelsProp.GetArrayElementAtIndex(1));

            warnProp.boolValue = true;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void CopyLevelDefinitionToSerialized(LevelDefinition source, SerializedProperty target)
        {
            SerializedProperty levelId = target.FindPropertyRelative("levelId").FindPropertyRelative("_value");
            SerializedProperty routeRef = target.FindPropertyRelative("routeRef");
            SerializedProperty routeIdLegacy = target.FindPropertyRelative("routeId").FindPropertyRelative("_value");
            SerializedProperty contentId = target.FindPropertyRelative("contentId");

            levelId.stringValue = source.levelId.Value;
            routeRef.objectReferenceValue = source.routeRef;
            routeIdLegacy.stringValue = string.Empty;
            contentId.stringValue = LevelFlowContentDefaults.Normalize(source.contentId);
        }

        private static void ConfigureLevel(LevelDefinition definition, LevelId levelId, SceneRouteDefinitionAsset routeAsset)
        {
            definition.levelId = levelId;
            definition.routeRef = routeAsset;
            definition.routeId = SceneRouteId.None;
            definition.contentId = LevelFlowContentDefaults.DefaultContentId;
        }

        private static LevelCatalogAsset LoadOrCreateCatalog(string assetPath, string assetName)
        {
            LevelCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogAsset>(assetPath);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<LevelCatalogAsset>();
            catalog.name = assetName;
            AssetDatabase.CreateAsset(catalog, assetPath);
            return catalog;
        }

        private static LevelDefinitionAssetContainer LoadOrCreateLevelContainer(string assetPath, string assetName)
        {
            LevelDefinitionAssetContainer container = AssetDatabase.LoadAssetAtPath<LevelDefinitionAssetContainer>(assetPath);
            if (container != null)
            {
                if (container.Definition == null)
                {
                    container.Definition = new LevelDefinition();
                    EditorUtility.SetDirty(container);
                }

                return container;
            }

            container = ScriptableObject.CreateInstance<LevelDefinitionAssetContainer>();
            container.name = assetName;
            container.Definition = new LevelDefinition();
            AssetDatabase.CreateAsset(container, assetPath);
            EditorUtility.SetDirty(container);
            return container;
        }

        private static SceneRouteDefinitionAsset FindScenarioRouteAsset()
        {
            string[] guids = AssetDatabase.FindAssets("t:SceneRouteDefinitionAsset");
            SceneRouteDefinitionAsset fallback = null;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                SceneRouteDefinitionAsset route = AssetDatabase.LoadAssetAtPath<SceneRouteDefinitionAsset>(path);
                if (route == null || !route.RouteId.IsValid)
                {
                    continue;
                }

                if (string.Equals(route.RouteId.ToString(), "to-gameplay", StringComparison.Ordinal))
                {
                    return route;
                }

                fallback ??= route;
            }

            return fallback;
        }

        private static void EnsureFolderPath(string absoluteUnityPath)
        {
            string[] split = absoluteUnityPath.Split('/');
            string current = split[0];
            for (int i = 1; i < split.Length; i++)
            {
                string next = $"{current}/{split[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, split[i]);
                }

                current = next;
            }
        }

        private sealed class LevelDefinitionAssetContainer : ScriptableObject
        {
            [SerializeField] private LevelDefinition definition = new();

            public LevelDefinition Definition
            {
                get => definition;
                set => definition = value;
            }
        }
    }
}
#endif
