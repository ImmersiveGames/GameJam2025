using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Contracts;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Runtime;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Input;
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
            WorldReset,
            Navigation,
            SceneComposition,
            LevelFlow
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

            _compositionInstallStage = CompositionInstallStage.WorldReset;
            InstallCompositionModules();

            _compositionInstallStage = CompositionInstallStage.Navigation;
            InstallCompositionModules();

            _compositionInstallStage = CompositionInstallStage.SceneComposition;
            InstallCompositionModules();

            _compositionInstallStage = CompositionInstallStage.LevelFlow;
            InstallCompositionModules();

            RegisterExitToMenuCoordinator();
            RegisterMacroRestartCoordinator();
            RegisterLevelSelectedRestartSnapshotBridge();
            RegisterNavigationLevelRouteBgmBridge();

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
                case CompositionInstallStage.WorldReset:
                    InstallWorldResetServices();
                    break;
                case CompositionInstallStage.Navigation:
                    InstallNavigationServices();
                    break;
                case CompositionInstallStage.SceneComposition:
                    InstallSceneCompositionServices();
                    break;
                case CompositionInstallStage.LevelFlow:
                    InstallLevelFlowServices();
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
            GameLoopStartRequestEmitter.EnsureInstalled();

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
