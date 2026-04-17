using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Config;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Runtime;
namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Bootstrap
{
    public static class PreferencesInstaller
    {
        private static bool _installed;

        public static void Install(BootstrapConfigAsset bootstrapConfig)
        {
            if (_installed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config] BootstrapConfigAsset obrigatorio ausente para instalar Preferences.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IAudioSettingsService>(out var audioSettings) || audioSettings == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IAudioSettingsService obrigatorio ausente antes de instalar Preferences.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var audioDefaults) || audioDefaults == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] AudioDefaultsAsset obrigatorio ausente antes de instalar Preferences.");
            }

            VideoDefaultsAsset videoDefaults = bootstrapConfig.VideoDefaults
                ?? throw new InvalidOperationException("[FATAL][Preferences] BootstrapConfigAsset obrigatorio: VideoDefaults ausente.");

            if (DependencyManager.Provider.TryGetGlobal<VideoDefaultsAsset>(out var registeredVideoDefaults)
                && registeredVideoDefaults != null)
            {
                videoDefaults = registeredVideoDefaults;
            }
            else
            {
                DependencyManager.Provider.RegisterGlobal(videoDefaults, allowOverride: false);

                DebugUtility.LogVerbose(
                    typeof(PreferencesInstaller),
                    $"[Preferences][BOOT] VideoDefaultsAsset registered. asset='{videoDefaults.name}'.",
                    DebugUtility.Colors.Info);
            }

            RegisterBackend();
            RegisterPreferencesService(audioSettings, audioDefaults, videoDefaults);

            _installed = true;

            DebugUtility.Log(typeof(PreferencesInstaller),
                "[Preferences] Module installer concluded.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterBackend()
        {
            RegisterIfMissing<IPreferencesBackend>(
                factory: () => new PlayerPrefsPreferencesBackend(),
                alreadyRegisteredMessage: "[Preferences][BOOT] IPreferencesBackend already registered.",
                registeredMessage: "[Preferences][BOOT] IPreferencesBackend registered (PlayerPrefs backend).");
        }

        private static void RegisterPreferencesService(
            IAudioSettingsService audioSettings,
            AudioDefaultsAsset audioDefaults,
            VideoDefaultsAsset videoDefaults)
        {
            var service = new PreferencesService(ResolveBackend(), audioSettings, audioDefaults, videoDefaults);
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
                factory: () => service,
                alreadyRegisteredMessage: "[Preferences][BOOT] IPreferencesStateService already registered.",
                registeredMessage: "[Preferences][BOOT] IPreferencesStateService registered.");

            RegisterIfMissing<IPreferencesSaveService>(
                factory: () => service,
                alreadyRegisteredMessage: "[Preferences][BOOT] IPreferencesSaveService already registered.",
                registeredMessage: "[Preferences][BOOT] IPreferencesSaveService registered.");
        }

        private static IPreferencesBackend ResolveBackend()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPreferencesBackend>(out var backend) && backend != null)
            {
                return backend;
            }

            throw new InvalidOperationException("[FATAL][Preferences] IPreferencesBackend obrigatorio ausente na instalacao.");
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

