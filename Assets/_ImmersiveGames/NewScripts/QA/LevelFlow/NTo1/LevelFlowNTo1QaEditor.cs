#if UNITY_EDITOR
using System;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.LevelFlow.NTo1
{
    /// <summary>
    /// Harness DEV/QA para configurar cenário N->1 (múltiplos levelId para a mesma routeRef).
    /// </summary>
    public static class LevelFlowNTo1QaEditor
    {
        private const string MenuRoot = "ImmersiveGames/NewScripts/QA/LevelFlow/NTo1";
        private const string LevelCatalogPath = "Assets/Resources/Navigation/LevelCatalog.asset";

        private const string BaseLevelId = "level.1";
        private const string LevelAId = "qa.level.nto1.a";
        private const string LevelBId = "qa.level.nto1.b";
        private const string ContentAId = "content.1";
        private const string ContentBId = "content.2";

        [MenuItem(MenuRoot + "/Create Or Update Catalog Entries", priority = 1400)]
        public static void CreateOrUpdateCatalogEntries()
        {
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogAsset>(LevelCatalogPath);
                if (catalog == null)
                {
                    Debug.LogError($"[ERROR][QA] NTo1 failed: LevelCatalogAsset not found at path='{LevelCatalogPath}'.");
                    return;
                }

                var serializedCatalog = new SerializedObject(catalog);
                SerializedProperty levelsProp = serializedCatalog.FindProperty("levels");
                if (levelsProp == null)
                {
                    Debug.LogError("[ERROR][QA] NTo1 failed: serialized property 'levels' is missing.");
                    return;
                }

                SceneRouteDefinitionAsset gameplayRouteRef = ResolveRouteRefForLevel(levelsProp, BaseLevelId);
                if (gameplayRouteRef == null)
                {
                    Debug.LogError($"[ERROR][QA] NTo1 failed: base level routeRef not found for levelId='{BaseLevelId}'.");
                    return;
                }

                UpsertLevel(levelsProp, LevelAId, gameplayRouteRef, ContentAId);
                UpsertLevel(levelsProp, LevelBId, gameplayRouteRef, ContentBId);

                serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();

                int duplicatedRoutes = CountDuplicatedRoutes(levelsProp);
                Debug.Log(
                    $"[OBS][Compat] NTo1 catalog prepared baseLevelId='{BaseLevelId}' levelA='{LevelAId}' levelB='{LevelBId}' routeRef='{gameplayRouteRef.name}' duplicatedRoutes={duplicatedRoutes}.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERROR][QA] NTo1 failed while preparing catalog. reason='{ex.Message}'.");
            }
        }

        private static SceneRouteDefinitionAsset ResolveRouteRefForLevel(SerializedProperty levelsProp, string levelId)
        {
            for (int i = 0; i < levelsProp.arraySize; i++)
            {
                SerializedProperty entry = levelsProp.GetArrayElementAtIndex(i);
                string currentLevelId = entry.FindPropertyRelative("levelId")?.FindPropertyRelative("_value")?.stringValue;
                if (!string.Equals(currentLevelId, levelId, StringComparison.Ordinal))
                {
                    continue;
                }

                return entry.FindPropertyRelative("routeRef")?.objectReferenceValue as SceneRouteDefinitionAsset;
            }

            return null;
        }

        private static void UpsertLevel(SerializedProperty levelsProp, string levelId, SceneRouteDefinitionAsset routeRef, string contentId)
        {
            int index = FindLevelIndex(levelsProp, levelId);
            if (index < 0)
            {
                levelsProp.arraySize++;
                index = levelsProp.arraySize - 1;
            }

            SerializedProperty entry = levelsProp.GetArrayElementAtIndex(index);
            if (entry == null)
            {
                Debug.LogError($"[ERROR][QA] NTo1 failed: could not access levels[{index}] serialized entry.");
                return;
            }

            SerializedProperty levelIdValue = entry.FindPropertyRelative("levelId")?.FindPropertyRelative("_value");
            if (levelIdValue == null)
            {
                Debug.LogError($"[ERROR][QA] NTo1 failed: missing serialized 'levelId._value' on levels[{index}].");
                return;
            }

            SerializedProperty routeRefProp = entry.FindPropertyRelative("routeRef");
            if (routeRefProp == null)
            {
                Debug.LogError($"[ERROR][QA] NTo1 failed: missing serialized 'routeRef' on levels[{index}].");
                return;
            }

            SerializedProperty routeIdValue = entry.FindPropertyRelative("routeId")?.FindPropertyRelative("_value");
            if (routeIdValue == null)
            {
                Debug.LogWarning($"[WARN][QA] NTo1: missing serialized 'routeId._value' on levels[{index}] (legacy field).");
            }

            SerializedProperty contentIdProp = entry.FindPropertyRelative("contentId");
            if (contentIdProp == null || contentIdProp.propertyType != SerializedPropertyType.String)
            {
                Debug.LogError($"[ERROR][QA] NTo1 failed: missing serialized string 'contentId' on levels[{index}].");
                return;
            }

            levelIdValue.stringValue = levelId;
            routeRefProp.objectReferenceValue = routeRef;
            if (routeIdValue != null)
            {
                routeIdValue.stringValue = string.Empty;
            }

            contentIdProp.stringValue = contentId;
        }

        private static int FindLevelIndex(SerializedProperty levelsProp, string levelId)
        {
            for (int i = 0; i < levelsProp.arraySize; i++)
            {
                string currentLevelId = levelsProp.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("levelId")?.FindPropertyRelative("_value")?.stringValue;

                if (string.Equals(currentLevelId, levelId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int CountDuplicatedRoutes(SerializedProperty levelsProp)
        {
            var counters = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
            int duplicatedRoutes = 0;

            for (int i = 0; i < levelsProp.arraySize; i++)
            {
                SerializedProperty routeRefProp = levelsProp.GetArrayElementAtIndex(i).FindPropertyRelative("routeRef");
                var routeRef = routeRefProp?.objectReferenceValue as SceneRouteDefinitionAsset;
                if (routeRef == null || !routeRef.RouteId.IsValid)
                {
                    continue;
                }

                string routeId = routeRef.RouteId.ToString();
                counters.TryGetValue(routeId, out int count);
                count++;
                counters[routeId] = count;
                if (count == 2)
                {
                    duplicatedRoutes++;
                }
            }

            return duplicatedRoutes;
        }
    }
}
#endif
