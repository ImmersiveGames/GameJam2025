using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEditor;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.IdSources
{
    /// <summary>
    /// Coleta TransitionStyleId a partir de TransitionStyleCatalogAsset.styles.
    /// </summary>
    public sealed class TransitionStyleIdSourceProvider : ISceneFlowIdSourceProvider<TransitionStyleId>
    {
        public SceneFlowIdSourceResult Collect()
        {
            var values = new HashSet<string>();
            var duplicates = new HashSet<string>();

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
                    SerializedProperty styleId = styleEntry.FindPropertyRelative("styleId");
                    SerializedProperty raw = styleId?.FindPropertyRelative("_value");
                    if (raw == null)
                    {
                        continue;
                    }

                    SceneFlowIdSourceUtility.AddAndTrackDuplicate(values, duplicates, raw.stringValue);
                }
            }

            return SceneFlowIdSourceUtility.BuildResult(values, duplicates);
        }
    }
}
