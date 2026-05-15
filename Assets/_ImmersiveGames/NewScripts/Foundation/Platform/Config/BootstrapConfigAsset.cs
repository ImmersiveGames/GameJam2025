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
    /// Legacy configuration asset kept only for editor-time serialization compatibility.
    /// Active Base 1.1 runtime path does not consume this asset.
    /// </summary>
    [Obsolete("Legacy asset. Active runtime uses RuntimeModeConfig -> RuntimeConfigSetAsset -> RuntimeConfigRegistry.")]
    [CreateAssetMenu(
        fileName = "BootstrapConfigAsset",
        menuName = "ImmersiveGames/NewScripts/Infrastructure/Config/BootstrapConfigAsset",
        order = 20)]
    public sealed class BootstrapConfigAsset : ScriptableObject
    {
        [Tooltip("LEGACY ONLY - not used by active Base 1.1 runtime path.")]
        [SerializeField] private LoggingConfigAsset loggingConfig;
        [Tooltip("LEGACY ONLY - not used by active Base 1.1 runtime path.")]
        [SerializeField] private RuntimeModeConfig runtimeModeConfig;
        [Tooltip("LEGACY ONLY - not used by active Base 1.1 runtime path.")]
        [SerializeField] private AudioDefaultsAsset audioDefaults;
        [Tooltip("LEGACY ONLY - not used by active Base 1.1 runtime path.")]
        [SerializeField] private VideoDefaultsAsset videoDefaults;

        public LoggingConfigAsset LoggingConfig => loggingConfig;
        public RuntimeModeConfig RuntimeModeConfig => runtimeModeConfig;
        public AudioDefaultsAsset AudioDefaults => audioDefaults;
        public VideoDefaultsAsset VideoDefaults => videoDefaults;
#if UNITY_EDITOR
        private void OnValidate()
        {
            _ = loggingConfig;
            _ = runtimeModeConfig;
            _ = audioDefaults;
            _ = videoDefaults;
            DebugUtility.LogVerbose(typeof(BootstrapConfigAsset),
                "[OBS][Config] BootstrapConfigAsset is legacy-only and not part of active Base 1.1 runtime config path.",
                DebugUtility.Colors.Info);
        }
#endif
    }
}

