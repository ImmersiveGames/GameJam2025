/*
 * ChangeLog
 * - Adicionado GamePauseGateBridge para refletir pause/resume no SimulationGate sem congelar física.
 * - StateDependentService agora usa apenas NewScriptsStateDependentService (legacy removido).
 * - Entrada de infraestrutura mínima (Gate/WorldLifecycle/DI/Câmera/StateBridge) para NewScripts.
 */
using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.Cameras;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
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
#if !NEWSCRIPTS_MODE
            DebugUtility.Log(typeof(GlobalBootstrap),
                "NEWSCRIPTS_MODE desativado: GlobalBootstrap ignorado.");
            return;
#endif
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
            PrimeEventSystems();

            // NewScripts generic ID factory (no gameplay semantics).
            RegisterIfMissing<IUniqueIdFactory>(() => new NewUniqueIdFactory());

            // Simulation Gate agora vive em NewScripts (gate oficial para novos sistemas).
            RegisterIfMissing<ISimulationGateService>(() => new SimulationGateService());

            RegisterPauseBridge();
            RegisterGameLoop();

            // Driver de runtime do WorldLifecycle (produção, sem dependência de QA runners).
            RegisterIfMissing(() => new WorldLifecycleRuntimeDriver());

            // Bridge oficial de permissões de ações (gate-aware).
            RegisterStateDependentService();

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

        private static void PrimeEventSystems()
        {
            EventBus<GameStartEvent>.Clear();
            EventBus<GamePauseEvent>.Clear();
            EventBus<GameResumeRequestedEvent>.Clear();
            EventBus<GameResetRequestedEvent>.Clear();

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[EventBus] EventBus inicializado para eventos do GameLoop (NewScripts).",
                DebugUtility.Colors.Info);
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

        private static void RegisterPauseBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GamePauseGateBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[Pause] GamePauseGateBridge já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService) || gateService == null)
            {
                DebugUtility.LogError(typeof(GlobalBootstrap),
                    "[Pause] ISimulationGateService indisponível; GamePauseGateBridge não pôde ser inicializado.");
                return;
            }

            var bridge = new GamePauseGateBridge(gateService);
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Pause] GamePauseGateBridge registrado (EventBus → SimulationGate).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameLoop()
        {
            GameLoopBootstrap.EnsureRegistered();
            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[GameLoop] GameLoopBootstrap.EnsureRegistered() executado (serviço + bridge no escopo global).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterStateDependentService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IStateDependentService>(out var existing) && existing != null)
            {
                if (existing is NewScriptsStateDependentService)
                {
                    DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                        "[StateDependent] NewScriptsStateDependentService já registrado no DI global.",
                        DebugUtility.Colors.Info);
                    return;
                }

                DebugUtility.LogWarning(typeof(GlobalBootstrap),
                    $"[StateDependent] Serviço registrado ({existing.GetType().Name}) não usa gate; substituindo por NewScriptsStateDependentService.");

                DependencyManager.Provider.RegisterGlobal<IStateDependentService>(new NewScriptsStateDependentService(),
                    allowOverride: true);

                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[StateDependent] Registrado NewScriptsStateDependentService (gate-aware) como IStateDependentService.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterIfMissing<IStateDependentService>(() => new NewScriptsStateDependentService());

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[StateDependent] Registrado NewScriptsStateDependentService (gate-aware) como IStateDependentService.",
                DebugUtility.Colors.Info);
        }
    }
}
