using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using _ImmersiveGames.NewScripts.Modules.Preferences.Contracts;
using _ImmersiveGames.NewScripts.Modules.Preferences.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Preferences.Bootstrap
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

            _ = bootstrapConfig;

            if (!DependencyManager.Provider.TryGetGlobal<IAudioSettingsService>(out var audioSettings) || audioSettings == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IAudioSettingsService obrigatorio ausente antes de instalar Preferences.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var audioDefaults) || audioDefaults == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] AudioDefaultsAsset obrigatorio ausente antes de instalar Preferences.");
            }

            RegisterBackend();
            RegisterPreferencesService(audioSettings, audioDefaults);

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

        private static void RegisterPreferencesService(IAudioSettingsService audioSettings, AudioDefaultsAsset audioDefaults)
        {
            var service = new PreferencesService(ResolveBackend(), audioSettings, audioDefaults);
            service.SetCurrent(
                AudioPreferencesSnapshot.CaptureFrom(
                    AudioPreferencesSnapshot.BootstrapProfileId,
                    AudioPreferencesSnapshot.BootstrapSlotId,
                    audioSettings),
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
