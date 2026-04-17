using System;
using ImmersiveGames.GameJam2025.Infrastructure.RuntimeMode;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Core.Logging.Config;
using ImmersiveGames.GameJam2025.Experience.Audio.Config;
using ImmersiveGames.GameJam2025.Experience.Preferences.Config;
using ImmersiveGames.GameJam2025.Orchestration.Navigation;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Navigation.Bindings;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Infrastructure.Config
{
    /// <summary>
    /// Root configuration with the canonical infrastructure references.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BootstrapConfigAsset",
        menuName = "ImmersiveGames/NewScripts/Infrastructure/Config/BootstrapConfigAsset",
        order = 20)]
    public sealed class BootstrapConfigAsset : ScriptableObject
    {
        [SerializeField] private GameNavigationCatalogAsset navigationCatalog;
        [SerializeField] private TransitionStyleAsset startupTransitionStyleRef;
        [SerializeField] private SceneKeyAsset fadeSceneKey;
        [SerializeField] private SceneKeyAsset loadingHudSceneKey;
        [SerializeField] private LoggingConfigAsset loggingConfig;
        [SerializeField] private RuntimeModeConfig runtimeModeConfig;
        [SerializeField] private AudioDefaultsAsset audioDefaults;
        [SerializeField] private VideoDefaultsAsset videoDefaults;

        public GameNavigationCatalogAsset NavigationCatalog => navigationCatalog;
        public TransitionStyleAsset StartupTransitionStyleRef => startupTransitionStyleRef;
        public SceneKeyAsset FadeSceneKey => fadeSceneKey;
        public SceneKeyAsset LoadingHudSceneKey => loadingHudSceneKey;
        public LoggingConfigAsset LoggingConfig => loggingConfig;
        public RuntimeModeConfig RuntimeModeConfig => runtimeModeConfig;
        public AudioDefaultsAsset AudioDefaults => audioDefaults;
        public VideoDefaultsAsset VideoDefaults => videoDefaults;
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
                    $"[FATAL][Config] BootstrapConfigAsset invalid: configure startupTransitionStyleRef with a valid profileRef. asset='{name}'.";

                DebugUtility.LogError(typeof(BootstrapConfigAsset), message);
                throw new InvalidOperationException(message);
            }

            if (loadingHudSceneKey == null || string.IsNullOrWhiteSpace(loadingHudSceneKey.SceneName))
            {
                string message =
                    $"[FATAL][Config] BootstrapConfigAsset invalid: configure loadingHudSceneKey with a valid SceneName. asset='{name}'.";

                DebugUtility.LogError(typeof(BootstrapConfigAsset), message);
                throw new InvalidOperationException(message);
            }

            if (fadeSceneKey == null || string.IsNullOrWhiteSpace(fadeSceneKey.SceneName))
            {
                string message =
                    $"[FATAL][Config] BootstrapConfigAsset invalid: configure fadeSceneKey with a valid SceneName. asset='{name}'.";

                DebugUtility.LogError(typeof(BootstrapConfigAsset), message);
                throw new InvalidOperationException(message);
            }

            if (runtimeModeConfig == null)
            {
                string message =
                    $"[FATAL][Config] BootstrapConfigAsset invalid: configure runtimeModeConfig with a valid RuntimeModeConfig asset. asset='{name}'.";

                DebugUtility.LogError(typeof(BootstrapConfigAsset), message);
                throw new InvalidOperationException(message);
            }

            if (videoDefaults == null)
            {
                string message =
                    $"[FATAL][Config] BootstrapConfigAsset invalid: configure videoDefaults with a valid VideoDefaultsAsset asset. asset='{name}'.";

                DebugUtility.LogError(typeof(BootstrapConfigAsset), message);
                throw new InvalidOperationException(message);
            }
        }
#endif
    }
}

