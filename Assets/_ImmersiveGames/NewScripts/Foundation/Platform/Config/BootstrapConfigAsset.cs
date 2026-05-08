using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Config;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Config
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
        [SerializeField] private LoggingConfigAsset loggingConfig;
        [SerializeField] private RuntimeModeConfig runtimeModeConfig;
        [SerializeField] private AudioDefaultsAsset audioDefaults;
        [SerializeField] private VideoDefaultsAsset videoDefaults;

        public LoggingConfigAsset LoggingConfig => loggingConfig;
        public RuntimeModeConfig RuntimeModeConfig => runtimeModeConfig;
        public AudioDefaultsAsset AudioDefaults => audioDefaults;
        public VideoDefaultsAsset VideoDefaults => videoDefaults;
#if UNITY_EDITOR
        private void OnValidate()
        {
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

