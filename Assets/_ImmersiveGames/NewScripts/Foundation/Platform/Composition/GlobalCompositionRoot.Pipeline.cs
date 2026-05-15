using _ImmersiveGames.NewScripts.Foundation.Core.Identifiers;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // Main registration pipeline (order is dependency-driven)
        // --------------------------------------------------------------------

        private static void RegisterEssentialServicesOnly()
        {
            PrimeEventSystems();
            var runtimeModeConfig = GetRequiredRuntimeModeConfig(out _);

            ExecuteInstallerPipeline(runtimeModeConfig);

#if NEWSCRIPTS_BASELINE_ASSERTS
            RegisterBaselineAsserter();
#endif

            ExecuteBootstrapPipeline(runtimeModeConfig);
        }

        private static void ExecuteInstallerPipeline(RuntimeModeConfig runtimeModeConfig)
        {
            var steps = GetCompositionPipelineSteps(runtimeModeConfig);
            CompositionPipelineExecutor.ExecuteInstallers(steps, runtimeModeConfig);
        }

        private static void ExecuteBootstrapPipeline(RuntimeModeConfig runtimeModeConfig)
        {
            var steps = GetCompositionPipelineSteps(runtimeModeConfig);
            CompositionPipelineExecutor.ExecuteBootstraps(steps, runtimeModeConfig);
        }

        private static void InstallGatesServices()
        {
            RegisterIfMissing<IUniqueIdFactory>(() => new UniqueIdFactory());
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

