using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Input;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Run;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Bootstrap
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
            RegisterGameRunOutcomeService();
            RegisterGameLoopCommands();
            RegisterPauseCommands();
            RegisterIntroStageCoordinator();
            RegisterIntroStageControlService();
            RegisterGameplaySceneClassifier();
            RegisterDefaultIntroStageStep();

            _installed = true;

            DebugUtility.Log(typeof(GameLoopInstaller),
                "[GameLoop] Module installer concluido.",
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
                    "[GameLoop] IPauseStateService ja registrado no DI global.",
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
                "[GameLoop] IPauseStateService registrado no DI global.",
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
                    "[GameLoop] IPauseCommands ja registrado no DI global.",
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
                "[GameLoop] IPauseCommands registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeService()
        {
            RegisterIfMissing<IGameRunOutcomeService>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IGameRunPlayingStateGuard>(out var gameplayStateGuard) || gameplayStateGuard == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameRunPlayingStateGuard ausente ao registrar IGameRunOutcomeService.");
                    }

                    return new GameRunOutcomeService(gameplayStateGuard);
                },
                "[GameLoop] IGameRunOutcomeService ja registrado no DI global.",
                "[GameLoop] GameRunOutcomeService registrado no DI global.");
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

        private static void RegisterIntroStageCoordinator()
        {
            RegisterIfMissing<IIntroStageCoordinator>(
                () => new IntroStageCoordinator(),
                "[IntroStageController] IIntroStageCoordinator ja registrado no DI global.",
                "[IntroStageController] IntroStageCoordinator registrado no DI global.");
        }

        private static void RegisterIntroStageControlService()
        {
            RegisterIfMissing<IIntroStageControlService>(
                () => new IntroStageControlService(),
                "[IntroStageController] IIntroStageControlService ja registrado no DI global.",
                "[IntroStageController] IntroStageControlService registrado no DI global.");
        }

        private static void RegisterGameplaySceneClassifier()
        {
            RegisterIfMissing<IGameplaySceneClassifier>(
                () => new DefaultGameplaySceneClassifier(),
                "[Gameplay] IGameplaySceneClassifier ja registrado no DI global.",
                "[Gameplay] IGameplaySceneClassifier registrado no DI global.");
        }

        private static void RegisterDefaultIntroStageStep()
        {
            RegisterIfMissing<IIntroStageStep>(
                () => new ConfirmToStartIntroStageStep(),
                "[IntroStageController] IIntroStageStep ja registrado no DI global.",
                "[IntroStageController] ConfirmToStartIntroStageStep registrado no DI global.");
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
