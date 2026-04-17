using ImmersiveGames.GameJam2025.Core.Identifiers;
using ImmersiveGames.GameJam2025.Infrastructure.Config;
using ImmersiveGames.GameJam2025.Infrastructure.Pooling.Contracts;
using ImmersiveGames.GameJam2025.Infrastructure.Pooling.Runtime;
using ImmersiveGames.GameJam2025.Infrastructure.SimulationGate;
using ImmersiveGames.GameJam2025.Core.Logging;
namespace ImmersiveGames.GameJam2025.Infrastructure.Composition
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

            var gateService = ResolveSimulationGateServiceOrFail();

#if NEWSCRIPTS_BASELINE_ASSERTS
            RegisterBaselineAsserter();
#endif

            ExecuteBootstrapPipeline(bootstrapConfig);
            InitializeReadinessGate(gateService);
        }

        private static void ExecuteInstallerPipeline(BootstrapConfigAsset bootstrapConfig)
        {
            var steps = GetCompositionPipelineSteps(bootstrapConfig);
            CompositionPipelineExecutor.ExecuteInstallers(steps, bootstrapConfig);

            RegisterInputModesFromRuntimeConfig();
        }

        private static void ExecuteBootstrapPipeline(BootstrapConfigAsset bootstrapConfig)
        {
            var steps = GetCompositionPipelineSteps(bootstrapConfig);
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

