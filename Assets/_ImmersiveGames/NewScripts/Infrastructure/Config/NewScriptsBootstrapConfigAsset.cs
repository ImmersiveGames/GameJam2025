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
        [SerializeField] private TransitionStyleCatalogAsset transitionStyleCatalog;
        [SerializeField] private LevelCatalogAsset levelCatalog;
        [SerializeField] private LevelId startGameplayLevelId;
        [SerializeField] private SceneRouteCatalogAsset sceneRouteCatalog;
        [SerializeField] private SceneTransitionProfileCatalogAsset transitionProfileCatalog;
        [SerializeField] private SceneKeyAsset fadeSceneKey;

        public GameNavigationCatalogAsset NavigationCatalog => navigationCatalog;
        public TransitionStyleCatalogAsset TransitionStyleCatalog => transitionStyleCatalog;
        public LevelCatalogAsset LevelCatalog => levelCatalog;
        public LevelId StartGameplayLevelId => startGameplayLevelId;
        public SceneRouteCatalogAsset SceneRouteCatalog => sceneRouteCatalog;
        public SceneTransitionProfileCatalogAsset TransitionProfileCatalog => transitionProfileCatalog;
        public SceneKeyAsset FadeSceneKey => fadeSceneKey;

    }
}
