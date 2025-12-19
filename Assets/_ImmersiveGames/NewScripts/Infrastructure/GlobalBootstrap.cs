using System;
using _ImmersiveGames.NewScripts.Infrastructure.Cameras;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.NewScripts.Infrastructure.State.Legacy;
using _ImmersiveGames.Scripts.StateMachineSystems;
using IUniqueIdFactory = _ImmersiveGames.NewScripts.Infrastructure.Ids.IUniqueIdFactory;

namespace _ImmersiveGames.NewScripts.Infrastructure
{
    /// <summary>
    /// Entry point for the NewScripts project area.
    /// Commit 1: minimal global infrastructure (no gameplay, no spawn, no scene transitions).
    /// </summary>
    public static class GlobalBootstrap
    {
        private static bool _initialized;
        private static GameReadinessService _gameReadinessService;

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
            RegisterEssentialServicesOnly();
            InitializeReadinessGate();

            DebugUtility.Log(
                typeof(GlobalBootstrap),
                "✅ NewScripts global infrastructure initialized (Commit 1 minimal).",
                DebugUtility.Colors.Success);
        }

        private static void InitializeLogging()
        {
            DebugUtility.SetDefaultDebugLevel(DebugLevel.Verbose);
            DebugUtility.LogVerbose(typeof(GlobalBootstrap), "NewScripts logging configured.");
        }

        private static void EnsureDependencyProvider()
        {
            if (DependencyManager.HasInstance)
            {
                return;
            }
            _ = DependencyManager.Provider;
            DebugUtility.LogVerbose(typeof(GlobalBootstrap), "DependencyManager created for global scope.");
        }

        private static void RegisterEssentialServicesOnly()
        {
            // NewScripts generic ID factory (no gameplay semantics).
            RegisterIfMissing<IUniqueIdFactory>(() => new NewUniqueIdFactory());

            // Simulation Gate agora vive em NewScripts (gate oficial para novos sistemas).
            RegisterIfMissing<ISimulationGateService>(() => new SimulationGateService());

            // Driver de runtime do WorldLifecycle (produção, sem dependência de QA runners).
            RegisterIfMissing(() => new WorldLifecycleRuntimeDriver());

            // TEMP bridge até o FSM novo ser implementado (NS-FSM-001).
            RegisterIfMissing<IStateDependentService>(() => new LegacyStateDependentServiceBridge());

            // Sistema de câmera nativo do NewScripts.
            RegisterIfMissing<ICameraResolver>(() => new CameraResolverService());
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

        private static void InitializeReadinessGate()
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gate) || gate == null)
            {
                DebugUtility.LogError(typeof(GlobalBootstrap),
                    "[Readiness] ISimulationGateService indisponível. Scene Flow readiness ficará sem proteção de gate.");
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<GameReadinessService>(out var registered) && registered != null)
            {
                _gameReadinessService = registered;
                DebugUtility.LogVerbose(typeof(GlobalBootstrap), "[Readiness] GameReadinessService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            _gameReadinessService = new GameReadinessService(gate);
            DependencyManager.Provider.RegisterGlobal(_gameReadinessService);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Readiness] GameReadinessService inicializado e registrado no DI global (Scene Flow → SimulationGate).",
                DebugUtility.Colors.Info);
        }
    }
}
