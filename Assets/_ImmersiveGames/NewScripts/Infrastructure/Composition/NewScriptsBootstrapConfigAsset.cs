using System;
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
        [Serializable]
        public sealed class EssentialScenesConfig
        {
            [SerializeField] private SceneReference fadeScene;
            [SerializeField] private SceneReference uiGlobalScene;
            [SerializeField] private SceneReference menuScene;
            [SerializeField] private SceneReference bootEntryScene;
            [SerializeField] private SceneReference loadingHudScene;

            // Cenas essenciais para o pipeline de bootstrap/start.
            public SceneReference FadeScene => fadeScene;
            public SceneReference UiGlobalScene => uiGlobalScene;
            public SceneReference MenuScene => menuScene;
            public SceneReference BootEntryScene => bootEntryScene;
            public SceneReference LoadingHudScene => loadingHudScene;

#if UNITY_EDITOR
            internal void SyncEditorScenePaths()
            {
                fadeScene?.SyncFromEditorAsset();
                uiGlobalScene?.SyncFromEditorAsset();
                menuScene?.SyncFromEditorAsset();
                bootEntryScene?.SyncFromEditorAsset();
                loadingHudScene?.SyncFromEditorAsset();
            }
#endif
        }

        // Caminho padrão em Resources para resolução do asset raiz.
        public const string DefaultResourcesPath = "Config/NewScriptsBootstrapConfig";

        [SerializeField] private GameNavigationCatalogAsset navigationCatalog;
        [SerializeField] private TransitionStyleCatalogAsset transitionStyleCatalog;
        [SerializeField] private LevelCatalogAsset levelCatalog;
        [SerializeField] private SceneRouteCatalogAsset sceneRouteCatalog;
        [SerializeField] private SceneTransitionProfileCatalogAsset transitionProfileCatalog;
        [SerializeField] private EssentialScenesConfig essentialScenes = new();

        // Referências somente leitura para uso no bootstrap/composition root.
        public GameNavigationCatalogAsset NavigationCatalog => navigationCatalog;
        public TransitionStyleCatalogAsset TransitionStyleCatalog => transitionStyleCatalog;
        public LevelCatalogAsset LevelCatalog => levelCatalog;
        public SceneRouteCatalogAsset SceneRouteCatalog => sceneRouteCatalog;
        public SceneTransitionProfileCatalogAsset TransitionProfileCatalog => transitionProfileCatalog;
        public EssentialScenesConfig EssentialScenes => essentialScenes;

#if UNITY_EDITOR
        private void OnValidate()
        {
            essentialScenes?.SyncEditorScenePaths();
        }
#endif
    }
}
