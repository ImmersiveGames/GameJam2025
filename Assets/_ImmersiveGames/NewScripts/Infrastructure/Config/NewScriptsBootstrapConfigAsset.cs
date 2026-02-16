using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Bindings;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
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
        [SerializeField] private GameNavigationIntentCatalogAsset navigationIntentCatalog;
        [SerializeField] private TransitionStyleCatalogAsset transitionStyleCatalog;
        [SerializeField] private LevelCatalogAsset levelCatalog;
        [SerializeField] private LevelId startGameplayLevelId;
        [SerializeField] private SceneRouteCatalogAsset sceneRouteCatalog;
        [SerializeField] private SceneTransitionProfileCatalogAsset transitionProfileCatalog;
        [SerializeField] private SceneKeyAsset fadeSceneKey;

        public GameNavigationCatalogAsset NavigationCatalog => navigationCatalog;
        public GameNavigationIntentCatalogAsset NavigationIntentCatalog => navigationIntentCatalog;
        public TransitionStyleCatalogAsset TransitionStyleCatalog => transitionStyleCatalog;
        public LevelCatalogAsset LevelCatalog => levelCatalog;
        public LevelId StartGameplayLevelId => startGameplayLevelId;
        public SceneRouteCatalogAsset SceneRouteCatalog => sceneRouteCatalog;
        public SceneTransitionProfileCatalogAsset TransitionProfileCatalog => transitionProfileCatalog;
        public SceneKeyAsset FadeSceneKey => fadeSceneKey;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (navigationIntentCatalog == null)
            {
                string message =
                    $"[FATAL][Config] NewScriptsBootstrapConfigAsset inválido: navigationIntentCatalog obrigatório e não configurado. asset='{name}'.";

                DebugUtility.LogError(typeof(NewScriptsBootstrapConfigAsset), message);
                throw new InvalidOperationException(message);
            }

            if (navigationCatalog != null)
            {
                navigationCatalog.ValidateCriticalIntentsInEditor(navigationIntentCatalog);
            }
        }
#endif

    }
}
