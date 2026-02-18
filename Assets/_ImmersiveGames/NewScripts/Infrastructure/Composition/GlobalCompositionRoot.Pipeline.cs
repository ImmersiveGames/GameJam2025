using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Infrastructure.Composition.Modules;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.View;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Application;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static CompositionInstallStage _compositionInstallStage;

        // --------------------------------------------------------------------
        // Main registration pipeline (order matters)
        // --------------------------------------------------------------------

        private static void RegisterEssentialServicesOnly()
        {
            PrimeEventSystems();

            _compositionInstallStage = CompositionInstallStage.RuntimePolicy;
            InstallCompositionModules();
            RegisterInputModesFromRuntimeConfig();

            RegisterIfMissing<IUniqueIdFactory>(() => new UniqueIdFactory());
            RegisterIfMissing<ISimulationGateService>(() => new SimulationGateService());

            // Resolve ISimulationGateService UMA vez para os consumidores (reduz repetição de TryGetGlobal).
            DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService);

            // ADR-0009: Fade module NewScripts (precisa estar antes do SceneFlowNative para o adapter resolver).
            RegisterSceneFlowFadeModule();

            RegisterPauseBridge(gateService);

            RegisterGameLoop();
            RegisterIntroStageCoordinator();
            RegisterIntroStageControlService();
            RegisterGameplaySceneClassifier();
            RegisterIntroStagePolicyResolver();
            RegisterDefaultIntroStageStep();

            // Resolve IGameLoopService UMA vez para serviços dependentes.
            DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService);

            RegisterGameRunEndRequestService();
            RegisterGameCommands();
            RegisterGameRunStatusService(gameLoopService);
            RegisterGameRunOutcomeService(gameLoopService);
            RegisterGameRunOutcomeEventInputBridge();
            RegisterPostPlayOwnershipService();

            // NewScripts standalone: registra sempre o SceneFlow nativo (sem bridge/adapters legados).
            _compositionInstallStage = CompositionInstallStage.SceneFlow;
            InstallCompositionModules();

            RegisterIfMissing(() => new WorldLifecycleSceneFlowResetDriver());
            RegisterIfMissing(() => new WorldResetService());
            RegisterIfMissing<IWorldResetRequestService>(() => new WorldResetRequestService(gateService));


            RegisterGameNavigationService();
            RegisterExitToMenuNavigationBridge();
            RegisterRestartNavigationBridge();

            RegisterSceneFlowLoadingIfAvailable();

            RegisterInputModeSceneFlowBridge();
            RegisterStateDependentService();
            RegisterIfMissing<ICameraResolver>(() => new CameraResolverService());
            // ADR-0016: ContentSwapContext precisa existir no DI global.
            RegisterIfMissing<IContentSwapContextService>(() => new ContentSwapContextService());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterIntroStageQaInstaller();
            RegisterContentSwapQaInstaller();
            RegisterSceneFlowQaInstaller();
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterIntroStageRuntimeDebugGui();
#endif

            // ContentSwapChange (InPlace-only): usa apenas ContentSwapContext e commit imediato.
            RegisterContentSwapChangeService();

#if NEWSCRIPTS_BASELINE_ASSERTS
            RegisterBaselineAsserter();
#endif

            InitializeReadinessGate(gateService);
            RegisterGameLoopSceneFlowCoordinatorIfAvailable();
        }

        private static void InstallCompositionModules()
        {
            var modules = new IGlobalCompositionModule[]
            {
                new RuntimePolicyCompositionModule(),
                new SceneFlowCompositionModule()
            };

            var context = new GlobalCompositionContext(
                _compositionInstallStage,
                installRuntimePolicy: RegisterRuntimePolicyServices,
                installSceneFlow: InstallSceneFlowServices);

            for (int i = 0; i < modules.Length; i++)
            {
                modules[i].Install(context);
            }
        }

        private static void InstallSceneFlowServices()
        {
            RegisterSceneFlowTransitionProfiles();
            RegisterSceneFlowRoutesRequired();
            RegisterSceneFlowNative();
            RegisterSceneFlowSignatureCache();
            RegisterSceneFlowRouteResetPolicy();
        }

    }
}
