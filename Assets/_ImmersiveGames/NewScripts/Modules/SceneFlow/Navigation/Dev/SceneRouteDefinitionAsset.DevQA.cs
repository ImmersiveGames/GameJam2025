#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    public partial class SceneRouteDefinitionAsset
    {
        private void ValidateRoutePolicyEditorOnly()
        {
            string validationError = GetRoutePolicyValidationError();
            if (string.IsNullOrWhiteSpace(validationError))
            {
                WarnLevelCollectionPolicyEditorOnly();
                return;
            }

#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(this);
#else
            string assetPath = name;
#endif

            DebugUtility.LogError(typeof(SceneRouteDefinitionAsset),
                $"[FATAL][Config] {validationError} asset='{assetPath}', routeId='{routeId}', routeKind='{routeKind}', requiresWorldReset={requiresWorldReset}.");
        }

        private void WarnLevelCollectionPolicyEditorOnly()
        {
            if (routeKind == SceneRouteKind.Gameplay && levelCollection != null &&
                !levelCollection.TryValidateRuntime(out string collectionError))
            {
#if UNITY_EDITOR
                string assetPath = AssetDatabase.GetAssetPath(this);
#else
                string assetPath = name;
#endif

                DebugUtility.LogWarning(typeof(SceneRouteDefinitionAsset),
                    $"[WARN][LevelFlow][Config] Gameplay route with invalid levelCollection. asset='{assetPath}', routeId='{routeId}', detail='{collectionError}'.");
            }
        }

        private static int ResolveBuildIndexEditorOnly(string sceneName)
        {
#if UNITY_EDITOR
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                string path = scenes[i].path;
                string name = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(name, sceneName, System.StringComparison.Ordinal))
                {
                    return i;
                }
            }
#endif

            return -1;
        }
    }
}
#endif
