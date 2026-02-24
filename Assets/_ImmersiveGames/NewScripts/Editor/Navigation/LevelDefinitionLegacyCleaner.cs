#if UNITY_EDITOR
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Bindings;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Editor.Navigation
{
    public static class LevelDefinitionLegacyCleaner
    {
        private const int MaxListedAssets = 8;

        [MenuItem("Tools/NewScripts/Navigation/Clear Legacy Scene Data in LevelDefinitions")]
        private static void ClearLegacySceneData()
        {
            string[] catalogGuids = AssetDatabase.FindAssets("t:LevelCatalogAsset");

            int scanned = 0;
            int changed = 0;
            var changedAssets = new List<string>(MaxListedAssets);

            for (int i = 0; i < catalogGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(catalogGuids[i]);
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                var catalogAsset = AssetDatabase.LoadAssetAtPath<LevelCatalogAsset>(assetPath);
                if (catalogAsset == null)
                {
                    continue;
                }

                scanned++;

                var serializedObject = new SerializedObject(catalogAsset);
                var levelsProperty = serializedObject.FindProperty("levels");
                if (levelsProperty == null || !levelsProperty.isArray)
                {
                    continue;
                }

                bool assetChanged = false;

                for (int levelIndex = 0; levelIndex < levelsProperty.arraySize; levelIndex++)
                {
                    var levelProperty = levelsProperty.GetArrayElementAtIndex(levelIndex);
                    if (levelProperty == null)
                    {
                        continue;
                    }

                    assetChanged |= ClearLegacyArray(levelProperty.FindPropertyRelative("scenesToLoad"));
                    assetChanged |= ClearLegacyArray(levelProperty.FindPropertyRelative("scenesToUnload"));
                    assetChanged |= ClearLegacyString(levelProperty.FindPropertyRelative("targetActiveScene"));
                }

                if (!assetChanged)
                {
                    continue;
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalogAsset);
                changed++;

                if (changedAssets.Count < MaxListedAssets)
                {
                    changedAssets.Add(assetPath);
                }
            }

            AssetDatabase.SaveAssets();

            string listed = changedAssets.Count > 0
                ? string.Join(", ", changedAssets)
                : "none";
            Debug.Log(
                $"[OBS][Navigation] Legacy Scene Data cleanup concluído. scanned={scanned}, changed={changed}, listed={listed}. " +
                "Menu: Tools/NewScripts/Navigation/Clear Legacy Scene Data in LevelDefinitions");
        }

        private static bool ClearLegacyArray(SerializedProperty property)
        {
            if (property == null || !property.isArray)
            {
                return false;
            }

            if (property.arraySize == 0)
            {
                return false;
            }

            property.ClearArray();
            return true;
        }

        private static bool ClearLegacyString(SerializedProperty property)
        {
            if (property == null || property.propertyType != SerializedPropertyType.String)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(property.stringValue))
            {
                return false;
            }

            property.stringValue = string.Empty;
            return true;
        }
    }
}
#endif
