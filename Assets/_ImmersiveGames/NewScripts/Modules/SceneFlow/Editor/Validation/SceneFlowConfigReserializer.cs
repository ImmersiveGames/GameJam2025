using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.Validation
{
    public static class SceneFlowConfigReserializer
    {
        private static readonly string[] CanonicalAssetPaths =
        {
            "Assets/Resources/GameNavigationIntentCatalog.asset",
            "Assets/Resources/Navigation/GameNavigationCatalog.asset",
            "Assets/Resources/SceneFlow/SceneRouteCatalog.asset",
            "Assets/Resources/Navigation/TransitionStyleCatalog.asset",
            "Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset",
            "Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset",
            "Assets/Resources/Navigation/LevelCatalog.asset",
            "Assets/Resources/RuntimeModeConfig.asset",
            "Assets/Resources/NewScriptsBootstrapConfig.asset"
        };

        [MenuItem("ImmersiveGames/NewScripts/Config/Reserialize SceneFlow Assets (DataCleanup v1)", priority = 3020)]
        public static void ReserializeDataCleanupV1Assets()
        {
            HashSet<string> allPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> canonicalPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> referencedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < CanonicalAssetPaths.Length; i++)
            {
                string canonicalPath = CanonicalAssetPaths[i];
                UnityEngine.Object canonicalAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(canonicalPath);
                if (canonicalAsset == null)
                {
                    Debug.LogWarning($"[WARN][Config] SceneFlow DataCleanup v1 reserialize: canonical asset not found at '{canonicalPath}'.");
                    continue;
                }

                AddPath(allPaths, canonicalPaths, referencedPaths, canonicalPath, isCanonical: true);
                CollectDirectReferences(canonicalAsset, allPaths, canonicalPaths, referencedPaths);
            }

            List<string> orderedPaths = new List<string>(allPaths);
            orderedPaths.Sort(StringComparer.OrdinalIgnoreCase);

            AssetDatabase.ForceReserializeAssets(orderedPaths);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[OBS][Config] SceneFlow DataCleanup v1 reserialize completed. " +
                $"assetsReserializedTotal={orderedPaths.Count}, canonicalCount={canonicalPaths.Count}, referencedCount={referencedPaths.Count}.");
        }

        private static void CollectDirectReferences(
            UnityEngine.Object canonicalAsset,
            ISet<string> allPaths,
            ISet<string> canonicalPaths,
            ISet<string> referencedPaths)
        {
            switch (canonicalAsset)
            {
                case SceneRouteCatalogAsset routeCatalog:
                    CollectRouteDefinitions(routeCatalog, allPaths, canonicalPaths, referencedPaths);
                    break;

                case TransitionStyleCatalogAsset styleCatalog:
                    CollectTransitionStyleProfiles(styleCatalog, allPaths, canonicalPaths, referencedPaths);
                    break;

                case SceneTransitionProfileCatalogAsset profileCatalog:
                    CollectProfileCatalogEntries(profileCatalog, allPaths, canonicalPaths, referencedPaths);
                    break;

                case GameNavigationCatalogAsset navigationCatalog:
                    CollectGameNavigationRouteRefs(navigationCatalog, allPaths, canonicalPaths, referencedPaths);
                    break;

                case GameNavigationIntentCatalogAsset intentCatalog:
                    CollectIntentCatalogRouteRefs(intentCatalog, allPaths, canonicalPaths, referencedPaths);
                    break;
            }
        }

        private static void CollectRouteDefinitions(
            SceneRouteCatalogAsset routeCatalog,
            ISet<string> allPaths,
            ISet<string> canonicalPaths,
            ISet<string> referencedPaths)
        {
            SerializedObject serializedObject = new SerializedObject(routeCatalog);
            SerializedProperty routeDefinitions = serializedObject.FindProperty("routeDefinitions");
            if (routeDefinitions == null || !routeDefinitions.isArray)
            {
                return;
            }

            for (int i = 0; i < routeDefinitions.arraySize; i++)
            {
                UnityEngine.Object routeDefinition = routeDefinitions.GetArrayElementAtIndex(i).objectReferenceValue;
                AddObjectReferencePath(routeDefinition, allPaths, canonicalPaths, referencedPaths);
            }
        }

        private static void CollectTransitionStyleProfiles(
            TransitionStyleCatalogAsset styleCatalog,
            ISet<string> allPaths,
            ISet<string> canonicalPaths,
            ISet<string> referencedPaths)
        {
            SerializedObject serializedObject = new SerializedObject(styleCatalog);
            SerializedProperty styles = serializedObject.FindProperty("styles");
            if (styles == null || !styles.isArray)
            {
                return;
            }

            for (int i = 0; i < styles.arraySize; i++)
            {
                SerializedProperty styleEntry = styles.GetArrayElementAtIndex(i);

                SerializedProperty profileRefProp = styleEntry.FindPropertyRelative("profileRef");
                if (profileRefProp == null)
                {
                    profileRefProp = styleEntry.FindPropertyRelative("transitionProfile");
                }

                UnityEngine.Object profileRef = profileRefProp != null ? profileRefProp.objectReferenceValue : null;
                AddObjectReferencePath(profileRef, allPaths, canonicalPaths, referencedPaths);
            }
        }

        private static void CollectProfileCatalogEntries(
            SceneTransitionProfileCatalogAsset profileCatalog,
            ISet<string> allPaths,
            ISet<string> canonicalPaths,
            ISet<string> referencedPaths)
        {
            SerializedObject serializedObject = new SerializedObject(profileCatalog);
            SerializedProperty entries = serializedObject.FindProperty("_entries");
            if (entries == null || !entries.isArray)
            {
                return;
            }

            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                SerializedProperty profileProp = entry.FindPropertyRelative("_profile");
                UnityEngine.Object profileRef = profileProp != null ? profileProp.objectReferenceValue : null;
                AddObjectReferencePath(profileRef, allPaths, canonicalPaths, referencedPaths);
            }
        }

        private static void CollectGameNavigationRouteRefs(
            GameNavigationCatalogAsset navigationCatalog,
            ISet<string> allPaths,
            ISet<string> canonicalPaths,
            ISet<string> referencedPaths)
        {
            SerializedObject serializedObject = new SerializedObject(navigationCatalog);

            string[] coreSlotPropertyNames =
            {
                "menuSlot",
                "gameplaySlot",
                "gameOverSlot",
                "victorySlot",
                "restartSlot",
                "exitToMenuSlot"
            };

            for (int i = 0; i < coreSlotPropertyNames.Length; i++)
            {
                SerializedProperty slot = serializedObject.FindProperty(coreSlotPropertyNames[i]);
                SerializedProperty routeRef = slot?.FindPropertyRelative("routeRef");
                AddObjectReferencePath(routeRef?.objectReferenceValue, allPaths, canonicalPaths, referencedPaths);
            }

            SerializedProperty extraRoutes = serializedObject.FindProperty("routes");
            if (extraRoutes == null || !extraRoutes.isArray)
            {
                return;
            }

            for (int i = 0; i < extraRoutes.arraySize; i++)
            {
                SerializedProperty routeEntry = extraRoutes.GetArrayElementAtIndex(i);
                SerializedProperty routeRef = routeEntry.FindPropertyRelative("routeRef");
                AddObjectReferencePath(routeRef?.objectReferenceValue, allPaths, canonicalPaths, referencedPaths);
            }
        }

        private static void CollectIntentCatalogRouteRefs(
            GameNavigationIntentCatalogAsset intentCatalog,
            ISet<string> allPaths,
            ISet<string> canonicalPaths,
            ISet<string> referencedPaths)
        {
            SerializedObject serializedObject = new SerializedObject(intentCatalog);
            CollectIntentRouteRefsFromBlock(serializedObject.FindProperty("core"), allPaths, canonicalPaths, referencedPaths);
            CollectIntentRouteRefsFromBlock(serializedObject.FindProperty("custom"), allPaths, canonicalPaths, referencedPaths);
        }

        private static void CollectIntentRouteRefsFromBlock(
            SerializedProperty block,
            ISet<string> allPaths,
            ISet<string> canonicalPaths,
            ISet<string> referencedPaths)
        {
            if (block == null || !block.isArray)
            {
                return;
            }

            for (int i = 0; i < block.arraySize; i++)
            {
                SerializedProperty entry = block.GetArrayElementAtIndex(i);
                SerializedProperty routeRef = entry.FindPropertyRelative("routeRef");
                AddObjectReferencePath(routeRef?.objectReferenceValue, allPaths, canonicalPaths, referencedPaths);
            }
        }

        private static void AddObjectReferencePath(
            UnityEngine.Object reference,
            ISet<string> allPaths,
            ISet<string> canonicalPaths,
            ISet<string> referencedPaths)
        {
            if (reference == null)
            {
                return;
            }

            string path = AssetDatabase.GetAssetPath(reference);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            AddPath(allPaths, canonicalPaths, referencedPaths, path, isCanonical: false);
        }

        private static void AddPath(
            ISet<string> allPaths,
            ISet<string> canonicalPaths,
            ISet<string> referencedPaths,
            string path,
            bool isCanonical)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            bool added = allPaths.Add(path);
            if (!added)
            {
                return;
            }

            if (isCanonical)
            {
                canonicalPaths.Add(path);
                return;
            }

            referencedPaths.Add(path);
        }
    }
}
