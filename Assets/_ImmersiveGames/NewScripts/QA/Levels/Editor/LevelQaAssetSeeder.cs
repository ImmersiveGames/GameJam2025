// Assets/_ImmersiveGames/NewScripts/QA/Levels/Editor/LevelQaAssetSeeder.cs
// Utilitário Editor-only para semear assets mínimos de LevelCatalog.

#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Catalogs;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Definitions;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Levels.Editor
{
    public static class LevelQaAssetSeeder
    {
        private const string MenuItemPath = "Tools/NewScripts/QA/Level/Seed LevelCatalog Assets (Resources)";

        private const string ResourcesLevelsFolder = "Assets/Resources/Levels";
        private const string DefinitionsFolder = "Assets/Resources/Levels/Definitions";
        private const string CatalogAssetPath = "Assets/Resources/Levels/LevelCatalog.asset";

        private const string Level1Id = "level.1";
        private const string Level2Id = "level.2";
        private const string Content1Id = "content.1";
        private const string Content2Id = "content.2";
        private const string Signature1 = "sig.content.1";
        private const string Signature2 = "sig.content.2";

        [MenuItem(MenuItemPath)]
        private static void SeedLevelCatalogAssets()
        {
            EnsureFolder(ResourcesLevelsFolder);
            EnsureFolder(DefinitionsFolder);

            var catalog = LoadOrCreateCatalog();
            var definition1 = LoadOrCreateDefinition(Level1Id, Content1Id, Signature1);
            var definition2 = LoadOrCreateDefinition(Level2Id, Content2Id, Signature2);

            bool updatedCatalog = false;

            if (string.IsNullOrWhiteSpace(catalog.InitialLevelId))
            {
                SetSerializedString(catalog, "initialLevelId", Level1Id);
                updatedCatalog = true;
            }

            if (catalog.OrderedLevels == null || catalog.OrderedLevels.Count == 0)
            {
                SetSerializedStringList(catalog, "orderedLevels", new List<string> { Level1Id, Level2Id });
                updatedCatalog = true;
            }

            if (catalog.Definitions == null || catalog.Definitions.Count == 0)
            {
                SetSerializedObjectList(catalog, "definitions", new List<LevelDefinition> { definition1, definition2 });
                updatedCatalog = true;
            }

            if (updatedCatalog)
            {
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = catalog;

            DebugUtility.Log(typeof(LevelQaAssetSeeder),
                $"[QA][Level] Seed concluído. catalogUpdated='{updatedCatalog}' catalog='{AssetDatabase.GetAssetPath(catalog)}'.",
                DebugUtility.Colors.Info);
        }

        private static LevelCatalog LoadOrCreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogAssetPath);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            EditorUtility.SetDirty(catalog);

            DebugUtility.Log(typeof(LevelQaAssetSeeder),
                $"[QA][Level] LevelCatalog criado em '{CatalogAssetPath}'.",
                DebugUtility.Colors.Info);

            return catalog;
        }

        private static LevelDefinition LoadOrCreateDefinition(string levelId, string contentId, string signature)
        {
            var assetName = $"LevelDefinition_{levelId}";
            var path = $"{DefinitionsFolder}/{assetName}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (definition != null)
            {
                return definition;
            }

            definition = ScriptableObject.CreateInstance<LevelDefinition>();
            SetSerializedString(definition, "levelId", levelId);
            SetSerializedString(definition, "contentId", contentId);
            SetSerializedString(definition, "contentSignature", signature);

            AssetDatabase.CreateAsset(definition, path);
            EditorUtility.SetDirty(definition);

            DebugUtility.Log(typeof(LevelQaAssetSeeder),
                $"[QA][Level] LevelDefinition criado em '{path}' (levelId='{levelId}').",
                DebugUtility.Colors.Info);

            return definition;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folderName = System.IO.Path.GetFileName(path);

            if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            if (!string.IsNullOrWhiteSpace(parent))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void SetSerializedString(Object target, string propertyName, string value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerializedStringList(Object target, string propertyName, List<string> values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return;
            }

            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).stringValue = values[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedObjectList(Object target, string propertyName, List<LevelDefinition> values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return;
            }

            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
