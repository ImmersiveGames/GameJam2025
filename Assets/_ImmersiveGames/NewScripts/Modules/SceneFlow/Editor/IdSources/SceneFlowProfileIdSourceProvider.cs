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
            var values = new HashSet<string>();
            var duplicates = new HashSet<string>();

            AddCanonicalProfiles(values, duplicates);
            CollectFromTransitionStyleCatalog(values, duplicates);
            CollectFromTransitionProfileCatalog(values, duplicates);

            return SceneFlowIdSourceUtility.BuildResult(values, duplicates);
        }

        private static void AddCanonicalProfiles(HashSet<string> values, HashSet<string> duplicates)
        {
            SceneFlowIdSourceUtility.AddAndTrackDuplicate(values, duplicates, SceneFlowProfileId.Startup.Value);
            SceneFlowIdSourceUtility.AddAndTrackDuplicate(values, duplicates, SceneFlowProfileId.Frontend.Value);
            SceneFlowIdSourceUtility.AddAndTrackDuplicate(values, duplicates, SceneFlowProfileId.Gameplay.Value);
        }

        private static void CollectFromTransitionStyleCatalog(HashSet<string> values, HashSet<string> duplicates)
        {
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

                    SceneFlowIdSourceUtility.AddAndTrackDuplicate(values, duplicates, raw.stringValue);
                }
            }
        }

        private static void CollectFromTransitionProfileCatalog(HashSet<string> values, HashSet<string> duplicates)
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

                for (int j = 0; j < entries.arraySize; j++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(j);
                    SerializedProperty profileId = entry.FindPropertyRelative("_profileId");
                    SerializedProperty raw = profileId?.FindPropertyRelative("_value");
                    if (raw == null)
                    {
                        continue;
                    }

                    SceneFlowIdSourceUtility.AddAndTrackDuplicate(values, duplicates, raw.stringValue);
                }
            }
        }
    }
}
