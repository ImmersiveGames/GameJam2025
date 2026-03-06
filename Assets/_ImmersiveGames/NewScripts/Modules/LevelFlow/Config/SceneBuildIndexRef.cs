using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Config
{
    [Serializable]
    public sealed class SceneBuildIndexRef : IEquatable<SceneBuildIndexRef>
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif
        [SerializeField] private int buildIndex = -1;
        [SerializeField] private string sceneName = string.Empty;

        public int BuildIndex => buildIndex;
        public string SceneName => sceneName ?? string.Empty;
        public bool IsValid => buildIndex >= 0;

#if UNITY_EDITOR
        public void SyncFromEditorAsset()
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

        public bool Equals(SceneBuildIndexRef other)
        {
            return other != null && buildIndex == other.buildIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is SceneBuildIndexRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            return buildIndex;
        }
    }
}
