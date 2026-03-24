using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Input;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Run;
using _ImmersiveGames.NewScripts.Modules.PostGame;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // GameLoop / GameRun
        // --------------------------------------------------------------------

        private static void RegisterGameLoop()
        {
            GameLoopBootstrap.Ensure(includeGameRunServices: false, includeOutcomeEventInputBridge: false);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameLoopBootstrap.EnsureRegistered() executado (servico + bridge no escopo global).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunEndRequestService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] IGameRunEndRequestService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterIfMissing<IGameRunEndRequestService>(() => new GameRunEndRequestService());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunEndRequestService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameCommands()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameLoopCommands>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoopCommands] IGameLoopCommands ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var runEndRequestService);

            RegisterIfMissing<IGameLoopCommands>(() => new GameLoopCommands(runEndRequestService));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoopCommands] GameLoopCommands registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunGameplayStateGuard(IGameLoopService gameLoopService)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunPlayingStateGuard>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] IGameRunPlayingStateGuard ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (gameLoopService == null)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    "[GameLoop] Nao foi possivel registrar IGameRunPlayingStateGuard: IGameLoopService indisponivel.");
                return;
            }

            RegisterIfMissing<IGameRunPlayingStateGuard>(() => new GameRunPlayingStateGuard(gameLoopService));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunPlayingStateGuard registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunStatusService(IGameLoopService gameLoopService)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunResultSnapshotService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] IGameRunResultSnapshotService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterIfMissing<IGameRunResultSnapshotService>(() => new GameRunResultSnapshotService());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunResultSnapshotService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeService(IGameLoopService gameLoopService)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] IGameRunOutcomeService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterGameRunGameplayStateGuard(gameLoopService);

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunPlayingStateGuard>(out var gameplayStateGuard) || gameplayStateGuard == null)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    "[GameLoop] Nao foi possivel registrar GameRunOutcomeService: IGameRunPlayingStateGuard nao disponivel.");
                return;
            }

            RegisterIfMissing<IGameRunOutcomeService>(() => new GameRunOutcomeService(gameplayStateGuard));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunOutcomeService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeEventInputBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameRunOutcomeRequestBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] GameRunOutcomeRequestBridge ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcomeService) || outcomeService == null)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    "[GameLoop] Nao foi possivel registrar GameRunOutcomeRequestBridge: IGameRunOutcomeService nao disponivel.");
                return;
            }

            RegisterIfMissing(() => new GameRunOutcomeRequestBridge(outcomeService));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunOutcomeRequestBridge registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPostGameResultService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPostGameResultService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[PostGame] IPostGameResultService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IPostGameResultService>(new PostGameResultService());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[PostGame] PostGameResultService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPostPlayOwnershipService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPostGameOwnershipService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[PostPlay] IPostGameOwnershipService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IPostGameOwnershipService>(
                new PostGameOwnershipService());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[PostPlay] PostGameOwnershipService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStageCoordinator()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[IntroStageController] IIntroStageCoordinator ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IIntroStageCoordinator>(
                new IntroStageCoordinator());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[IntroStageController] IntroStageCoordinator registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStageControlService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[IntroStageController] IIntroStageControlService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IIntroStageControlService>(
                new IntroStageControlService());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[IntroStageController] IntroStageControlService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStagePolicyResolver()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStagePolicyResolver>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[IntroStageController] IIntroStagePolicyResolver ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IIntroStagePolicyResolver>(
                new DefaultIntroStagePolicyResolver());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[IntroStageController] DefaultIntroStagePolicyResolver registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplaySceneClassifier()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Gameplay] IGameplaySceneClassifier ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IGameplaySceneClassifier>(
                new DefaultGameplaySceneClassifier());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Gameplay] IGameplaySceneClassifier registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterDefaultIntroStageStep()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageStep>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[IntroStageController] IIntroStageStep ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IIntroStageStep>(
                new ConfirmToStartIntroStageStep());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[IntroStageController] ConfirmToStartIntroStageStep registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
    }
}
