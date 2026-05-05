using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Bridges;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.LegacySimulationGate;
using _ImmersiveGames.NewScripts.Foundation.Platform.LegacySimulationGate.Interop;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Commands;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;

namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Installers
{
    /// <summary>
    /// Installer temporario de compatibilidade de pause legado.
    /// </summary>
    public static class LegacyPauseCompatibilityInstaller
    {
        private static bool _installed;
        private static bool _runtimeComposed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            RegisterGameLoopCommands();
            RegisterPauseCommands();

            _installed = true;

            DebugUtility.Log(typeof(LegacyPauseCompatibilityInstaller),
                "[OBS][LegacyPause][Compatibility] Installer concluido.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime()
        {
            if (_runtimeComposed)
            {
                return;
            }

            EnsurePauseBridge();
            EnsureAudioPauseDuckingBridge();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(LegacyPauseCompatibilityInstaller),
                "[OBS][LegacyPause][Compatibility] Runtime composition concluida.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPauseCommands()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPauseCommands>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(LegacyPauseCompatibilityInstaller),
                    "[OBS][LegacyPause][Compatibility] IPauseCommands ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopCommands>(out var gameCommands) || gameCommands == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LegacyPause] IGameLoopCommands ausente ao registrar IPauseCommands.");
            }

            if (gameCommands is not IPauseCommands pauseCommands)
            {
                throw new InvalidOperationException("[FATAL][Config][LegacyPause] IGameLoopCommands nao implementa IPauseCommands.");
            }

            DependencyManager.Provider.RegisterGlobal(pauseCommands);
            DebugUtility.LogVerbose(typeof(LegacyPauseCompatibilityInstaller),
                "[OBS][LegacyPause][Compatibility] IPauseCommands registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameLoopCommands()
        {
            RegisterIfMissing<IGameLoopCommands>(
                () => new GameLoopCommands(),
                "[LegacyPause][Compatibility] IGameLoopCommands ja registrado no DI global.",
                "[LegacyPause][Compatibility] GameLoopCommands registrado no DI global.");
        }

        private static void EnsurePauseBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GamePauseGateBridge>(out _))
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILegacySimulationGateService>(out var gateService) || gateService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LegacyPause] ILegacySimulationGateService ausente no DI global antes de registrar o GamePauseGateBridge.");
            }

            var bridge = new GamePauseGateBridge(gateService);
            DependencyManager.Provider.RegisterGlobal(bridge);
        }

        private static void EnsureAudioPauseDuckingBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<AudioPauseDuckingBridge>(out _))
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IAudioBgmService>(out var bgmService) || bgmService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LegacyPause] IAudioBgmService ausente no DI global antes de registrar o AudioPauseDuckingBridge.");
            }

            var bridge = AudioPauseDuckingBridge.EnsureCreated(bgmService);
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(LegacyPauseCompatibilityInstaller),
                "[OBS][LegacyPause][Compatibility] AudioPauseDuckingBridge composto.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(LegacyPauseCompatibilityInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(LegacyPauseCompatibilityInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
