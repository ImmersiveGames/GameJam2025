#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Composition;
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
        private const string ScenarioLevelAId = "compat.scenariob.a";
        private const string ScenarioLevelBId = "compat.scenariob.b";
        private const string ScenarioReasonA = "QA/Compat/ScenarioB/A";
        private const string ScenarioReasonB = "QA/Compat/ScenarioB/B";

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

                ConfigureLevel(levelA, new LevelId(ScenarioLevelAId), routeAsset);
                ConfigureLevel(levelB, new LevelId(ScenarioLevelBId), routeAsset);
                EditorUtility.SetDirty(containerA);
                EditorUtility.SetDirty(containerB);

                LevelCatalogAsset catalog = LoadOrCreateCatalog(CatalogPath, "LevelCatalog_CompatScenarioB");
                ConfigureCatalog(catalog, levelA, levelB);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                LevelCatalogAsset reloadedCatalog = AssetDatabase.LoadAssetAtPath<LevelCatalogAsset>(CatalogPath);
                if (reloadedCatalog == null)
                {
                    Debug.LogError($"[ERROR][QA] ScenarioB failed: could not reload catalog at path='{CatalogPath}' after update.");
                    return;
                }

                Debug.Log(
                    $"[OBS][Compat] ScenarioB assets ready catalog='{CatalogPath}' levelA='{LevelAPath}' levelB='{LevelBPath}' routeId='{routeAsset.RouteId}'.");
                Debug.Log($"[OBS][Compat] ScenarioB using catalog='{reloadedCatalog.name}' path='{CatalogPath}'");

                RunInternal(reloadedCatalog, validateOnly: true);
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

                Debug.Log($"[OBS][Compat] ScenarioB using catalog='{catalog.name}' path='{CatalogPath}'");
                RunInternal(catalog, validateOnly: false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERROR][QA] ScenarioB failed while running. reason='{ex.Message}'.");
            }
        }

        private static void RunInternal(LevelCatalogAsset catalog, bool validateOnly)
        {
            // Mantém validação estrutural de ambiguidade sem depender de reverse lookup routeId -> levelId.
            catalog.TryResolve(new LevelId(ScenarioLevelAId), out _, out _);
            catalog.TryResolve(new LevelId(ScenarioLevelBId), out _, out _);

            int duplicatedRoutes = CountDuplicatedRoutes(catalog);
            if (duplicatedRoutes <= 0)
            {
                Debug.LogError(
                    "[ERROR][QA] ScenarioB not ambiguous (duplicatedRoutes=0). Check routeRef validity and confirm both test levels point to the same route.");
                return;
            }

            if (!validateOnly)
            {
                if (RunExplicitStartGameplayScenario())
                {
                    Debug.Log(
                        $"[PASS][Compat] ScenarioB run completed StartGameplayAsync levelA='{ScenarioLevelAId}' reasonA='{ScenarioReasonA}' levelB='{ScenarioLevelBId}' reasonB='{ScenarioReasonB}' duplicatedRoutes={duplicatedRoutes}.");
                }
            }
        }

        private static bool RunExplicitStartGameplayScenario()
        {
            if (DependencyManager.Provider == null)
            {
                Debug.LogError("[ERROR][QA] ScenarioB failed: DependencyManager.Provider is null.");
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelFlowRuntimeService>(out ILevelFlowRuntimeService levelFlow) || levelFlow == null)
            {
                Debug.LogError("[ERROR][QA] ScenarioB failed: global service ILevelFlowRuntimeService is unavailable.");
                return false;
            }

            levelFlow.StartGameplayAsync(ScenarioLevelAId, ScenarioReasonA).GetAwaiter().GetResult();
            levelFlow.StartGameplayAsync(ScenarioLevelBId, ScenarioReasonB).GetAwaiter().GetResult();
            return true;
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
