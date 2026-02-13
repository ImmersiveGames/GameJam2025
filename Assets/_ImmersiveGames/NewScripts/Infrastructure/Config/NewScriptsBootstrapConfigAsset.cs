using _ImmersiveGames.NewScripts.Modules.LevelFlow.Bindings;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Config
{
    [CreateAssetMenu(
        fileName = "NewScriptsBootstrapConfig",
        menuName = "Immersive Games/NewScripts/Bootstrap Config",
        order = 100)]
    public sealed class NewScriptsBootstrapConfigAsset : ScriptableObject
    {
        public const string DefaultResourcesPath = "NewScriptsBootstrapConfig";

        [SerializeField] private GameNavigationCatalogAsset navigationCatalog;
        [SerializeField] private TransitionStyleCatalogAsset transitionStyleCatalog;
        [SerializeField] private LevelCatalogAsset levelCatalog;
        [SerializeField] private SceneRouteCatalogAsset sceneRouteCatalog;
        [SerializeField] private SceneTransitionProfileCatalogAsset transitionProfileCatalog;

        public GameNavigationCatalogAsset NavigationCatalog => navigationCatalog;
        public TransitionStyleCatalogAsset TransitionStyleCatalog => transitionStyleCatalog;
        public LevelCatalogAsset LevelCatalog => levelCatalog;
        public SceneRouteCatalogAsset SceneRouteCatalog => sceneRouteCatalog;
        public SceneTransitionProfileCatalogAsset TransitionProfileCatalog => transitionProfileCatalog;
    }
}
