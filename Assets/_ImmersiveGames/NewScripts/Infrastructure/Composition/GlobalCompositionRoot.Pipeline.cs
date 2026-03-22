using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Contracts;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Runtime;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Bindings.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.View;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private enum CompositionInstallStage
        {
            RuntimePolicy,
            Pooling,
            Gates,
            Audio,
            GameLoop,
            SceneFlow,
            WorldLifecycle,
            Navigation,
            Levels,
            ContentSwap
        }

        private static CompositionInstallStage _compositionInstallStage;

        // --------------------------------------------------------------------
        // Main registration pipeline (order matters)
        // --------------------------------------------------------------------

        private static void RegisterEssentialServicesOnly()
        {
            PrimeEventSystems();

            _compositionInstallStage = CompositionInstallStage.RuntimePolicy;
            InstallCompositionModules();

            _compositionInstallStage = CompositionInstallStage.Pooling;
            InstallCompositionModules();
            RegisterInputModesFromRuntimeConfig();

            _compositionInstallStage = CompositionInstallStage.Gates;
            InstallCompositionModules();

            var gateService = ResolveSimulationGateServiceOrNull();

            RegisterPauseBridge(gateService);

            _compositionInstallStage = CompositionInstallStage.Audio;
            InstallCompositionModules();

            _compositionInstallStage = CompositionInstallStage.GameLoop;
            InstallCompositionModules();

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

            RegisterExitToMenuCoordinator();
            RegisterMacroRestartCoordinator();
            RegisterLevelSelectedRestartSnapshotBridge();
            RegisterNavigationLevelRouteBgmBridge();

            RegisterInputModeCoordinator();
            RegisterSceneFlowInputModeBridge();

            RegisterLevelStageOrchestrator();
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
            switch (_compositionInstallStage)
            {
                case CompositionInstallStage.RuntimePolicy:
                    RegisterRuntimePolicyServices();
                    break;
                case CompositionInstallStage.Pooling:
                    InstallPoolingServices();
                    break;
                case CompositionInstallStage.Gates:
                    InstallGatesServices();
                    break;
                case CompositionInstallStage.Audio:
                    InstallAudioServices();
                    break;
                case CompositionInstallStage.GameLoop:
                    InstallGameLoopServices();
                    break;
                case CompositionInstallStage.SceneFlow:
                    InstallSceneFlowServices();
                    break;
                case CompositionInstallStage.WorldLifecycle:
                    InstallWorldLifecycleServices();
                    break;
                case CompositionInstallStage.Navigation:
                    InstallNavigationServices();
                    break;
                case CompositionInstallStage.Levels:
                    RegisterLevelsServices();
                    break;
                case CompositionInstallStage.ContentSwap:
                    InstallContentSwapServices();
                    break;
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
            GameStartRequestEmitter.EnsureInstalled();

            DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService);

            RegisterGameRunStatusService(gameLoopService);
            RegisterGameRunOutcomeService(gameLoopService);
            RegisterGameRunOutcomeEventInputBridge();
            RegisterPostGameResultService();
            RegisterPostPlayOwnershipService();
        }

        private static void InstallNavigationServices()
        {
            RegisterGameNavigationService();
        }

        private static void InstallContentSwapServices()
        {
            RegisterIfMissing<IContentSwapContextService>(() => new ContentSwapContextService());
            RegisterContentSwapChangeService();
        }

        private static void InstallPoolingServices()
        {
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[BOOT][Pooling] Installing pooling module (Package B runtime core).",
                DebugUtility.Colors.Info);

            RegisterIfMissing<IPoolService>(
                () => new PoolService(),
                alreadyRegisteredMessage: "[BOOT][Pooling] IPoolService already registered in global DI.",
                registeredMessage: "[BOOT][Pooling] Registered IPoolService in global DI (Package B).");
        }
    }
}
