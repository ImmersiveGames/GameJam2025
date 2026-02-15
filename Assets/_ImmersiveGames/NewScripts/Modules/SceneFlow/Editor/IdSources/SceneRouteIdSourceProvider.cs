using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources
{
    /// <summary>
    /// Coleta SceneRouteId a partir de RouteDefinitionAsset e SceneRouteCatalogAsset.
    /// </summary>
    public sealed class SceneRouteIdSourceProvider : ISceneFlowIdSourceProvider<SceneRouteId>
    {
        public SceneFlowIdSourceResult Collect()
        {
            var values = new HashSet<string>();
            var duplicates = new HashSet<string>();

            CollectFromRouteDefinitionAssets(values, duplicates);
            CollectFromRouteCatalogAssets(values, duplicates);

            return SceneFlowIdSourceUtility.BuildResult(values, duplicates);
        }

        private static void CollectFromRouteDefinitionAssets(HashSet<string> values, HashSet<string> duplicates)
        {
            string[] guids = AssetDatabase.FindAssets("t:SceneRouteDefinitionAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<SceneRouteDefinitionAsset>(path);
                if (asset == null)
                {
                    continue;
                }

                SceneFlowIdSourceUtility.AddAndTrackDuplicate(values, duplicates, asset.RouteId.Value);
            }
        }

        private static void CollectFromRouteCatalogAssets(HashSet<string> values, HashSet<string> duplicates)
        {
            string[] guids = AssetDatabase.FindAssets("t:SceneRouteCatalogAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<SceneRouteCatalogAsset>(path);
                if (catalog == null)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(catalog);
                ReadRouteDefinitionReferences(serializedObject, values, duplicates);
                ReadInlineRoutes(serializedObject, values, duplicates);
            }
        }

        private static void ReadRouteDefinitionReferences(SerializedObject serializedObject, HashSet<string> values, HashSet<string> duplicates)
        {
            SerializedProperty definitions = serializedObject.FindProperty("routeDefinitions");
            if (definitions == null || !definitions.isArray)
            {
                return;
            }

            for (int i = 0; i < definitions.arraySize; i++)
            {
                SerializedProperty element = definitions.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue is not SceneRouteDefinitionAsset routeAsset)
                {
                    continue;
                }

                SceneFlowIdSourceUtility.AddAndTrackDuplicate(values, duplicates, routeAsset.RouteId.Value);
            }
        }

        private static void ReadInlineRoutes(SerializedObject serializedObject, HashSet<string> values, HashSet<string> duplicates)
        {
            SerializedProperty routes = serializedObject.FindProperty("routes");
            if (routes == null || !routes.isArray)
            {
                return;
            }

            for (int i = 0; i < routes.arraySize; i++)
            {
                SerializedProperty routeEntry = routes.GetArrayElementAtIndex(i);
                SerializedProperty routeId = routeEntry.FindPropertyRelative("routeId");
                SerializedProperty raw = routeId?.FindPropertyRelative("_value");
                if (raw == null)
                {
                    continue;
                }

                SceneFlowIdSourceUtility.AddAndTrackDuplicate(values, duplicates, raw.stringValue);
            }
        }
    }
}
