using System;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;
using ImmersiveGames.GameJam2025.Infrastructure.Config;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Experience.Audio.Bridges;
using ImmersiveGames.GameJam2025.Experience.Audio.Config;
using ImmersiveGames.GameJam2025.Experience.Audio.Context;
using ImmersiveGames.GameJam2025.Experience.Audio.Runtime;
using ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Core;
using ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Host;
using ImmersiveGames.GameJam2025.Infrastructure.Pooling.Contracts;
using ImmersiveGames.GameJam2025.Experience.Preferences.Contracts;
using ImmersiveGames.GameJam2025.Orchestration.Navigation;
namespace ImmersiveGames.GameJam2025.Experience.Audio.Bootstrap
{
    /// <summary>
    /// Runtime composer do Audio.
    ///
    /// Responsabilidade:
    /// - compor o wiring operacional do Audio depois que os installers concluirem;
    /// - nao registrar contratos pre-runtime;
    /// - nao mascarar ausencias de prerequisitos do installer.
    /// </summary>
    public static class AudioRuntimeComposer
    {
        private static bool _runtimeComposed;

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(AudioRuntimeComposer));

            if (_runtimeComposed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Audio] BootstrapConfigAsset obrigatorio ausente para compor o runtime.");
            }

            ApplyPreferencesToAudioSettings();
            EnsureAudioListenerHost();
            EnsureAudioBgmService();
            EnsureAudioBgmContextService(bootstrapConfig);
            EnsureNavigationLevelRouteBgmBridge();
            EnsureGlobalAudioService();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(AudioRuntimeComposer),
                "[Audio] Runtime composition concluida.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureAudioListenerHost()
        {
            AudioListenerRuntimeHost.EnsureCreated();

            DebugUtility.LogVerbose(
                typeof(AudioRuntimeComposer),
                "[Audio][BOOT] Canonical AudioListener runtime host ensured.",
                DebugUtility.Colors.Info);
        }

        private static void ApplyPreferencesToAudioSettings()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPreferencesStateService>(out var preferencesState) || preferencesState == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesStateService obrigatorio ausente antes do apply de AudioRuntimeComposer.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IAudioSettingsService>(out var audioSettings) || audioSettings == null)
            {
                throw new InvalidOperationException("[FATAL][Audio] IAudioSettingsService obrigatorio ausente antes do apply de Preferences.");
            }

            if (!preferencesState.HasSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Snapshot obrigatorio ausente antes do apply para o runtime de Audio.");
            }

            preferencesState.ApplyTo(audioSettings, "AudioRuntimeComposer/ApplyPreferences");
        }

        private static void EnsureAudioBgmService()
        {
            RegisterIfMissing(
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

        private static void EnsureNavigationLevelRouteBgmBridge()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IAudioBgmContextService>(out var bgmContextService) || bgmContextService == null)
            {
                DebugUtility.LogWarning(typeof(AudioRuntimeComposer),
                    "[Audio][BGM][Bridge] Skipped registration: IAudioBgmContextService unavailable.");
                return;
            }

            RegisterIfMissing<NavigationLevelRouteBgmBridge>(
                () => new NavigationLevelRouteBgmBridge(bgmContextService),
                "[Audio][BGM][Bridge] NavigationLevelRouteBgmBridge already registered in global DI.",
                "[Audio][BGM][Bridge] NavigationLevelRouteBgmBridge registered in global DI.");
        }

        private static void EnsureAudioBgmContextService(BootstrapConfigAsset bootstrapConfig)
        {
            if (!DependencyManager.Provider.TryGetGlobal<IAudioBgmService>(out var bgmService) || bgmService == null)
            {
                DebugUtility.LogWarning(typeof(AudioRuntimeComposer),
                    "[Audio][BGM][Context] Skipped registration: IAudioBgmService unavailable.");
                return;
            }

            var navigationCatalog = bootstrapConfig.NavigationCatalog as GameNavigationCatalogAsset;
            if (navigationCatalog == null)
            {
                DebugUtility.LogWarning(typeof(AudioRuntimeComposer),
                    "[Audio][BGM][Context] Skipped registration: NavigationCatalog missing in bootstrap.");
                return;
            }

            RegisterIfMissing<IAudioBgmContextService>(
                () => new AudioBgmContextService(bgmService, navigationCatalog),
                "[Audio][BGM][Context] IAudioBgmContextService already registered in global DI.",
                "[Audio][BGM][Context] IAudioBgmContextService registered in global DI.");
        }

        private static void EnsureGlobalAudioService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPoolService>(out var poolService) || poolService == null)
            {
                throw new InvalidOperationException("[FATAL][Pooling] IPoolService obrigatorio ausente antes de compor o AudioGlobalSfxService.");
            }

            RegisterIfMissing(
                factory: () =>
                {
                    DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var defaults);
                    DependencyManager.Provider.TryGetGlobal<IAudioSettingsService>(out var settings);
                    DependencyManager.Provider.TryGetGlobal<IAudioRoutingResolver>(out var routing);

                    return AudioGlobalSfxService.Create(defaults, settings, routing, poolService);
                },
                alreadyRegisteredMessage: "[Audio][BOOT] IGlobalAudioService already registered.",
                registeredMessage: "[Audio][BOOT] IGlobalAudioService registered (F4/F5 direct + pooled SFX runtime).");
        }

        private static void RegisterIfMissing<T>(
            Func<T> factory,
            string alreadyRegisteredMessage,
            string registeredMessage) where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(AudioRuntimeComposer), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(AudioRuntimeComposer), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}

