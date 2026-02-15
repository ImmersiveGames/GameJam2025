using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using UnityEditor;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources
{
    /// <summary>
    /// Coleta SceneFlowProfileId a partir do conjunto canônico + catálogos existentes.
    /// </summary>
    public sealed class SceneFlowProfileIdSourceProvider : ISceneFlowIdSourceProvider<SceneFlowProfileId>
    {
        public SceneFlowIdSourceResult Collect()
        {
            var allValues = new HashSet<string>();
            var duplicates = new HashSet<string>();

            AddCanonicalProfiles(allValues);
            CollectFromTransitionStyleCatalog(allValues);
            CollectFromTransitionProfileCatalog(allValues, duplicates);

            return SceneFlowIdSourceUtility.BuildResult(allValues, duplicates);
        }

        private static void AddCanonicalProfiles(HashSet<string> allValues)
        {
            // Comentário: canônicos podem sobrepor catálogos; isso é esperado.
            SceneFlowIdSourceUtility.AddValue(allValues, SceneFlowProfileId.Startup.Value);
            SceneFlowIdSourceUtility.AddValue(allValues, SceneFlowProfileId.Frontend.Value);
            SceneFlowIdSourceUtility.AddValue(allValues, SceneFlowProfileId.Gameplay.Value);
        }

        private static void CollectFromTransitionStyleCatalog(HashSet<string> allValues)
        {
            // Comentário: estilos e catálogo de profiles podem compartilhar os mesmos IDs.
            string[] guids = AssetDatabase.FindAssets("t:TransitionStyleCatalogAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<TransitionStyleCatalogAsset>(path);
                if (catalog == null)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(catalog);
                SerializedProperty styles = serializedObject.FindProperty("styles");
                if (styles == null || !styles.isArray)
                {
                    continue;
                }

                for (int j = 0; j < styles.arraySize; j++)
                {
                    SerializedProperty styleEntry = styles.GetArrayElementAtIndex(j);
                    SerializedProperty profileId = styleEntry.FindPropertyRelative("profileId");
                    SerializedProperty raw = profileId?.FindPropertyRelative("_value");
                    if (raw == null)
                    {
                        continue;
                    }

                    SceneFlowIdSourceUtility.AddValue(allValues, raw.stringValue);
                }
            }
        }

        private static void CollectFromTransitionProfileCatalog(HashSet<string> allValues, HashSet<string> duplicates)
        {
            string[] guids = AssetDatabase.FindAssets("t:SceneTransitionProfileCatalogAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<SceneTransitionProfileCatalogAsset>(path);
                if (catalog == null)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(catalog);
                SerializedProperty entries = serializedObject.FindProperty("_entries");
                if (entries == null || !entries.isArray)
                {
                    continue;
                }

                // Comentário: duplicidade relevante aqui é dentro do próprio catálogo de profiles.
                var profileCatalogIds = new HashSet<string>();

                for (int j = 0; j < entries.arraySize; j++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(j);
                    SerializedProperty profileId = entry.FindPropertyRelative("_profileId");
                    SerializedProperty raw = profileId?.FindPropertyRelative("_value");
                    if (raw == null)
                    {
                        continue;
                    }

                    string value = raw.stringValue;
                    SceneFlowIdSourceUtility.AddAndTrackDuplicate(profileCatalogIds, duplicates, value);
                    SceneFlowIdSourceUtility.AddValue(allValues, value);
                }
            }
        }
    }
}
