#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using _ImmersiveGames.NewScripts.Core.Logging;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    public sealed partial class SceneRouteCatalogAsset
    {
        private void OnValidate()
        {
            _cacheBuilt = false;

            try
            {
                EnsureCache();
            }
            catch (Exception ex)
            {
                string assetPath = name;
#if UNITY_EDITOR
                assetPath = AssetDatabase.GetAssetPath(this);
#endif
                DebugUtility.LogError(typeof(SceneRouteCatalogAsset),
                    $"[FATAL][Config] SceneRouteCatalogAsset invalido durante OnValidate. asset='{assetPath}', detail='{ex.Message}'.");
            }
        }
    }
}
#endif
