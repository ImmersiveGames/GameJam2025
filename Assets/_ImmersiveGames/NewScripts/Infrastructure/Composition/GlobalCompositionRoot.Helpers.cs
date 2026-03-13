using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gates;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // --------------------------------------------------------------------
        // DI helpers
        // --------------------------------------------------------------------

        private static void RegisterIfMissing<T>(Func<T> factory) where T : class
        {
            RegisterIfMissing(
                factory,
                alreadyRegisteredMessage: $"Global service already present: {typeof(T).Name}.",
                registeredMessage: $"Registered global service: {typeof(T).Name}.");
        }

        private static void RegisterIfMissing<T>(
            Func<T> factory,
            string alreadyRegisteredMessage,
            string registeredMessage) where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                if (!string.IsNullOrWhiteSpace(alreadyRegisteredMessage))
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                }

                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);

            if (!string.IsNullOrWhiteSpace(registeredMessage))
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot), registeredMessage, DebugUtility.Colors.Info);
            }
        }

        private static ISimulationGateService ResolveSimulationGateServiceOrNull()
        {
            DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService);
            return gateService;
        }
    }
}