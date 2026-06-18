using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Config;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Runtime;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Bootstrap
{
    public static class PreferencesInstaller
    {
        private static bool _installed;

        public static void Install(RuntimeModeConfig runtimeModeConfig)
        {
            if (_installed)
            {
                return;
            }

            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PreferencesRuntime] RuntimeModeConfig obrigatorio ausente para instalar Preferences.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IAudioSettingsService>(out var audioSettings) || audioSettings == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IAudioSettingsService obrigatorio ausente antes de instalar Preferences.");
            }

            var audioDefaults = PreferencesRuntimeConfigResolver.ResolveAudioDefaultsOrFail(runtimeModeConfig);
            if (DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var registeredAudioDefaults)
                && registeredAudioDefaults != null)
            {
                if (!ReferenceEquals(registeredAudioDefaults, audioDefaults))
                {
                    throw new InvalidOperationException("[FATAL][Config][PreferencesRuntime] AudioDefaultsAsset conflitante ja registrada no DI.");
                }
            }
            else
            {
                DependencyManager.Provider.RegisterGlobal(audioDefaults, false);

                DebugUtility.LogVerbose(
                    typeof(PreferencesInstaller),
                    $"[Preferences][BOOT] AudioDefaultsAsset registered. asset='{audioDefaults.name}'.",
                    DebugUtility.Colors.Info);
            }

            var videoDefaults = PreferencesRuntimeConfigResolver.ResolveVideoDefaultsOrFail(runtimeModeConfig);

            if (DependencyManager.Provider.TryGetGlobal<VideoDefaultsAsset>(out var registeredVideoDefaults)
                && registeredVideoDefaults != null)
            {
                if (!ReferenceEquals(registeredVideoDefaults, videoDefaults))
                {
                    throw new InvalidOperationException("[FATAL][Config][PreferencesRuntime] VideoDefaultsAsset conflitante ja registrada no DI.");
                }

                videoDefaults = registeredVideoDefaults;
            }
            else
            {
                DependencyManager.Provider.RegisterGlobal(videoDefaults, false);

                DebugUtility.LogVerbose(
                    typeof(PreferencesInstaller),
                    $"[Preferences][BOOT] VideoDefaultsAsset registered. asset='{videoDefaults.name}'.",
                    DebugUtility.Colors.Info);
            }

            RegisterPreferencesService(audioSettings, audioDefaults, videoDefaults);
            RegisterPreferencesSaveAdapter(runtimeModeConfig);
            RegisterPreferencesRuntimePipeline();

            _installed = true;

            DebugUtility.Log(typeof(PreferencesInstaller),
                "[Preferences] Module installer concluded.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPreferencesService(
            IAudioSettingsService audioSettings,
            AudioDefaultsAsset audioDefaults,
            VideoDefaultsAsset videoDefaults)
        {
            var service = new PreferencesService(audioSettings, audioDefaults, videoDefaults);
            service.SetCurrent(
                AudioPreferencesSnapshot.CaptureFrom(
                    AudioPreferencesSnapshot.BootstrapProfileId,
                    AudioPreferencesSnapshot.BootstrapSlotId,
                    audioSettings),
                "Preferences/InstallerSeed");

            service.SetCurrent(
                videoDefaults.CreateDefaultSnapshot(
                    VideoPreferencesSnapshot.BootstrapProfileId,
                    VideoPreferencesSnapshot.BootstrapSlotId),
                "Preferences/InstallerSeed");

            RegisterIfMissing<IPreferencesStateService>(
                () => service,
                "[Preferences][BOOT] IPreferencesStateService already registered.",
                "[Preferences][BOOT] IPreferencesStateService registered.");
        }

        private static void RegisterPreferencesSaveAdapter(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] RuntimeModeConfig obrigatorio ausente para registrar PreferencesSaveAdapter.");
            }

            RegisterIfMissing<IPreferencesSaveAdapter>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<ISaveService>(out var saveService) || saveService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Preferences] ISaveService obrigatorio ausente antes de registrar PreferencesSaveAdapter.");
                    }

                    var saveConfig = SaveRuntimeConfigResolver.ResolveSaveConfigOrFail(runtimeModeConfig);
                    return new PreferencesSaveAdapter(saveService, saveConfig.SchemaVersion);
                },
                "[Preferences][BOOT] IPreferencesSaveAdapter already registered.",
                "[Preferences][BOOT] IPreferencesSaveAdapter registered.");
        }

        private static void RegisterPreferencesRuntimePipeline()
        {
            RegisterIfMissing<IPreferencesRuntimePipeline>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IPreferencesStateService>(out var stateService) || stateService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Preferences] IPreferencesStateService obrigatorio ausente antes de registrar IPreferencesRuntimePipeline.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IPreferencesSaveAdapter>(out var saveAdapter) || saveAdapter == null)
                    {
                        throw new InvalidOperationException("[FATAL][Preferences] IPreferencesSaveAdapter obrigatorio ausente antes de registrar IPreferencesRuntimePipeline.");
                    }

                    return new PreferencesRuntimePipeline(stateService, saveAdapter);
                },
                "[Preferences][BOOT] IPreferencesRuntimePipeline already registered.",
                "[Preferences][BOOT] IPreferencesRuntimePipeline registered.");
        }

        private static void RegisterIfMissing<T>(
            Func<T> factory,
            string alreadyRegisteredMessage,
            string registeredMessage) where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(PreferencesInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(PreferencesInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
