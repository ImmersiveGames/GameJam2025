using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.Audio.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Bootstrap
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

            EnsureAudioListenerHost();
            EnsureAudioBgmService();
            EnsureAudioPauseDuckingBridge();
            EnsureGlobalAudioService();
            EnsureEntityAudioService(bootstrapConfig);

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

        private static void EnsureAudioPauseDuckingBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<AudioPauseDuckingBridge>(out _))
            {
                return;
            }

            var bridge = AudioPauseDuckingBridge.EnsureCreated();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(
                typeof(AudioRuntimeComposer),
                "[Audio][BOOT] AudioPauseDuckingBridge registered (PauseStateChangedEvent -> IAudioBgmService.SetPauseDucking).",
                DebugUtility.Colors.Info);
        }

        private static void EnsureGlobalAudioService()
        {
            RegisterIfMissing(
                factory: () =>
                {
                    DependencyManager.Provider.TryGetGlobal<AudioDefaultsAsset>(out var defaults);
                    DependencyManager.Provider.TryGetGlobal<IAudioSettingsService>(out var settings);
                    DependencyManager.Provider.TryGetGlobal<IAudioRoutingResolver>(out var routing);

                    return AudioGlobalSfxService.Create(defaults, settings, routing);
                },
                alreadyRegisteredMessage: "[Audio][BOOT] IGlobalAudioService already registered.",
                registeredMessage: "[Audio][BOOT] IGlobalAudioService registered (F4/F5 direct + pooled SFX runtime).");
        }

        private static void EnsureEntityAudioService(BootstrapConfigAsset bootstrapConfig)
        {
            RegisterIfMissing<IEntityAudioService>(
                factory: () =>
                {
                    DependencyManager.Provider.TryGetGlobal<IGlobalAudioService>(out var globalAudio);
                    var semanticMap = bootstrapConfig.EntityAudioSemanticMap;

                    var service = new AudioEntitySemanticService(globalAudio, semanticMap);

                    DebugUtility.LogVerbose(
                        typeof(AudioRuntimeComposer),
                        $"[Audio][BOOT] IEntityAudioService semantic map asset='{(semanticMap != null ? semanticMap.name : "null")}'.",
                        DebugUtility.Colors.Info);

                    return service;
                },
                alreadyRegisteredMessage: "[Audio][BOOT] IEntityAudioService already registered.",
                registeredMessage: "[Audio][BOOT] IEntityAudioService registered (F6 semantic standalone runtime).");
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
