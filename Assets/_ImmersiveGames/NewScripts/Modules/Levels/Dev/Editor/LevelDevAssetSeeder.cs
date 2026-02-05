#nullable enable
#if UNITY_EDITOR
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Levels.Catalogs;
using _ImmersiveGames.NewScripts.Modules.Levels.Definitions;
using UnityEditor;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Levels.Dev.Editor
{
    public static class LevelDevAssetSeeder
    {
        private const string MenuItemPath = "Tools/NewScripts/QA/Level/Seed LevelCatalog Assets (Resources)";

        private const string NewResourcesRoot = "Assets/Resources/NewScripts/Config";
        private const string NewDefinitionsFolder = "Assets/Resources/NewScripts/Config/Levels/Definitions";
        private const string NewCatalogAssetPath = "Assets/Resources/NewScripts/Config/LevelCatalog.asset";

        private const string LegacyResourcesRoot = "Assets/Resources/Levels";
        private const string LegacyDefinitionsFolder = "Assets/Resources/Levels/Definitions";
        private const string LegacyCatalogAssetPath = "Assets/Resources/Levels/LevelCatalog.asset";

        private const string Level1Id = "level.1";
        private const string Level2Id = "level.2";
        private const string Content1Id = "content.1";
        private const string Content2Id = "content.2";
        private const string Signature1 = "sig.content.1";
        private const string Signature2 = "sig.content.2";

        [MenuItem(MenuItemPath)]
        private static void SeedLevelCatalogAssets()
        {
            EnsureFolder(NewResourcesRoot);
            EnsureFolder(NewDefinitionsFolder);

            var newCatalog = LoadOrCreateCatalog(NewCatalogAssetPath, "novo");
            var newDefinition1 = LoadOrCreateDefinition(NewDefinitionsFolder, Level1Id, Content1Id, Signature1, "novo");
            var newDefinition2 = LoadOrCreateDefinition(NewDefinitionsFolder, Level2Id, Content2Id, Signature2, "novo");

            bool updatedNewCatalog = EnsureCatalogData(newCatalog, newDefinition1, newDefinition2, "novo");

            var legacyCatalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LegacyCatalogAssetPath);
            if (legacyCatalog == null)
            {
                EnsureFolder(LegacyResourcesRoot);
                EnsureFolder(LegacyDefinitionsFolder);

                legacyCatalog = LoadOrCreateCatalog(LegacyCatalogAssetPath, "legado");
                var legacyDefinition1 = LoadOrCreateDefinition(LegacyDefinitionsFolder, Level1Id, Content1Id, Signature1, "legado");
                var legacyDefinition2 = LoadOrCreateDefinition(LegacyDefinitionsFolder, Level2Id, Content2Id, Signature2, "legado");

                EnsureCatalogData(legacyCatalog, legacyDefinition1, legacyDefinition2, "legado");
            }
            else
            {
                DebugUtility.Log(typeof(LevelDevAssetSeeder),
                    $"[QA][Level] LevelCatalog legado encontrado em '{LegacyCatalogAssetPath}'.",
                    DebugUtility.Colors.Info);
            }

            if (updatedNewCatalog)
            {
                EditorUtility.SetDirty(newCatalog);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = newCatalog;

            DebugUtility.Log(typeof(LevelDevAssetSeeder),
                $"[QA][Level] Seed concluído. catalogUpdated='{updatedNewCatalog}' catalog='{AssetDatabase.GetAssetPath(newCatalog)}'.",
                DebugUtility.Colors.Info);
        }

        private static LevelCatalog LoadOrCreateCatalog(string catalogAssetPath, string scopeLabel)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(catalogAssetPath);
            if (catalog != null)
            {
                DebugUtility.Log(typeof(LevelDevAssetSeeder),
                    $"[QA][Level] LevelCatalog {scopeLabel} encontrado em '{catalogAssetPath}'.",
                    DebugUtility.Colors.Info);
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogAssetPath);
            EditorUtility.SetDirty(catalog);

            DebugUtility.Log(typeof(LevelDevAssetSeeder),
                $"[QA][Level] LevelCatalog {scopeLabel} criado em '{catalogAssetPath}'.",
                DebugUtility.Colors.Info);

            return catalog;
        }

        private static LevelDefinition LoadOrCreateDefinition(
            string definitionsFolder,
            string levelId,
            string contentId,
            string signature,
            string scopeLabel)
        {
            string assetName = $"LevelDefinition_{levelId}";
            string path = $"{definitionsFolder}/{assetName}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (definition != null)
            {
                DebugUtility.Log(typeof(LevelDevAssetSeeder),
                    $"[QA][Level] LevelDefinition {scopeLabel} encontrado em '{path}' (levelId='{levelId}').",
                    DebugUtility.Colors.Info);
                return definition;
            }

            definition = ScriptableObject.CreateInstance<LevelDefinition>();
            SetSerializedString(definition, "levelId", levelId);
            SetSerializedString(definition, "contentId", contentId);
            SetSerializedString(definition, "contentSignature", signature);

            AssetDatabase.CreateAsset(definition, path);
            EditorUtility.SetDirty(definition);

            DebugUtility.Log(typeof(LevelDevAssetSeeder),
                $"[QA][Level] LevelDefinition {scopeLabel} criado em '{path}' (levelId='{levelId}').",
                DebugUtility.Colors.Info);

            return definition;
        }

        private static bool EnsureCatalogData(
            LevelCatalog catalog,
            LevelDefinition definition1,
            LevelDefinition definition2,
            string scopeLabel)
        {
            bool updated = false;

            if (string.IsNullOrWhiteSpace(catalog.InitialLevelId))
            {
                SetSerializedString(catalog, "initialLevelId", Level1Id);
                updated = true;
            }

            var orderedLevels = new List<string>();
            if (catalog.OrderedLevels != null)
            {
                orderedLevels.AddRange(catalog.OrderedLevels);
            }

            EnsureListContains(orderedLevels, Level1Id);
            EnsureListContains(orderedLevels, Level2Id);

            if (orderedLevels.Count > 0)
            {
                SetSerializedStringList(catalog, "orderedLevels", orderedLevels);
                updated = true;
            }

            var definitions = new List<LevelDefinition>();
            if (catalog.Definitions != null)
            {
                definitions.AddRange(catalog.Definitions);
            }

            EnsureDefinitionContains(definitions, definition1);
            EnsureDefinitionContains(definitions, definition2);

            if (definitions.Count > 0)
            {
                SetSerializedObjectList(catalog, "definitions", definitions);
                updated = true;
            }

            if (updated)
            {
                DebugUtility.Log(typeof(LevelDevAssetSeeder),
                    $"[QA][Level] LevelCatalog {scopeLabel} atualizado com dados mínimos.",
                    DebugUtility.Colors.Success);
            }

            return updated;
        }

        private static void EnsureListContains(List<string> list, string value)
        {
            if (list.Exists(entry => string.Equals(entry, value, System.StringComparison.Ordinal)))
            {
                return;
            }

            list.Add(value);
        }

        private static void EnsureDefinitionContains(List<LevelDefinition> list, LevelDefinition definition)
        {
            foreach (var existing in list)
            {
                if (existing == null)
                {
                    continue;
                }

                if (string.Equals(existing.LevelId, definition.LevelId, System.StringComparison.Ordinal))
                {
                    return;
                }
            }

            list.Add(definition);
        }

        private static void EnsureFolder(string? path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string? parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            string? folderName = System.IO.Path.GetFileName(path);

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
