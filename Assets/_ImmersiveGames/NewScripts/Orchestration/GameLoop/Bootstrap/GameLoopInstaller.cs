using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.Commands;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunOutcome;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.Bootstrap
{
    /// <summary>
    /// Installer do GameLoop.
    ///
    /// Responsabilidade:
    /// - registrar contratos e implementaÃ§Ãµes do mÃ³dulo no boot;
    /// - nÃ£o compor runtime nem ativar bridges/hosts.
    /// </summary>
    public static class GameLoopInstaller
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            RegisterGameLoopService();
            RegisterPauseStateService();
            RegisterGameRunEndRequestService();
            RegisterGameRunPlayingStateGuard();
            RegisterGameLoopCommands();
            RegisterPauseCommands();

            _installed = true;

            DebugUtility.Log(typeof(GameLoopInstaller),
                "[OBS][GameLoop][Operational] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameLoopService()
        {
            RegisterIfMissing<IGameLoopService>(
                () => new GameLoopService(),
                "[GameLoop] IGameLoopService ja registrado no DI global.",
                "[GameLoop] GameLoopService registrado no DI global.");
        }

        private static void RegisterPauseStateService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPauseStateService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopInstaller),
                    "[OBS][GameLoop][Operational] IPauseStateService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameLoopService ausente ao registrar IPauseStateService.");
            }

            if (gameLoopService is not IPauseStateService pauseStateService)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameLoopService nao implementa IPauseStateService.");
            }

            DependencyManager.Provider.RegisterGlobal(pauseStateService);
            DebugUtility.LogVerbose(typeof(GameLoopInstaller),
                "[OBS][GameLoop][Operational] IPauseStateService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunEndRequestService()
        {
            RegisterIfMissing<IGameRunEndRequestService>(
                () => new GameRunEndRequestService(),
                "[GameLoop] IGameRunEndRequestService ja registrado no DI global.",
                "[GameLoop] GameRunEndRequestService registrado no DI global.");
        }

        private static void RegisterGameRunPlayingStateGuard()
        {
            RegisterIfMissing<IGameRunPlayingStateGuard>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameLoopService ausente ao registrar IGameRunPlayingStateGuard.");
                    }

                    return new GameRunPlayingStateGuard(gameLoopService);
                },
                "[GameLoop] IGameRunPlayingStateGuard ja registrado no DI global.",
                "[GameLoop] GameRunPlayingStateGuard registrado no DI global.");
        }

        private static void RegisterPauseCommands()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPauseCommands>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopInstaller),
                    "[OBS][GameLoop][Operational] IPauseCommands ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopCommands>(out var gameCommands) || gameCommands == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameLoopCommands ausente ao registrar IPauseCommands.");
            }

            if (gameCommands is not IPauseCommands pauseCommands)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameLoopCommands nao implementa IPauseCommands.");
            }

            DependencyManager.Provider.RegisterGlobal(pauseCommands);
            DebugUtility.LogVerbose(typeof(GameLoopInstaller),
                "[OBS][GameLoop][Operational] IPauseCommands registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameLoopCommands()
        {
            RegisterIfMissing<IGameLoopCommands>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var runEndRequestService) || runEndRequestService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameRunEndRequestService ausente ao registrar IGameLoopCommands.");
                    }

                    return new GameLoopCommands(runEndRequestService);
                },
                "[GameLoop] IGameLoopCommands ja registrado no DI global.",
                "[GameLoop] GameLoopCommands registrado no DI global.");
        }

        private static void RegisterIfMissing<T>(Func<T> factory, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(GameLoopInstaller), registeredMessage, DebugUtility.Colors.Info);
        }
    }
}
