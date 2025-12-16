using System;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure
{
    /// <summary>
    /// Entry point for the NewScripts project area.
    /// Commit 1: minimal global infrastructure (no gameplay, no spawn, no scene transitions).
    /// Commit 2: ensures Scene scope bootstrapper exists to validate scene-scope lifecycle.
    /// </summary>
    public static class GlobalBootstrap
    {
        private static bool _initialized;
        private static GameObject _sceneBootstrapperGo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            InitializeLogging();
            EnsureDependencyProvider();
            RegisterEssentialServicesOnly();

            DebugUtility.Log(
                typeof(GlobalBootstrap),
                "✅ NewScripts global infrastructure initialized (Commit 1 minimal + Commit 2 hook).",
                DebugUtility.Colors.Success);
        }

        private static void InitializeLogging()
        {
            DebugUtility.SetDefaultDebugLevel(DebugLevel.Verbose);
            DebugUtility.LogVerbose(typeof(GlobalBootstrap), "NewScripts logging configured.");
        }

        private static void EnsureDependencyProvider()
        {
            if (!DependencyManager.HasInstance)
            {
                _ = DependencyManager.Provider;
                DebugUtility.LogVerbose(typeof(GlobalBootstrap), "DependencyManager created for global scope.");
            }
        }

        private static void RegisterEssentialServicesOnly()
        {
            RegisterIfMissing<IUniqueIdFactory>(() => new UniqueIdFactory());
            RegisterIfMissing<ISimulationGateService>(() => new SimulationGateService());
        }

        private static void RegisterIfMissing<T>(Func<T> factory) where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap), $"Global service already present: {typeof(T).Name}.");
                return;
            }

            var instance = factory();
            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(GlobalBootstrap), $"Registered global service: {typeof(T).Name}.");
        }
    }
}
