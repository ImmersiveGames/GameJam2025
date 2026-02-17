using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Editor.Validation
{
    public static class SceneFlowConfigReserializer
    {
        private static readonly string[] CanonicalAssetPaths =
        {
            "Assets/Resources/GameNavigationIntentCatalog.asset",
            "Assets/Resources/Navigation/GameNavigationCatalog.asset",
            "Assets/Resources/SceneFlow/SceneRouteCatalog.asset",
            "Assets/Resources/Navigation/TransitionStyleCatalog.asset",
            "Assets/Resources/SceneFlow/SceneTransitionProfileCatalog.asset",
            "Assets/Resources/SceneFlow/Profiles/DefaultTransitionProfile.asset",
            "Assets/Resources/Navigation/LevelCatalog.asset",
            "Assets/Resources/RuntimeModeConfig.asset",
            "Assets/Resources/NewScriptsBootstrapConfig.asset"
        };

        [MenuItem("ImmersiveGames/NewScripts/Config/Reserialize SceneFlow Assets (DataCleanup v1)", priority = 3020)]
        public static void ReserializeDataCleanupV1Assets()
        {
            List<string> existingPaths = new List<string>(CanonicalAssetPaths.Length);

            for (int i = 0; i < CanonicalAssetPaths.Length; i++)
            {
                string path = CanonicalAssetPaths[i];
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    existingPaths.Add(path);
                }
            }

            AssetDatabase.ForceReserializeAssets(existingPaths);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[OBS][Config] SceneFlow DataCleanup v1 reserialize completed. assetsReserialized={existingPaths.Count}.");
        }
    }
}
