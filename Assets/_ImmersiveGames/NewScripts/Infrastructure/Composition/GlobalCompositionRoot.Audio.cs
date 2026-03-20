using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void InstallAudioServices()
        {
            RegisterAudioDefaults();
            RegisterAudioSettings();
            RegisterAudioRoutingResolver();
            RegisterAudioListenerHost();
            RegisterAudioBgmService();

            DebugUtility.LogVerbose(
                typeof(GlobalCompositionRoot),
                "[Audio][BOOT] Audio module foundations registered (Defaults + Settings + Routing + Listener + BGM Runtime).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterAudioDefaults()
        {
            if (DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(
                    typeof(GlobalCompositionRoot),
                    "[Audio][BOOT] AudioDefaultsAsset already registered in DI.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bootstrap = GetRequiredBootstrapConfig(out var via);
            var defaults = bootstrap.AudioDefaults;

            if (defaults == null)
            {
                ReportAudioDegraded(
                    reason: "missing_audio_defaults_asset",
                    detail: $"BootstrapConfigAsset '{bootstrap.name}' resolved via '{via}' has null AudioDefaults.");

                defaults = ScriptableObject.CreateInstance<AudioDefaultsAsset>();
                defaults.name = "AudioDefaults_RuntimeFallback";

                DebugUtility.LogWarning(
                    typeof(GlobalCompositionRoot),
                    "[Audio][BOOT][DEGRADED] AudioDefaultsAsset missing; using runtime fallback defaults.");
            }

            DependencyManager.Provider.RegisterGlobal(defaults, allowOverride: false);

            DebugUtility.LogVerbose(
                typeof(GlobalCompositionRoot),
                $"[Audio][BOOT] AudioDefaultsAsset registered. source='{via}' asset='{defaults.name}'.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterAudioSettings()
        {
            RegisterIfMissing<IAudioSettingsService>(
                factory: () =>
                {
                    DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var defaults);

                    return new AudioSettingsService(
                        masterVolume: defaults != null ? defaults.MasterVolume : 1f,
                        bgmVolume: defaults != null ? defaults.BgmVolume : 1f,
                        sfxVolume: defaults != null ? defaults.SfxVolume : 1f,
                        bgmCategoryMultiplier: defaults != null ? defaults.BgmCategoryMultiplier : 1f,
                        sfxCategoryMultiplier: defaults != null ? defaults.SfxCategoryMultiplier : 1f);
                },
                alreadyRegisteredMessage: "[Audio][BOOT] IAudioSettingsService already registered.",
                registeredMessage: "[Audio][BOOT] IAudioSettingsService registered from AudioDefaultsAsset.");
        }

        private static void RegisterAudioRoutingResolver()
        {
            RegisterIfMissing<IAudioRoutingResolver>(
                factory: () =>
                {
                    DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var defaults);
                    return new AudioRoutingResolver(defaults);
                },
                alreadyRegisteredMessage: "[Audio][BOOT] IAudioRoutingResolver already registered.",
                registeredMessage: "[Audio][BOOT] IAudioRoutingResolver registered.");
        }

        private static void RegisterAudioBgmService()
        {
            RegisterIfMissing<IAudioBgmService>(
                factory: () =>
                {
                    DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var defaults);
                    DependencyManager.Provider.TryGetGlobal<IAudioSettingsService>(out var settings);
                    DependencyManager.Provider.TryGetGlobal<IAudioRoutingResolver>(out var routing);

                    return AudioBgmService.Create(defaults, settings, routing);
                },
                alreadyRegisteredMessage: "[Audio][BOOT] IAudioBgmService already registered.",
                registeredMessage: "[Audio][BOOT] IAudioBgmService registered (F3 BGM runtime).");
        }

        private static void RegisterAudioListenerHost()
        {
            AudioListenerRuntimeHost.EnsureCreated();

            DebugUtility.LogVerbose(
                typeof(GlobalCompositionRoot),
                "[Audio][BOOT] Canonical AudioListener runtime host ensured.",
                DebugUtility.Colors.Info);
        }

        private static void ReportAudioDegraded(string reason, string detail)
        {
            if (DependencyManager.Provider.TryGetGlobal<IDegradedModeReporter>(out var reporter) && reporter != null)
            {
                reporter.Report(
                    feature: "Audio",
                    reason: reason,
                    detail: detail,
                    signature: "GlobalCompositionRoot.Audio",
                    profile: "Bootstrap");
            }
        }
    }
}
