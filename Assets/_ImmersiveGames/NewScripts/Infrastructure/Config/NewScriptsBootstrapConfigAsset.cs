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
        [SerializeField, HideInInspector] private LevelCatalogAsset levelCatalog;
        [SerializeField, HideInInspector] private LevelId startGameplayLevelId;
        [SerializeField] private SceneRouteCatalogAsset sceneRouteCatalog;
        [SerializeField] private SceneTransitionProfileCatalogAsset transitionProfileCatalog;
        [SerializeField] private SceneKeyAsset fadeSceneKey;

        private static bool _legacyFieldsWarned;

        public GameNavigationCatalogAsset NavigationCatalog => navigationCatalog;
        public GameNavigationIntentCatalogAsset NavigationIntentCatalog => navigationIntentCatalog;
        public TransitionStyleCatalogAsset TransitionStyleCatalog => transitionStyleCatalog;
        public SceneRouteCatalogAsset SceneRouteCatalog => sceneRouteCatalog;
        public SceneTransitionProfileCatalogAsset TransitionProfileCatalog => transitionProfileCatalog;
        public SceneKeyAsset FadeSceneKey => fadeSceneKey;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_legacyFieldsWarned && (levelCatalog != null || startGameplayLevelId.IsValid))
            {
                _legacyFieldsWarned = true;
                DebugUtility.Log(typeof(NewScriptsBootstrapConfigAsset),
                    $"[OBS][LEGACY] Bootstrap legacy fields are ignored in canonical flow. asset='{name}' hasLevelCatalog='{(levelCatalog != null)}' hasStartGameplayLevelId='{startGameplayLevelId.IsValid}'.",
                    DebugUtility.Colors.Info);
            }

            if (navigationIntentCatalog == null)
            {
                string message =
                    $"[FATAL][Config] NewScriptsBootstrapConfigAsset invalido: navigationIntentCatalog obrigatorio e nao configurado. asset='{name}'.";

                DebugUtility.LogError(typeof(NewScriptsBootstrapConfigAsset), message);
                throw new InvalidOperationException(message);
            }

            if (navigationCatalog != null)
            {
                if (navigationCatalog.IntentCatalogAssetRef != navigationIntentCatalog)
                {
                    string message =
                        $"[FATAL][Config] NewScriptsBootstrapConfigAsset inconsistente: navigationCatalog.assetRef deve apontar para navigationIntentCatalog. asset='{name}'.";

                    DebugUtility.LogError(typeof(NewScriptsBootstrapConfigAsset), message);
                    throw new InvalidOperationException(message);
                }

                navigationCatalog.ValidateCriticalIntentsInEditor(navigationIntentCatalog);
            }
        }
#endif
    }
}
