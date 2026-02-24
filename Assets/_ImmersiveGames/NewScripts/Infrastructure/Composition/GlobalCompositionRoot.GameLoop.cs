using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Bindings.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Commands;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Services;
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
                "[GameLoop] GameLoopBootstrap.EnsureRegistered() executado (serviço + bridge no escopo global).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunEndRequestService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] IGameRunEndRequestService já registrado no DI global.",
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
            if (DependencyManager.Provider.TryGetGlobal<IGameCommands>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameCommands] IGameCommands já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var runEndRequestService);

            RegisterIfMissing<IGameCommands>(() => new GameCommands(runEndRequestService));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameCommands] GameCommands registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunStatusService(IGameLoopService gameLoopService)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunStateService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] IGameRunStateService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            // Mantém compatibilidade com a assinatura atual do GameRunStateService (injeção por construtor).
            RegisterIfMissing<IGameRunStateService>(() => new GameRunStateService(gameLoopService));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunStateService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeService(IGameLoopService gameLoopService)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] IGameRunOutcomeService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            // Mantém compatibilidade com a assinatura atual do GameRunOutcomeService (injeção por construtor).
            RegisterIfMissing<IGameRunOutcomeService>(() => new GameRunOutcomeService(gameLoopService));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunOutcomeService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeEventInputBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameRunOutcomeCommandBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] GameRunOutcomeCommandBridge já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcomeService) || outcomeService == null)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    "[GameLoop] Não foi possível registrar GameRunOutcomeCommandBridge: IGameRunOutcomeService não disponível.");
                return;
            }

            RegisterIfMissing(() => new GameRunOutcomeCommandBridge(outcomeService));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunOutcomeCommandBridge registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPostPlayOwnershipService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPostGameOwnershipService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[PostPlay] IPostGameOwnershipService já registrado no DI global.",
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
                    "[IntroStageController] IIntroStageCoordinator já registrado no DI global.",
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
                    "[IntroStageController] IIntroStageControlService já registrado no DI global.",
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
                    "[IntroStageController] IIntroStagePolicyResolver já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var classifier = DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var resolved) && resolved != null
                ? resolved
                : new DefaultGameplaySceneClassifier();

            DependencyManager.Provider.RegisterGlobal<IIntroStagePolicyResolver>(
                new DefaultIntroStagePolicyResolver(classifier));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[IntroStageController] DefaultIntroStagePolicyResolver registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplaySceneClassifier()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Gameplay] IGameplaySceneClassifier já registrado no DI global.",
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
                    "[IntroStageController] IIntroStageStep já registrado no DI global.",
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
