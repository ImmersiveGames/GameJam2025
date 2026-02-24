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

            _compositionInstallStage = CompositionInstallStage.Gates;
            InstallCompositionModules();

            // Resolve ISimulationGateService UMA vez para os consumidores (reduz repetição de TryGetGlobal).
            DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService);

            RegisterPauseBridge(gateService);

            _compositionInstallStage = CompositionInstallStage.GameLoop;
            InstallCompositionModules();

            // NewScripts standalone: registra sempre o SceneFlow nativo (sem bridge/adapters legados).
            _compositionInstallStage = CompositionInstallStage.SceneFlow;
            InstallCompositionModules();

            _compositionInstallStage = CompositionInstallStage.WorldLifecycle;
            InstallCompositionModules();

            _compositionInstallStage = CompositionInstallStage.Navigation;
            InstallCompositionModules();

            _compositionInstallStage = CompositionInstallStage.Levels;
            InstallCompositionModules();

            _compositionInstallStage = CompositionInstallStage.ContentSwap;
            InstallCompositionModules();

            _compositionInstallStage = CompositionInstallStage.DevQA;
            InstallCompositionModules();

            RegisterExitToMenuNavigationBridge();
            RegisterRestartNavigationBridge();
            RegisterLevelSelectedRestartSnapshotBridge();
            RegisterRestartSnapshotContentSwapBridge();

            RegisterInputModeSceneFlowBridge();
            RegisterStateDependentService();
            RegisterIfMissing<ICameraResolver>(() => new CameraResolverService());

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
                new GatesCompositionModule(),
                new GameLoopCompositionModule(),
                new SceneFlowCompositionModule(),
                new WorldLifecycleCompositionModule(),
                new NavigationCompositionModule(),
                new LevelsCompositionModule(),
                new ContentSwapCompositionModule(),
                new DevQaCompositionModule()
            };

            var context = new GlobalCompositionContext(
                _compositionInstallStage,
                installRuntimePolicy: RegisterRuntimePolicyServices,
                installSceneFlow: InstallSceneFlowServices,
                installLevels: RegisterLevelsServices,
                installGates: InstallGatesServices,
                installGameLoop: InstallGameLoopServices,
                installWorldLifecycle: InstallWorldLifecycleServices,
                installNavigation: InstallNavigationServices,
                installContentSwap: InstallContentSwapServices,
                installDevQa: InstallDevQaServices);

            for (int i = 0; i < modules.Length; i++)
            {
                modules[i].Install(context);
            }
        }

        private static void InstallGatesServices()
        {
            RegisterIfMissing<IUniqueIdFactory>(() => new UniqueIdFactory());
            RegisterIfMissing<ISimulationGateService>(() => new SimulationGateService());
        }

        private static void InstallGameLoopServices()
        {
            RegisterGameLoop();
            RegisterIntroStageCoordinator();
            RegisterIntroStageControlService();
            RegisterGameplaySceneClassifier();
            RegisterIntroStagePolicyResolver();
            RegisterDefaultIntroStageStep();

            RegisterGameRunEndRequestService();
            RegisterGameCommands();

            // Resolve IGameLoopService UMA vez para serviços dependentes.
            DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService);

            RegisterGameRunStatusService(gameLoopService);
            RegisterGameRunOutcomeService(gameLoopService);
            RegisterGameRunOutcomeEventInputBridge();
            RegisterPostPlayOwnershipService();
        }

        private static void InstallWorldLifecycleServices()
        {
            RegisterIfMissing(() => new WorldLifecycleSceneFlowResetDriver());
            RegisterIfMissing(() => new WorldResetService());
            RegisterIfMissing<IWorldResetCommands>(() => new WorldResetCommands());

            DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService);
            RegisterIfMissing<IWorldResetRequestService>(() => new WorldResetRequestService(gateService));
        }

        private static void InstallNavigationServices()
        {
            RegisterGameNavigationService();
        }

        private static void InstallContentSwapServices()
        {
            // ADR-0016: ContentSwapContext precisa existir no DI global.
            RegisterIfMissing<IContentSwapContextService>(() => new ContentSwapContextService());

            // ContentSwapChange (InPlace-only): usa apenas ContentSwapContext e commit imediato.
            RegisterContentSwapChangeService();
        }

        private static void InstallDevQaServices()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterIntroStageQaInstaller();
            RegisterContentSwapQaInstaller();
            RegisterSceneFlowQaInstaller();
            RegisterIntroStageRuntimeDebugGui();
#endif
        }

        private static void InstallSceneFlowServices()
        {
            // ADR-0009: Fade module NewScripts (precisa estar antes do SceneFlowNative para o adapter resolver).
            RegisterSceneFlowFadeModule();

            RegisterSceneFlowTransitionProfiles();
            RegisterSceneFlowRoutesRequired();
            RegisterSceneFlowNative();
            RegisterSceneFlowSignatureCache();
            RegisterSceneFlowRouteResetPolicy();

            // ADR-0010: mantém o Loading no final da instalação do SceneFlow
            // para preservar o ponto de registro equivalente do pipeline.
            RegisterSceneFlowLoadingIfAvailable();
        }

    }
}
