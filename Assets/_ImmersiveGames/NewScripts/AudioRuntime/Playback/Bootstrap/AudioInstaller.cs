using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Bootstrap
{
    /// <summary>
    /// Installer do Audio.
    ///
    /// Responsabilidade:
    /// - registrar contratos, configs e servicos estruturais do modulo;
    /// - nao compor runtime operacional nem criar bridges/hosts;
    /// - falhar cedo quando a configuracao obrigatoria estiver ausente.
    /// </summary>
    public static class AudioInstaller
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
                throw new InvalidOperationException("[FATAL][Config][Audio] BootstrapConfigAsset obrigatorio ausente para instalar Audio.");
            }

            AudioDefaultsAsset audioDefaults = bootstrapConfig.AudioDefaults
                ?? throw new InvalidOperationException("[FATAL][Config][Audio] BootstrapConfigAsset obrigatorio: AudioDefaults ausente.");

            RegisterAudioDefaults(audioDefaults);
            RegisterAudioSettings();
            RegisterAudioRoutingResolver();

            _installed = true;

            DebugUtility.Log(typeof(AudioInstaller),
                "[Audio] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterAudioDefaults(AudioDefaultsAsset audioDefaults)
        {
            if (DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(
                    typeof(AudioInstaller),
                    "[Audio][BOOT] AudioDefaultsAsset already registered in DI.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal(audioDefaults, allowOverride: false);

            DebugUtility.LogVerbose(
                typeof(AudioInstaller),
                $"[Audio][BOOT] AudioDefaultsAsset registered. asset='{audioDefaults.name}'.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterAudioSettings()
        {
            RegisterIfMissing<IAudioSettingsService>(
                factory: () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var defaults) || defaults == null)
                    {
                        throw new InvalidOperationException("[FATAL][Audio] AudioDefaultsAsset obrigatorio ausente antes de registrar IAudioSettingsService.");
                    }

                    return new AudioSettingsService(
                        masterVolume: defaults.MasterVolume,
                        bgmVolume: defaults.BgmVolume,
                        sfxVolume: defaults.SfxVolume,
                        bgmCategoryMultiplier: defaults.BgmCategoryMultiplier,
                        sfxCategoryMultiplier: defaults.SfxCategoryMultiplier);
                },
                alreadyRegisteredMessage: "[Audio][BOOT] IAudioSettingsService already registered.",
                registeredMessage: "[Audio][BOOT] IAudioSettingsService registered from AudioDefaultsAsset.");
        }

        private static void RegisterAudioRoutingResolver()
        {
            RegisterIfMissing<IAudioRoutingResolver>(
                factory: () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var defaults) || defaults == null)
                    {
                        throw new InvalidOperationException("[FATAL][Audio] AudioDefaultsAsset obrigatorio ausente antes de registrar IAudioRoutingResolver.");
                    }

                    return new AudioRoutingResolver(defaults);
                },
                alreadyRegisteredMessage: "[Audio][BOOT] IAudioRoutingResolver already registered.",
                registeredMessage: "[Audio][BOOT] IAudioRoutingResolver registered.");
        }

        private static void RegisterIfMissing<T>(
            Func<T> factory,
            string alreadyRegisteredMessage,
            string registeredMessage) where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(AudioInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(AudioInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}

