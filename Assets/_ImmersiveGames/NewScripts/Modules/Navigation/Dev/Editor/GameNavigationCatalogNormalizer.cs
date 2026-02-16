using System;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Dev.Editor
{
    /// <summary>
    /// Ferramenta de configuração manual (Editor-only) para normalizar os catálogos de navegação.
    /// Não executa em runtime.
    /// </summary>
    public static class GameNavigationCatalogNormalizer
    {
        private const string IntentCatalogPath = "Assets/Resources/GameNavigationIntentCatalog.asset";
        private const string NavigationCatalogPath = "Assets/Resources/Navigation/GameNavigationCatalog.asset";

        private const string IntentMenu = "to-menu";
        private const string IntentGameplay = "to-gameplay";
        private const string IntentGameOverCanonical = "gameover";
        private const string IntentDefeatAlias = "defeat";
        private const string IntentVictory = "victory";
        private const string IntentRestart = "restart";
        private const string IntentExitToMenu = "exit-to-menu";

        private const string StyleFrontend = "style.frontend";
        private const string StyleGameplay = "style.gameplay";
        private const string StyleGameplayNoFade = "style.gameplay.nofade";

        [MenuItem("ImmersiveGames/NewScripts/Config/Normalize Navigation Catalogs", priority = 3000)]
        public static void NormalizeNavigationCatalogs()
        {
            try
            {
                GameNavigationIntentCatalogAsset intentCatalog = LoadOrCreateIntentCatalog();
                GameNavigationCatalogAsset navigationCatalog = LoadOrCreateNavigationCatalog();

                NormalizeIntentCatalog(intentCatalog);
                NormalizeNavigationCatalog(navigationCatalog, intentCatalog);

                EditorUtility.SetDirty(intentCatalog);
                EditorUtility.SetDirty(navigationCatalog);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("[OBS][SceneFlow][Config] Navigation catalogs normalized successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FATAL][Config] Failed to normalize navigation catalogs. reason='{ex.Message}'.");
                throw;
            }
        }

        private static GameNavigationIntentCatalogAsset LoadOrCreateIntentCatalog()
        {
            GameNavigationIntentCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<GameNavigationIntentCatalogAsset>(IntentCatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            EnsureDirectoryForAsset(IntentCatalogPath);
            catalog = ScriptableObject.CreateInstance<GameNavigationIntentCatalogAsset>();
            catalog.name = "GameNavigationIntentCatalog";
            AssetDatabase.CreateAsset(catalog, IntentCatalogPath);

            Debug.Log($"[OBS][SceneFlow][Config] Created missing intent catalog at '{IntentCatalogPath}'.");
            return catalog;
        }

        private static GameNavigationCatalogAsset LoadOrCreateNavigationCatalog()
        {
            GameNavigationCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<GameNavigationCatalogAsset>(NavigationCatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            string[] guids = AssetDatabase.FindAssets("t:GameNavigationCatalogAsset");
            if (guids.Length > 0)
            {
                string anyPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                catalog = AssetDatabase.LoadAssetAtPath<GameNavigationCatalogAsset>(anyPath);
                if (catalog != null)
                {
                    Debug.Log($"[OBS][SceneFlow][Config] Using existing navigation catalog found at '{anyPath}'.");
                    return catalog;
                }
            }

            EnsureDirectoryForAsset(NavigationCatalogPath);
            catalog = ScriptableObject.CreateInstance<GameNavigationCatalogAsset>();
            catalog.name = "GameNavigationCatalog";
            AssetDatabase.CreateAsset(catalog, NavigationCatalogPath);

            Debug.Log($"[OBS][SceneFlow][Config] Created missing navigation catalog at '{NavigationCatalogPath}'.");
            return catalog;
        }

        private static void NormalizeIntentCatalog(GameNavigationIntentCatalogAsset intentCatalog)
        {
            SerializedObject so = new SerializedObject(intentCatalog);
            SerializedProperty core = so.FindProperty("core");

            EnsureCoreIntentEntry(core, IntentMenu, routeIntent: IntentMenu, styleId: StyleFrontend, criticalRequired: true);
            EnsureCoreIntentEntry(core, IntentGameplay, routeIntent: IntentGameplay, styleId: StyleGameplay, criticalRequired: true);

            // Mantém o id canônico atual do projeto para GameOver e também garante alias legado/uso gameplay.
            EnsureCoreIntentEntry(core, IntentGameOverCanonical, routeIntent: IntentMenu, styleId: StyleFrontend, criticalRequired: false);
            EnsureCoreIntentEntry(core, IntentDefeatAlias, routeIntent: IntentMenu, styleId: StyleFrontend, criticalRequired: false);

            EnsureCoreIntentEntry(core, IntentVictory, routeIntent: IntentMenu, styleId: StyleFrontend, criticalRequired: false);
            EnsureCoreIntentEntry(core, IntentRestart, routeIntent: IntentGameplay, styleId: StyleGameplay, criticalRequired: false);
            EnsureCoreIntentEntry(core, IntentExitToMenu, routeIntent: IntentMenu, styleId: StyleFrontend, criticalRequired: false);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void NormalizeNavigationCatalog(
            GameNavigationCatalogAsset navigationCatalog,
            GameNavigationIntentCatalogAsset intentCatalog)
        {
            SerializedObject so = new SerializedObject(navigationCatalog);

            SerializedProperty assetRef = so.FindProperty("assetRef");
            assetRef.objectReferenceValue = intentCatalog;

            NormalizeCoreSlot(so, "menuSlot", IntentMenu, StyleFrontend, required: true);
            NormalizeCoreSlot(so, "gameplaySlot", IntentGameplay, StyleGameplay, required: true);
            NormalizeCoreSlot(so, "gameOverSlot", IntentMenu, StyleGameplayNoFade, required: false);
            NormalizeCoreSlot(so, "victorySlot", IntentMenu, StyleGameplayNoFade, required: false);
            NormalizeCoreSlot(so, "restartSlot", IntentGameplay, StyleGameplayNoFade, required: false);
            NormalizeCoreSlot(so, "exitToMenuSlot", IntentMenu, StyleFrontend, required: false);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void NormalizeCoreSlot(
            SerializedObject navigationCatalogSo,
            string slotPropertyName,
            string defaultRouteIntent,
            string defaultStyleId,
            bool required)
        {
            SerializedProperty slot = navigationCatalogSo.FindProperty(slotPropertyName);
            if (slot == null)
            {
                if (required)
                {
                    throw new InvalidOperationException($"Missing slot property '{slotPropertyName}' in GameNavigationCatalogAsset.");
                }

                Debug.LogWarning($"[WARN][SceneFlow][Config] Optional slot property '{slotPropertyName}' not found.");
                return;
            }

            SerializedProperty routeRef = slot.FindPropertyRelative("routeRef");
            SerializedProperty styleId = slot.FindPropertyRelative("styleId").FindPropertyRelative("_value");

            if (routeRef.objectReferenceValue == null)
            {
                SceneRouteDefinitionAsset routeAsset = FindRouteByIntent(defaultRouteIntent);
                if (routeAsset != null)
                {
                    routeRef.objectReferenceValue = routeAsset;
                }
                else if (required)
                {
                    throw new InvalidOperationException(
                        $"Required core slot '{slotPropertyName}' is missing routeRef and no route asset was found for intent '{defaultRouteIntent}'.");
                }
                else
                {
                    Debug.LogWarning(
                        $"[WARN][SceneFlow][Config] Optional core slot '{slotPropertyName}' left without routeRef (route '{defaultRouteIntent}' not found).");
                }
            }

            if (string.IsNullOrWhiteSpace(styleId.stringValue))
            {
                if (required)
                {
                    styleId.stringValue = defaultStyleId;
                }
                else
                {
                    styleId.stringValue = defaultStyleId;
                    Debug.Log($"[OBS][SceneFlow][Config] Optional core slot '{slotPropertyName}' received default style '{defaultStyleId}'.");
                }
            }
        }

        private static void EnsureCoreIntentEntry(
            SerializedProperty coreList,
            string intentId,
            string routeIntent,
            string styleId,
            bool criticalRequired)
        {
            int index = FindCoreIntentIndex(coreList, intentId);
            if (index < 0)
            {
                index = coreList.arraySize;
                coreList.InsertArrayElementAtIndex(index);
            }

            SerializedProperty entry = coreList.GetArrayElementAtIndex(index);
            SerializedProperty serializedIntentId = entry.FindPropertyRelative("intentId").FindPropertyRelative("_value");
            SerializedProperty routeRef = entry.FindPropertyRelative("routeRef");
            SerializedProperty serializedStyleId = entry.FindPropertyRelative("styleId").FindPropertyRelative("_value");
            SerializedProperty serializedCritical = entry.FindPropertyRelative("criticalRequired");

            serializedIntentId.stringValue = intentId;
            serializedCritical.boolValue = criticalRequired;

            if (routeRef.objectReferenceValue == null)
            {
                routeRef.objectReferenceValue = FindRouteByIntent(routeIntent);
            }

            if (string.IsNullOrWhiteSpace(serializedStyleId.stringValue))
            {
                serializedStyleId.stringValue = styleId;
            }
        }

        private static int FindCoreIntentIndex(SerializedProperty coreList, string intentId)
        {
            for (int i = 0; i < coreList.arraySize; i++)
            {
                SerializedProperty entry = coreList.GetArrayElementAtIndex(i);
                SerializedProperty entryIntentId = entry.FindPropertyRelative("intentId").FindPropertyRelative("_value");
                if (string.Equals(entryIntentId.stringValue, intentId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static SceneRouteDefinitionAsset FindRouteByIntent(string intentId)
        {
            string[] guids = AssetDatabase.FindAssets("t:SceneRouteDefinitionAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                SceneRouteDefinitionAsset route = AssetDatabase.LoadAssetAtPath<SceneRouteDefinitionAsset>(path);
                if (route == null)
                {
                    continue;
                }

                if (string.Equals(route.RouteId.Value, intentId, StringComparison.OrdinalIgnoreCase))
                {
                    return route;
                }
            }

            return null;
        }

        private static void EnsureDirectoryForAsset(string assetPath)
        {
            int slash = assetPath.LastIndexOf('/');
            if (slash <= 0)
            {
                return;
            }

            string directory = assetPath.Substring(0, slash);
            if (AssetDatabase.IsValidFolder(directory))
            {
                return;
            }

            string[] parts = directory.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
