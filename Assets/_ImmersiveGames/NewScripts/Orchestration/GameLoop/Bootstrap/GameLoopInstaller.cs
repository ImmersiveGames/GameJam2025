using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Ownership;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.Commands;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunOutcome;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Readiness.Runtime;
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
            RegisterGameRunOutcomeService();
            RegisterGameplaySessionOperationalSeams();

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

        private static void RegisterGameRunOutcomeService()
        {
            RegisterIfMissing<IGameRunOutcomeService>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IGameRunPlayingStateGuard>(out var gameplayStateGuard) || gameplayStateGuard == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameRunPlayingStateGuard ausente ao registrar IGameRunOutcomeService.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) || gameLoopService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameLoopService ausente ao registrar IGameRunOutcomeService.");
                    }

                    return new GameRunOutcomeService(gameplayStateGuard, gameLoopService);
                },
                "[GameLoop] IGameRunOutcomeService ja registrado no DI global.",
                "[GameLoop] GameRunOutcomeService registrado no DI global.");
        }

        private static void RegisterGameplaySessionOperationalSeams()
        {
            RegisterIntroStagePresenterScopeResolver();
            RegisterIntroStageSessionService();
            RegisterIntroStagePresenterRegistry();
            RegisterIntroStageCoordinator();
            RegisterIntroStageControlService();
            RegisterIntroStageLifecycleStateService();
            RegisterIntroStageLifecycleDispatchService();
            RegisterPhaseNextPhaseEntryHandoffService();
            RegisterPhaseNextPhaseService();
            RegisterGameplaySceneClassifier();
            RegisterDefaultIntroStageStep();
            RegisterIntroStageLifecycleOrchestrator();
        }

        private static void RegisterIntroStagePresenterScopeResolver()
        {
            RegisterIfMissing<IIntroStagePresenterScopeResolver>(
                () => new IntroStagePresenterScopeResolver(),
                "[GameLoop] IIntroStagePresenterScopeResolver ja registrado no DI global.",
                "[GameLoop] IntroStagePresenterScopeResolver registrado no DI global como seam operacional scene-local.");
        }

        private static void RegisterIntroStageSessionService()
        {
            RegisterIfMissing<IIntroStageSessionService>(
                () => new IntroStageSessionService(),
                "[GameLoop] IIntroStageSessionService ja registrado no DI global.",
                "[GameLoop] IntroStageSessionService registrado no DI global como seam operacional de GameplaySessionFlow.");
        }

        private static void RegisterIntroStagePresenterRegistry()
        {
            RegisterIfMissing<IIntroStagePresenterRegistry>(
                () => new IntroStagePresenterHost(),
                "[GameLoop] IIntroStagePresenterRegistry ja registrado no DI global.",
                "[GameLoop] IntroStagePresenterHost registrado no DI global como resolver scene-local de GameplaySessionFlow.");
        }

        private static void RegisterIntroStageCoordinator()
        {
            RegisterIfMissing<IIntroStageCoordinator>(
                () => new IntroStageCoordinator(),
                "[GameLoop] IIntroStageCoordinator ja registrado no DI global.",
                "[GameLoop] IntroStageCoordinator registrado no DI global como support service de GameplaySessionFlow.");
        }

        private static void RegisterIntroStageControlService()
        {
            RegisterIfMissing<IIntroStageControlService>(
                () => new IntroStageControlService(),
                "[GameLoop] IIntroStageControlService ja registrado no DI global.",
                "[GameLoop] IntroStageControlService registrado no DI global como support service de GameplaySessionFlow.");
        }

        private static void RegisterGameplaySceneClassifier()
        {
            RegisterIfMissing<IGameplaySceneClassifier>(
                () => new DefaultGameplaySceneClassifier(),
                "[GameLoop] IGameplaySceneClassifier ja registrado no DI global.",
                "[GameLoop] DefaultGameplaySceneClassifier registrado no DI global como support service de GameplaySessionFlow.");
        }

        private static void RegisterDefaultIntroStageStep()
        {
            RegisterIfMissing<IIntroStageStep>(
                () => new ConfirmToStartIntroStageStep(),
                "[GameLoop] IIntroStageStep ja registrado no DI global.",
                "[GameLoop] ConfirmToStartIntroStageStep registrado no DI global como support service de IntroStage.");
        }

        private static void RegisterIntroStageLifecycleOrchestrator()
        {
            RegisterIfMissing<IntroStageLifecycleOrchestrator>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IIntroStageLifecycleStateService>(out var stateService) || stateService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][GameLoop] IIntroStageLifecycleStateService ausente ao registrar IntroStageLifecycleOrchestrator.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IIntroStageLifecycleDispatchService>(out var dispatchService) || dispatchService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][GameLoop] IIntroStageLifecycleDispatchService ausente ao registrar IntroStageLifecycleOrchestrator.");
                    }

                    return new IntroStageLifecycleOrchestrator(stateService, dispatchService);
                },
                "[GameLoop] IntroStageLifecycleOrchestrator ja registrado no DI global.",
                "[GameLoop] IntroStageLifecycleOrchestrator registrado no DI global como gate operacional de GameplaySessionFlow.");
        }

        private static void RegisterIntroStageLifecycleStateService()
        {
            RegisterIfMissing<IIntroStageLifecycleStateService>(
                () => new IntroStageLifecycleStateService(),
                "[GameLoop] IIntroStageLifecycleStateService ja registrado no DI global.",
                "[GameLoop] IntroStageLifecycleStateService registrado no DI global como seam operacional de eligibilidade/deferencia.");
        }

        private static void RegisterIntroStageLifecycleDispatchService()
        {
            RegisterIfMissing<IIntroStageLifecycleDispatchService>(
                () =>
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IIntroStagePresenterRegistry>(out var presenterRegistry) || presenterRegistry == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][GameLoop] IIntroStagePresenterRegistry ausente ao registrar IntroStageLifecycleDispatchService.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var coordinator) || coordinator == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][GameLoop] IIntroStageCoordinator ausente ao registrar IntroStageLifecycleDispatchService.");
                    }

                    return new IntroStageLifecycleDispatchService(presenterRegistry, coordinator);
                },
                "[GameLoop] IIntroStageLifecycleDispatchService ja registrado no DI global.",
                "[GameLoop] IntroStageLifecycleDispatchService registrado no DI global como seam operacional de despacho/no-content.");
        }

        private static void RegisterPhaseNextPhaseEntryHandoffService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseEntryHandoffService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopInstaller),
                    "[OBS][GameLoop][Operational] IPhaseNextPhaseEntryHandoffService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseSelectionService>(out var phaseNextPhaseSelectionService) || phaseNextPhaseSelectionService == null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopInstaller),
                    "[OBS][GameLoop][Operational] NextPhase entry handoff skipped because phase rail is not active yet.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageSessionService>(out var introStageSessionService) || introStageSessionService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IIntroStageSessionService missing from global DI before next-phase entry handoff registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageLifecycleDispatchService>(out var introStageLifecycleDispatchService) || introStageLifecycleDispatchService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IIntroStageLifecycleDispatchService missing from global DI before next-phase entry handoff registration.");
            }

            DependencyManager.Provider.RegisterGlobal<IPhaseNextPhaseEntryHandoffService>(
                new PhaseNextPhaseEntryHandoffService(introStageSessionService, introStageLifecycleDispatchService));
            DebugUtility.LogVerbose(typeof(GameLoopInstaller),
                "[GameLoop] PhaseNextPhaseEntryHandoffService registrado no DI global como bridge estreito de next-phase.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPhaseNextPhaseService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopInstaller),
                    "[OBS][GameLoop][Operational] IPhaseNextPhaseService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseSelectionService>(out var selectionService) || selectionService == null)
            {
                DebugUtility.LogVerbose(typeof(GameLoopInstaller),
                    "[OBS][GameLoop][Operational] NextPhase service skipped because phase rail is not active yet.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseCompositionService>(out var compositionService) || compositionService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IPhaseNextPhaseCompositionService missing from global DI before next-phase service registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseEntryHandoffService>(out var handoffService) || handoffService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IPhaseNextPhaseEntryHandoffService missing from global DI before next-phase service registration.");
            }

            DependencyManager.Provider.RegisterGlobal<IPhaseNextPhaseService>(
                new PhaseNextPhaseService(selectionService, compositionService, handoffService));
            DebugUtility.LogVerbose(typeof(GameLoopInstaller),
                "[GameLoop] PhaseNextPhaseService registrado no DI global como orquestrador fino de next-phase.",
                DebugUtility.Colors.Info);
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
