#if UNITY_EDITOR
using System;
using System.Linq;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace _ImmersiveGames.NewScripts.Editor
{
    /// <summary>
    /// Validador de build para obrigatoriedades de configuração do Fade.
    /// </summary>
    public sealed class FadeBootstrapBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            var config = Resources.Load<NewScriptsBootstrapConfigAsset>(NewScriptsBootstrapConfigAsset.DefaultResourcesPath);
            if (config == null)
            {
                throw new BuildFailedException(
                    $"[FATAL][Config] Missing NewScriptsBootstrapConfigAsset at Resources/{NewScriptsBootstrapConfigAsset.DefaultResourcesPath}.asset");
            }

            var fadeSceneKey = config.FadeSceneKey;
            if (fadeSceneKey == null)
            {
                throw new BuildFailedException(
                    $"[FATAL][Config] Missing required fadeSceneKey in bootstrap config asset='{config.name}'.");
            }

            var fadeSceneName = (fadeSceneKey.SceneName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fadeSceneName))
            {
                throw new BuildFailedException(
                    $"[FATAL][Config] Invalid fadeSceneKey SceneName. asset='{config.name}', keyAsset='{fadeSceneKey.name}'.");
            }

            bool inBuildSettings = EditorBuildSettings.scenes
                .Where(s => s != null && s.enabled)
                .Any(s => string.Equals(GetSceneNameFromPath(s.path), fadeSceneName, StringComparison.Ordinal));

            if (!inBuildSettings)
            {
                throw new BuildFailedException(
                    $"[FATAL][Config] Fade scene is not in Build Settings. scene='{fadeSceneName}', keyAsset='{fadeSceneKey.name}'.");
            }
        }

        private static string GetSceneNameFromPath(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return string.Empty;
            }

            string fileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            return string.IsNullOrWhiteSpace(fileName) ? string.Empty : fileName.Trim();
        }
    }
}
#endif
