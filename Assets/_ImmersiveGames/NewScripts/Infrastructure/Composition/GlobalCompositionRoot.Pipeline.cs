using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Contracts;
using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Runtime;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Input;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Camera;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // Main registration pipeline (order is dependency-driven)
        // --------------------------------------------------------------------

        private static void RegisterEssentialServicesOnly()
        {
            PrimeEventSystems();
            var bootstrapConfig = GetRequiredBootstrapConfig(out _);

            ExecuteInstallerPipeline(bootstrapConfig);

            var gateService = ResolveSimulationGateServiceOrNull();
            RegisterPauseBridge(gateService);

            RegisterStateDependentService();
            RegisterIfMissing<IGameplayCameraResolver>(() => new GameplayCameraResolver());

#if NEWSCRIPTS_BASELINE_ASSERTS
            RegisterBaselineAsserter();
#endif

            ExecuteBootstrapPipeline(bootstrapConfig);
            InitializeReadinessGate(gateService);
        }

        private static void ExecuteInstallerPipeline(BootstrapConfigAsset bootstrapConfig)
        {
            var steps = GetCompositionPipelineSteps();
            CompositionPipelineExecutor.ExecuteInstallers(steps, bootstrapConfig);

            RegisterInputModesFromRuntimeConfig();
        }

        private static void ExecuteBootstrapPipeline(BootstrapConfigAsset bootstrapConfig)
        {
            var steps = GetCompositionPipelineSteps();
            CompositionPipelineExecutor.ExecuteBootstraps(steps, bootstrapConfig);
        }

        private static void InstallGatesServices()
        {
            RegisterIfMissing<IUniqueIdFactory>(() => new UniqueIdFactory());
            RegisterIfMissing<ISimulationGateService>(() => new SimulationGateService());
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
