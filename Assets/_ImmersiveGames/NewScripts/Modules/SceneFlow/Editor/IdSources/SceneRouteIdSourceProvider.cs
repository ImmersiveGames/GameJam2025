using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEditor;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources
{
    /// <summary>
    /// Coleta SceneRouteId a partir de RouteDefinitionAsset e SceneRouteCatalogAsset.
    /// </summary>
    public sealed class SceneRouteIdSourceProvider : ISceneFlowIdSourceProvider<SceneRouteId>
    {
        public SceneFlowIdSourceResult Collect()
        {
            var allValues = new HashSet<string>();
            var duplicates = new HashSet<string>();

            CollectFromRouteDefinitionAssets(allValues, duplicates);
            CollectFromRouteCatalogAssets(allValues, duplicates);

            return SceneFlowIdSourceUtility.BuildResult(allValues, duplicates);
        }

        private static void CollectFromRouteDefinitionAssets(HashSet<string> allValues, HashSet<string> duplicates)
        {
            // Comentário: aqui duplicidade é relevante (dois RouteDefinitionAsset com o mesmo routeId).
            var routeDefinitionIds = new HashSet<string>();

            string[] guids = AssetDatabase.FindAssets("t:SceneRouteDefinitionAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<SceneRouteDefinitionAsset>(path);
                if (asset == null)
                {
                    continue;
                }

                string routeId = asset.RouteId.Value;
                if (!SceneFlowIdSourceUtility.AddAndTrackDuplicate(routeDefinitionIds, duplicates, routeId))
                {
                    continue;
                }

                SceneFlowIdSourceUtility.AddValue(allValues, routeId);
            }
        }

        private static void CollectFromRouteCatalogAssets(HashSet<string> allValues, HashSet<string> duplicates)
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
                ReadRouteDefinitionReferences(serializedObject, allValues);
                ReadInlineRoutes(serializedObject, allValues, duplicates);
            }
        }

        private static void ReadRouteDefinitionReferences(SerializedObject serializedObject, HashSet<string> allValues)
        {
            // Comentário: sobreposição com RouteDefinitionAsset é esperada e NÃO deve gerar duplicidade.
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

                SceneFlowIdSourceUtility.AddValue(allValues, routeAsset.RouteId.Value);
            }
        }

        private static void ReadInlineRoutes(SerializedObject serializedObject, HashSet<string> allValues, HashSet<string> duplicates)
        {
            SerializedProperty routes = serializedObject.FindProperty("routes");
            if (routes == null || !routes.isArray)
            {
                return;
            }

            // Comentário: duplicidade de inline routes entre si continua relevante.
            var inlineRouteIds = new HashSet<string>();

            for (int i = 0; i < routes.arraySize; i++)
            {
                SerializedProperty routeEntry = routes.GetArrayElementAtIndex(i);
                SerializedProperty routeId = routeEntry.FindPropertyRelative("routeId");
                SerializedProperty raw = routeId?.FindPropertyRelative("_value");
                if (raw == null)
                {
                    continue;
                }

                string value = raw.stringValue;
                SceneFlowIdSourceUtility.AddAndTrackDuplicate(inlineRouteIds, duplicates, value);
                SceneFlowIdSourceUtility.AddValue(allValues, value);
            }
        }
    }
}
