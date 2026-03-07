#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Config
{
    public sealed partial class SceneBuildIndexRef
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;

        partial void SyncFromEditorAssetEditor()
        {
            if (sceneAsset == null)
            {
                buildIndex = -1;
                sceneName = string.Empty;
                return;
            }

            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            sceneName = sceneAsset.name ?? string.Empty;
            buildIndex = ResolveBuildIndex(scenePath);

            if (buildIndex < 0)
            {
                DebugUtility.LogWarning(typeof(SceneBuildIndexRef),
                    $"[WARN][LevelFlow][Config] Scene not in Build Settings or disabled: {scenePath}");
            }
        }

        private static int ResolveBuildIndex(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return -1;
            }

            return SceneUtility.GetBuildIndexByScenePath(scenePath);
        }
#endif
    }
}
#endif
