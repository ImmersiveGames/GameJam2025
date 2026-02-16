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
            if (core == null)
            {
                FailFastEditor("[FATAL][Config] GameNavigationIntentCatalogAsset sem propriedade serializada 'core'.");
            }

            // Direct-ref-first: usa routeRef já configurado nos intents canônicos obrigatórios.
            SceneRouteDefinitionAsset menuRouteRef = GetRequiredCoreRouteRefOrFail(core, IntentMenu);
            SceneRouteDefinitionAsset gameplayRouteRef = GetRequiredCoreRouteRefOrFail(core, IntentGameplay);

            EnsureCoreIntentEntry(core, IntentMenu, menuRouteRef, StyleFrontend, criticalRequired: true);
            EnsureCoreIntentEntry(core, IntentGameplay, gameplayRouteRef, StyleGameplay, criticalRequired: true);

            // Extras opcionais: continuam mapeados por referência direta, reaproveitando refs canônicas já configuradas.
            EnsureCoreIntentEntry(core, IntentGameOverCanonical, menuRouteRef, StyleFrontend, criticalRequired: false);
            EnsureCoreIntentEntry(core, IntentDefeatAlias, menuRouteRef, StyleFrontend, criticalRequired: false);
            EnsureCoreIntentEntry(core, IntentVictory, menuRouteRef, StyleFrontend, criticalRequired: false);
            EnsureCoreIntentEntry(core, IntentRestart, gameplayRouteRef, StyleGameplay, criticalRequired: false);
            EnsureCoreIntentEntry(core, IntentExitToMenu, menuRouteRef, StyleFrontend, criticalRequired: false);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void NormalizeNavigationCatalog(
            GameNavigationCatalogAsset navigationCatalog,
            GameNavigationIntentCatalogAsset intentCatalog)
        {
            SerializedObject navigationSo = new SerializedObject(navigationCatalog);
            SetIntentCatalogReferenceOrFail(navigationSo, intentCatalog);

            SerializedObject intentSo = new SerializedObject(intentCatalog);
            SerializedProperty core = intentSo.FindProperty("core");
            if (core == null)
            {
                FailFastEditor("[FATAL][Config] GameNavigationIntentCatalogAsset sem propriedade serializada 'core'.");
            }

            SceneRouteDefinitionAsset menuRouteRef = GetRequiredCoreRouteRefOrFail(core, IntentMenu);
            SceneRouteDefinitionAsset gameplayRouteRef = GetRequiredCoreRouteRefOrFail(core, IntentGameplay);

            NormalizeCoreSlot(navigationSo, "menuSlot", menuRouteRef, StyleFrontend, required: true);
            NormalizeCoreSlot(navigationSo, "gameplaySlot", gameplayRouteRef, StyleGameplay, required: true);
            NormalizeCoreSlot(navigationSo, "gameOverSlot", menuRouteRef, StyleGameplayNoFade, required: false);
            NormalizeCoreSlot(navigationSo, "victorySlot", menuRouteRef, StyleGameplayNoFade, required: false);
            NormalizeCoreSlot(navigationSo, "restartSlot", gameplayRouteRef, StyleGameplayNoFade, required: false);
            NormalizeCoreSlot(navigationSo, "exitToMenuSlot", menuRouteRef, StyleFrontend, required: false);

            navigationSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetIntentCatalogReferenceOrFail(SerializedObject navigationSo, GameNavigationIntentCatalogAsset intentCatalog)
        {
            SerializedProperty intentCatalogProperty = navigationSo.FindProperty("intentCatalog") ?? navigationSo.FindProperty("assetRef");
            if (intentCatalogProperty == null)
            {
                FailFastEditor("[FATAL][Config] GameNavigationCatalogAsset sem propriedade serializada 'intentCatalog'/'assetRef'.");
            }

            intentCatalogProperty.objectReferenceValue = intentCatalog;
        }

        private static void NormalizeCoreSlot(
            SerializedObject navigationCatalogSo,
            string slotPropertyName,
            SceneRouteDefinitionAsset defaultRouteRef,
            string defaultStyleId,
            bool required)
        {
            SerializedProperty slot = navigationCatalogSo.FindProperty(slotPropertyName);
            if (slot == null)
            {
                FailFastEditor($"[FATAL][Config] Missing slot property '{slotPropertyName}' in GameNavigationCatalogAsset.");
            }

            SerializedProperty routeRef = slot.FindPropertyRelative("routeRef");
            SerializedProperty styleId = slot.FindPropertyRelative("styleId").FindPropertyRelative("_value");

            if (routeRef.objectReferenceValue == null)
            {
                if (defaultRouteRef == null)
                {
                    FailFastEditor(
                        $"[FATAL][Config] Slot '{slotPropertyName}' sem routeRef e sem defaultRouteRef (direct-ref-first). required={required}.");
                }

                routeRef.objectReferenceValue = defaultRouteRef;
            }

            if (string.IsNullOrWhiteSpace(styleId.stringValue))
            {
                styleId.stringValue = defaultStyleId;
            }
        }

        private static void EnsureCoreIntentEntry(
            SerializedProperty coreList,
            string intentId,
            SceneRouteDefinitionAsset routeRef,
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
            SerializedProperty serializedRouteRef = entry.FindPropertyRelative("routeRef");
            SerializedProperty serializedStyleId = entry.FindPropertyRelative("styleId").FindPropertyRelative("_value");
            SerializedProperty serializedCritical = entry.FindPropertyRelative("criticalRequired");

            serializedIntentId.stringValue = intentId;
            serializedCritical.boolValue = criticalRequired;

            if (routeRef == null)
            {
                FailFastEditor($"[FATAL][Config] Intent '{intentId}' sem routeRef para normalização direct-ref-first.");
            }

            serializedRouteRef.objectReferenceValue = routeRef;

            if (string.IsNullOrWhiteSpace(serializedStyleId.stringValue))
            {
                serializedStyleId.stringValue = styleId;
            }
        }

        private static SceneRouteDefinitionAsset GetRequiredCoreRouteRefOrFail(SerializedProperty coreList, string intentId)
        {
            int index = FindCoreIntentIndex(coreList, intentId);
            if (index < 0)
            {
                FailFastEditor(
                    $"[FATAL][Config] Intent core obrigatória '{intentId}' não encontrada no bloco core do GameNavigationIntentCatalogAsset. Configure routeRef direto antes de normalizar.");
            }

            SerializedProperty entry = coreList.GetArrayElementAtIndex(index);
            SerializedProperty routeRef = entry.FindPropertyRelative("routeRef");
            SceneRouteDefinitionAsset routeAsset = routeRef.objectReferenceValue as SceneRouteDefinitionAsset;
            if (routeAsset == null)
            {
                FailFastEditor(
                    $"[FATAL][Config] Intent core obrigatória '{intentId}' sem routeRef direto. Configure routeRef no GameNavigationIntentCatalogAsset antes de normalizar.");
            }

            return routeAsset;
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

        private static void FailFastEditor(string message)
        {
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }
    }
}
