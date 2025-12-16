using System;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure
{
    /// <summary>
    /// Entry point for the new isolated project. It wires global infrastructure
    /// without altering or refactoring the legacy systems.
    /// </summary>
    public static class GlobalBootstrap
    {
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            InitializeLogging();
            EnsureDependencyProvider();
            RegisterEssentialServices();

            DebugUtility.Log(
                typeof(GlobalBootstrap),
                "✅ Global infrastructure initialized (no gameplay started).",
                DebugUtility.Colors.Success);
        }

        private static void InitializeLogging()
        {
            DebugUtility.SetDefaultDebugLevel(DebugLevel.Warning);
            DebugUtility.LogVerbose(
                typeof(GlobalBootstrap),
                "Logging configured for new project bootstrap.");
        }

        private static void EnsureDependencyProvider()
        {
            if (!DependencyManager.HasInstance)
            {
                _ = DependencyManager.Provider;
                DebugUtility.LogVerbose(
                    typeof(GlobalBootstrap),
                    "DependencyManager created for global scope.");
            }
        }

        private static void RegisterEssentialServices()
        {
            RegisterIfMissing(() => new UniqueIdFactory());
            RegisterIfMissing(() => new SimulationGateService());
        }

        private static void RegisterIfMissing<T>(Func<T> factory) where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var service) && service != null)
            {
                return;
            }

            var instance = factory();
            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(
                typeof(GlobalBootstrap),
                $"Registered global service: {typeof(T).Name}.");
        }
    }
}
