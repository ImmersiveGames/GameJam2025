#if UNITY_EDITOR
using UnityEditor;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        static partial void RequestEditorStopPlayMode()
        {
            EditorApplication.isPlaying = false;
        }

        static partial void TryResolveBuildSettingsScenePathEditor(string fadeSceneName, ref string scenePath)
        {
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (buildScenes == null)
            {
                return;
            }

            for (int i = 0; i < buildScenes.Length; i++)
            {
                EditorBuildSettingsScene scene = buildScenes[i];
                if (scene == null || !scene.enabled || string.IsNullOrWhiteSpace(scene.path))
                {
                    continue;
                }

                string candidatePath = scene.path.Trim();
                string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(candidatePath);
                if (string.Equals(sceneFileName, fadeSceneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    scenePath = candidatePath;
                    return;
                }
            }
        }
    }
}
#endif
