using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Config
{
    [CreateAssetMenu(
        fileName = "NewScriptsBootstrapConfigAsset",
        menuName = "ImmersiveGames/NewScripts/Infrastructure/Config/Configs/NewScriptsBootstrapConfigAsset",
        order = 20)]
    public sealed class NewScriptsBootstrapConfigAsset : ScriptableObject
    {
        [SerializeField] private GameNavigationCatalogAsset navigationCatalog;
        [SerializeField] private SceneRouteCatalogAsset sceneRouteCatalog;
        [SerializeField] private TransitionStyleAsset startupTransitionStyleRef;
        [SerializeField] private SceneKeyAsset fadeSceneKey;

        public GameNavigationCatalogAsset NavigationCatalog => navigationCatalog;
        public SceneRouteCatalogAsset SceneRouteCatalog => sceneRouteCatalog;
        public TransitionStyleAsset StartupTransitionStyleRef => startupTransitionStyleRef;
        public SceneKeyAsset FadeSceneKey => fadeSceneKey;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (navigationCatalog != null)
            {
                navigationCatalog.ValidateCriticalIntentsInEditor();
            }

            if (startupTransitionStyleRef == null || startupTransitionStyleRef.Profile == null)
            {
                string message =
                    $"[FATAL][Config] NewScriptsBootstrapConfigAsset invalido: configure startupTransitionStyleRef com profileRef valido. asset='{name}'.";

                DebugUtility.LogError(typeof(NewScriptsBootstrapConfigAsset), message);
                throw new InvalidOperationException(message);
            }
        }
#endif
    }
}
