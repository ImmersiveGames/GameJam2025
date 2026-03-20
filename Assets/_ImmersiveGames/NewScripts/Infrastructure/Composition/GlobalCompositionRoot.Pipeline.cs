using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Bindings.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.View;
using _ImmersiveGames.NewScripts.Modules.Gates;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private enum CompositionInstallStage
        {
            RuntimePolicy,
            Pooling,
            Gates,
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

            RegisterInputModeSceneFlowBridge();
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
            const string poolServiceContractTypeName =
                "_ImmersiveGames.NewScripts.Infrastructure.Pooling.Contracts.IPoolService, Assembly-CSharp";
            const string poolServiceImplementationTypeName =
                "_ImmersiveGames.NewScripts.Infrastructure.Pooling.Runtime.PoolService, Assembly-CSharp";

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[BOOT][Pooling] Installing pooling module (Package A / wiring-only).",
                DebugUtility.Colors.Info);

            var contractType = Type.GetType(poolServiceContractTypeName);
            var implementationType = Type.GetType(poolServiceImplementationTypeName);

            if (contractType == null || implementationType == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Pooling] Pooling types not found. Ensure Infrastructure/Pooling/** is part of compilation.");
                return;
            }

            if (TryResolveGlobalByType(contractType, out _))
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[BOOT][Pooling] IPoolService already registered in global DI.",
                    DebugUtility.Colors.Info);
                return;
            }

            object instance;
            try
            {
                instance = Activator.CreateInstance(implementationType);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    $"[FATAL][Pooling] Failed to instantiate PoolService. ex='{ex}'.");
                return;
            }

            RegisterGlobalByType(contractType, instance);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[BOOT][Pooling] Registered IPoolService in global DI (Package A).",
                DebugUtility.Colors.Info);
        }

        private static bool TryResolveGlobalByType(Type serviceType, out object service)
        {
            var provider = DependencyManager.Provider;
            var tryGetMethod = provider.GetType().GetMethod("TryGetGlobal");
            var genericMethod = tryGetMethod?.MakeGenericMethod(serviceType);
            if (genericMethod == null)
            {
                service = null;
                return false;
            }

            var args = new object[] { null };
            bool found = (bool)genericMethod.Invoke(provider, args);
            service = args[0];
            return found && service != null;
        }

        private static void RegisterGlobalByType(Type contractType, object instance)
        {
            var provider = DependencyManager.Provider;
            var registerMethod = provider.GetType().GetMethod("RegisterGlobal");
            var genericMethod = registerMethod?.MakeGenericMethod(contractType);
            genericMethod?.Invoke(provider, new[] { instance, false });
        }
    }
}
