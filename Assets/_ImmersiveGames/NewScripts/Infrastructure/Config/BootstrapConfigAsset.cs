using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Logging.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Config
{
    /// <summary>
    /// Configuração raiz obrigatória que contém todas as referências críticas de infraestrutura do projeto.
    /// Inclui navegação, roteamento de cenas, áudio, logging e transições.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BootstrapConfigAsset",
        menuName = "ImmersiveGames/NewScripts/Infrastructure/Config/BootstrapConfigAsset",
        order = 20)]
    public sealed class BootstrapConfigAsset : ScriptableObject
    {
        /// <summary>
        /// Catálogo de navegação global com intenções críticas mapeadas.
        /// </summary>
        [SerializeField] private GameNavigationCatalogAsset navigationCatalog;
        /// <summary>
        /// Catálogo de roteamento de cenas e mapeamento de rotas.
        /// </summary>
        [SerializeField] private SceneRouteCatalogAsset sceneRouteCatalog;
        /// <summary>
        /// Estilo de transição de inicialização.
        /// </summary>
        [SerializeField] private TransitionStyleAsset startupTransitionStyleRef;
        /// <summary>
        /// Referência da chave de cena para fade/transição.
        /// </summary>
        [SerializeField] private SceneKeyAsset fadeSceneKey;
        /// <summary>
        /// Chave canônica da cena de loading HUD.
        /// </summary>
        [SerializeField] private SceneKeyAsset loadingHudSceneKey;
        /// <summary>
        /// Configuração de logging (níveis, canais, etc).
        /// </summary>
        [SerializeField] private LoggingConfigAsset loggingConfig;
        /// <summary>
        /// Configuração padrão de áudio (volumes, mixer groups, multiplicadores).
        /// </summary>
        [SerializeField] private AudioDefaultsAsset audioDefaults;
        /// <summary>
        /// Mapa semântico de propósitos de áudio para entidades.
        /// </summary>
        [SerializeField] private EntityAudioSemanticMapAsset entityAudioSemanticMap;

        public GameNavigationCatalogAsset NavigationCatalog => navigationCatalog;
        public SceneRouteCatalogAsset SceneRouteCatalog => sceneRouteCatalog;
        public TransitionStyleAsset StartupTransitionStyleRef => startupTransitionStyleRef;
        public SceneKeyAsset FadeSceneKey => fadeSceneKey;
        public SceneKeyAsset LoadingHudSceneKey => loadingHudSceneKey;
        public LoggingConfigAsset LoggingConfig => loggingConfig;
        public AudioDefaultsAsset AudioDefaults => audioDefaults;
        public EntityAudioSemanticMapAsset EntityAudioSemanticMap => entityAudioSemanticMap;

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
                    $"[FATAL][Config] BootstrapConfigAsset inválido: configure startupTransitionStyleRef com profileRef válido. asset='{name}'.";

                DebugUtility.LogError(typeof(BootstrapConfigAsset), message);
                throw new InvalidOperationException(message);
            }

            if (loadingHudSceneKey == null || string.IsNullOrWhiteSpace(loadingHudSceneKey.SceneName))
            {
                string message =
                    $"[FATAL][Config] BootstrapConfigAsset inválido: configure loadingHudSceneKey com SceneName válido. asset='{name}'.";

                DebugUtility.LogError(typeof(BootstrapConfigAsset), message);
                throw new InvalidOperationException(message);
            }
        }
#endif
    }
}
