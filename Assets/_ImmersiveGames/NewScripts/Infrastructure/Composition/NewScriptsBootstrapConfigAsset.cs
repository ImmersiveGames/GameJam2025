using _ImmersiveGames.NewScripts.Modules.LevelFlow.Bindings;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    /// <summary>
    /// Configuração raiz de bootstrap para reduzir acoplamento por strings de Resources.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewScriptsBootstrapConfig",
        menuName = "ImmersiveGames/NewScripts/Config/Bootstrap Config",
        order = 0)]
    public sealed class NewScriptsBootstrapConfigAsset : ScriptableObject
    {
        // Caminho padrão em Resources para resolução do asset raiz.
        public const string DefaultResourcesPath = "Config/NewScriptsBootstrapConfig";

        [SerializeField] private GameNavigationCatalogAsset navigationCatalog;
        [SerializeField] private TransitionStyleCatalogAsset transitionStyleCatalog;
        [SerializeField] private LevelCatalogAsset levelCatalog;
        [SerializeField] private SceneRouteCatalogAsset sceneRouteCatalog;
        [SerializeField] private SceneTransitionProfileCatalogAsset transitionProfileCatalog;

        // Referências somente leitura para uso no bootstrap/composition root.
        public GameNavigationCatalogAsset NavigationCatalog => navigationCatalog;
        public TransitionStyleCatalogAsset TransitionStyleCatalog => transitionStyleCatalog;
        public LevelCatalogAsset LevelCatalog => levelCatalog;
        public SceneRouteCatalogAsset SceneRouteCatalog => sceneRouteCatalog;
        public SceneTransitionProfileCatalogAsset TransitionProfileCatalog => transitionProfileCatalog;
    }
}
