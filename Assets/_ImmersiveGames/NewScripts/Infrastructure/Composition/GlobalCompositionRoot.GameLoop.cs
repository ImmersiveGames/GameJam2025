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
                "[GameLoop] Register summary: bootstrapEnsureExecuted=1.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunEndRequestService()
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var existing) || existing == null)
            {
                RegisterIfMissing<IGameRunEndRequestService>(() => new GameRunEndRequestService());
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[GameLoop] RegisterGameRunEndRequestService summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameCommands()
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<IGameCommands>(out var existing) || existing == null)
            {
                DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var runEndRequestService);
                RegisterIfMissing<IGameCommands>(() => new GameCommands(runEndRequestService));
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[GameCommands] Register summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunStatusService(IGameLoopService gameLoopService)
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<IGameRunStateService>(out var existing) || existing == null)
            {
                // Mantém compatibilidade com a assinatura atual do GameRunStateService (injeção por construtor).
                RegisterIfMissing<IGameRunStateService>(() => new GameRunStateService(gameLoopService));
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[GameLoop] RegisterGameRunStatusService summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeService(IGameLoopService gameLoopService)
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var existing) || existing == null)
            {
                // Mantém compatibilidade com a assinatura atual do GameRunOutcomeService (injeção por construtor).
                RegisterIfMissing<IGameRunOutcomeService>(() => new GameRunOutcomeService(gameLoopService));
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[GameLoop] RegisterGameRunOutcomeService summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeEventInputBridge()
        {
            bool added = false;
            int degraded = 0;

            if (!DependencyManager.Provider.TryGetGlobal<GameRunOutcomeCommandBridge>(out var existing) || existing == null)
            {
                if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcomeService) || outcomeService == null)
                {
                    degraded = 1;
                    DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                        "[GameLoop] Não foi possível registrar GameRunOutcomeCommandBridge: IGameRunOutcomeService não disponível.");
                }
                else
                {
                    RegisterIfMissing(() => new GameRunOutcomeCommandBridge(outcomeService));
                    added = true;
                }
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[GameLoop] RegisterGameRunOutcomeEventInputBridge summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}, degraded={degraded}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPostPlayOwnershipService()
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<IPostGameOwnershipService>(out var existing) || existing == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPostGameOwnershipService>(
                    new PostGameOwnershipService());
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[PostPlay] Register summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStageCoordinator()
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var existing) || existing == null)
            {
                DependencyManager.Provider.RegisterGlobal<IIntroStageCoordinator>(
                    new IntroStageCoordinator());
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[IntroStageController] RegisterIntroStageCoordinator summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStageControlService()
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var existing) || existing == null)
            {
                DependencyManager.Provider.RegisterGlobal<IIntroStageControlService>(
                    new IntroStageControlService());
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[IntroStageController] RegisterIntroStageControlService summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStagePolicyResolver()
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<IIntroStagePolicyResolver>(out var existing) || existing == null)
            {
                var classifier = DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var resolved) && resolved != null
                    ? resolved
                    : new DefaultGameplaySceneClassifier();

                DependencyManager.Provider.RegisterGlobal<IIntroStagePolicyResolver>(
                    new DefaultIntroStagePolicyResolver(classifier));
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[IntroStageController] RegisterIntroStagePolicyResolver summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplaySceneClassifier()
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var existing) || existing == null)
            {
                DependencyManager.Provider.RegisterGlobal<IGameplaySceneClassifier>(
                    new DefaultGameplaySceneClassifier());
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[Gameplay] RegisterGameplaySceneClassifier summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterDefaultIntroStageStep()
        {
            bool added = false;
            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageStep>(out var existing) || existing == null)
            {
                DependencyManager.Provider.RegisterGlobal<IIntroStageStep>(
                    new ConfirmToStartIntroStageStep());
                added = true;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[IntroStageController] RegisterDefaultIntroStageStep summary: added={(added ? 1 : 0)}, skippedAlready={(added ? 0 : 1)}.",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
    }
}
