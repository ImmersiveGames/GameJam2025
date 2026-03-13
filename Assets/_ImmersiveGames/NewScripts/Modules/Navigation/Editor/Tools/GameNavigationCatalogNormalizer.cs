#if UNITY_EDITOR
using System;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Editor.Tools
{
    public static class GameNavigationCatalogNormalizer
    {
        private const string NavigationCatalogPath = "Assets/Resources/Navigation/GameNavigationCatalog.asset";
        private const string SceneRouteCatalogPath = "Assets/Resources/SceneFlow/SceneRouteCatalog.asset";
        private const string FrontendStylePath = "Assets/Resources/SceneFlow/Styles/TransitionStyle_Frontend.asset";
        private const string GameplayStylePath = "Assets/Resources/SceneFlow/Styles/TransitionStyle_Gameplay.asset";
        private const string GameplayNoFadeStylePath = "Assets/Resources/SceneFlow/Styles/TransitionStyle_GameplayNoFade.asset";

        private const string IntentMenu = "to-menu";
        private const string IntentGameplay = "to-gameplay";

        [MenuItem("ImmersiveGames/NewScripts/QA/Navigation/Normalize Catalogs", priority = 1510)]
        public static void NormalizeNavigationCatalogs()
        {
            GameNavigationCatalogAsset navigationCatalog = LoadOrCreateNavigationCatalog();
            SceneRouteCatalogAsset sceneRouteCatalog = LoadRequiredSceneRouteCatalog();
            NormalizeNavigationCatalog(navigationCatalog, sceneRouteCatalog);
            EditorUtility.SetDirty(navigationCatalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OBS][SceneFlow][Config] Navigation catalog normalized successfully.");
        }

        private static GameNavigationCatalogAsset LoadOrCreateNavigationCatalog()
        {
            GameNavigationCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<GameNavigationCatalogAsset>(NavigationCatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            EnsureDirectoryForAsset(NavigationCatalogPath);
            catalog = ScriptableObject.CreateInstance<GameNavigationCatalogAsset>();
            catalog.name = "GameNavigationCatalog";
            AssetDatabase.CreateAsset(catalog, NavigationCatalogPath);
            return catalog;
        }

        private static SceneRouteCatalogAsset LoadRequiredSceneRouteCatalog()
        {
            SceneRouteCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<SceneRouteCatalogAsset>(SceneRouteCatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException($"[FATAL][Config] SceneRouteCatalogAsset obrigatorio ausente em '{SceneRouteCatalogPath}'.");
            }

            return catalog;
        }

        private static void NormalizeNavigationCatalog(GameNavigationCatalogAsset navigationCatalog, SceneRouteCatalogAsset sceneRouteCatalog)
        {
            SerializedObject navigationSo = new SerializedObject(navigationCatalog);
            SceneRouteDefinitionAsset menuRouteRef = GetRequiredRouteAssetOrFail(sceneRouteCatalog, IntentMenu);
            SceneRouteDefinitionAsset gameplayRouteRef = GetRequiredRouteAssetOrFail(sceneRouteCatalog, IntentGameplay);

            NormalizeCoreSlot(navigationSo, "menuSlot", menuRouteRef, LoadRequiredStyleAsset(FrontendStylePath), true);
            NormalizeCoreSlot(navigationSo, "gameplaySlot", gameplayRouteRef, LoadRequiredStyleAsset(GameplayStylePath), true);
            NormalizeCoreSlot(navigationSo, "gameOverSlot", menuRouteRef, LoadRequiredStyleAsset(GameplayNoFadeStylePath), false);
            NormalizeCoreSlot(navigationSo, "victorySlot", menuRouteRef, LoadRequiredStyleAsset(GameplayNoFadeStylePath), false);
            NormalizeCoreSlot(navigationSo, "restartSlot", gameplayRouteRef, LoadRequiredStyleAsset(GameplayNoFadeStylePath), false);
            NormalizeCoreSlot(navigationSo, "exitToMenuSlot", menuRouteRef, LoadRequiredStyleAsset(FrontendStylePath), false);
            navigationSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void NormalizeCoreSlot(SerializedObject navigationCatalogSo, string slotPropertyName, SceneRouteDefinitionAsset defaultRouteRef, TransitionStyleAsset defaultStyleRef, bool required)
        {
            SerializedProperty slot = navigationCatalogSo.FindProperty(slotPropertyName);
            SerializedProperty routeRef = slot?.FindPropertyRelative("routeRef");
            SerializedProperty transitionStyleRef = slot?.FindPropertyRelative("transitionStyleRef");
            if (slot == null || routeRef == null || transitionStyleRef == null)
            {
                throw new InvalidOperationException($"[FATAL][Config] Missing slot property '{slotPropertyName}' in GameNavigationCatalogAsset.");
            }

            if (routeRef.objectReferenceValue == null)
            {
                routeRef.objectReferenceValue = defaultRouteRef;
            }

            if (transitionStyleRef.objectReferenceValue == null)
            {
                transitionStyleRef.objectReferenceValue = defaultStyleRef;
            }

            if (required)
            {
                SceneRouteDefinitionAsset routeAsset = routeRef.objectReferenceValue as SceneRouteDefinitionAsset;
                if (routeAsset == null || !routeAsset.RouteId.IsValid)
                {
                    throw new InvalidOperationException($"[FATAL][Config] Required core slot '{slotPropertyName}' must resolve to a valid SceneRouteDefinitionAsset.");
                }

                if (transitionStyleRef.objectReferenceValue == null)
                {
                    throw new InvalidOperationException($"[FATAL][Config] Required core slot '{slotPropertyName}' must resolve to a valid TransitionStyleAsset.");
                }
            }
        }

        private static TransitionStyleAsset LoadRequiredStyleAsset(string assetPath)
        {
            TransitionStyleAsset styleAsset = AssetDatabase.LoadAssetAtPath<TransitionStyleAsset>(assetPath);
            if (styleAsset == null)
            {
                throw new InvalidOperationException($"[FATAL][Config] Missing canonical TransitionStyleAsset at '{assetPath}'.");
            }

            return styleAsset;
        }

        private static SceneRouteDefinitionAsset GetRequiredRouteAssetOrFail(SceneRouteCatalogAsset sceneRouteCatalog, string routeId)
        {
            SceneRouteId typedRouteId = SceneRouteId.FromName(routeId);
            if (!sceneRouteCatalog.TryGetAsset(typedRouteId, out SceneRouteDefinitionAsset routeAsset) || routeAsset == null)
            {
                throw new InvalidOperationException($"[FATAL][Config] Missing canonical route asset in SceneRouteCatalogAsset. routeId='{routeId}'.");
            }

            return routeAsset;
        }

        private static void EnsureDirectoryForAsset(string assetPath)
        {
            string directory = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory) || AssetDatabase.IsValidFolder(directory))
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
#endif
