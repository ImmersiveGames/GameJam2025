/*
 * ChangeLog
 * - Adicionado GamePauseGateBridge para refletir pause/resume no SimulationGate sem congelar física.
 * - StateDependentService agora usa apenas NewScriptsStateDependentService (legacy removido).
 * - Entrada de infraestrutura mínima (Gate/WorldLifecycle/DI/Câmera/StateBridge) para NewScripts.
 * - (Opção B) Registrado GameLoopSceneFlowCoordinator para coordenar Start via SceneFlow
 *   (GameStartCommandEvent -> Transition -> ScenesReady -> RequestStart).
 *
 * Nota (QA):
 * - O coordinator NÃO deve cachear IGameLoopService; deve resolver no momento do ScenesReady
 *   para que overrides de QA no DI sejam observados.
 */
using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.Cameras;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
using _ImmersiveGames.NewScripts.Infrastructure.InputSystems;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Fade;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
using UnityEngine;
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

        // Opção B: mantém referência viva do coordinator (evita GC / descarte prematuro).
        private static GameLoopSceneFlowCoordinator _sceneFlowCoordinator;

        // Profile fixo do start (para filtrar ScenesReady/ResetCompleted).
        private const string StartProfileName = SceneFlowProfileNames.Startup;

        // Scene names (Unity: SceneManager.GetActiveScene().name)
        private const string SceneNewBootstrap = "NewBootstrap";
        private const string SceneMenu = "MenuScene";
        private const string SceneUIGlobal = "UIGlobalScene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if !NEWSCRIPTS_MODE
            DebugUtility.Log(typeof(GlobalBootstrap),
                "NEWSCRIPTS_MODE desativado: GlobalBootstrap ignorado.");
            return;
#else
            if (_initialized)
                return;

            _initialized = true;

            InitializeLogging();
            EnsureDependencyProvider();
            RegisterEssentialServicesOnly();
            InitializeReadinessGate();
            RegisterGameLoopSceneFlowCoordinatorIfAvailable();

            DebugUtility.Log(
                typeof(GlobalBootstrap),
                "✅ NewScripts global infrastructure initialized (Commit 1 minimal).",
                DebugUtility.Colors.Success);
#endif
        }

        private static void InitializeLogging()
        {
            DebugUtility.SetDefaultDebugLevel(DebugLevel.Verbose);
            DebugUtility.LogVerbose(typeof(GlobalBootstrap), "NewScripts logging configured.");
        }

        private static void EnsureDependencyProvider()
        {
            if (DependencyManager.HasInstance)
                return;

            _ = DependencyManager.Provider;
            DebugUtility.LogVerbose(typeof(GlobalBootstrap), "DependencyManager created for global scope.");
        }

        private static void RegisterEssentialServicesOnly()
        {
            PrimeEventSystems();

            RegisterIfMissing<IUniqueIdFactory>(() => new NewUniqueIdFactory());
            RegisterIfMissing<ISimulationGateService>(() => new SimulationGateService());

            // ADR-0009: Fade module NewScripts (precisa estar antes do SceneFlowNative para o adapter resolver).
            RegisterSceneFlowFadeModule();

            RegisterPauseBridge();
            RegisterGameLoop();
            RegisterGameRunStatusService();

            // NewScripts standalone: registra sempre o SceneFlow nativo (sem bridge/adapters legados).
            RegisterSceneFlowNative();

            RegisterGameNavigationService();
            RegisterExitToMenuNavigationBridge();

            RegisterSceneFlowLoadingIfAvailable();

            RegisterIfMissing(() => new WorldLifecycleRuntimeCoordinator());

            RegisterInputModeSceneFlowBridge();

            RegisterStateDependentService();
            RegisterIfMissing<ICameraResolver>(() => new CameraResolverService());
        }

        private static void RegisterSceneFlowFadeModule()
        {
            // Registra o serviço de fade NewScripts no DI global.
            RegisterIfMissing<INewScriptsFadeService>(() => new NewScriptsFadeService());

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Fade] INewScriptsFadeService registrado no DI global (ADR-0009).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowLoadingIfAvailable()
        {
            RegisterIfMissing<INewScriptsLoadingHudService>(() => new NewScriptsLoadingHudService());

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Loading] INewScriptsLoadingHudService registrado no DI global.",
                DebugUtility.Colors.Info);

            if (DependencyManager.Provider.TryGetGlobal<SceneFlowLoadingService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[Loading] SceneFlowLoadingService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterIfMissing(() => new SceneFlowLoadingService());

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Loading] SceneFlowLoadingService registrado no DI global.",
                DebugUtility.Colors.Info);
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
            EventBus<GameStartRequestedEvent>.Clear();
            EventBus<GamePauseCommandEvent>.Clear();
            EventBus<GameResumeRequestedEvent>.Clear();
            EventBus<GameExitToMenuRequestedEvent>.Clear();
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
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[Readiness] GameReadinessService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            _gameReadinessService = new GameReadinessService(gate);
            DependencyManager.Provider.RegisterGlobal(_gameReadinessService);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Readiness] GameReadinessService inicializado e registrado no DI global (Scene Flow → SimulationGate).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowNative()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[SceneFlow] SceneTransitionService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            // Loader/Fade (NewScripts standalone)
            var loaderAdapter = NewScriptsSceneFlowAdapters.CreateLoaderAdapter();
            var fadeAdapter = NewScriptsSceneFlowAdapters.CreateFadeAdapter(DependencyManager.Provider);

            // Gate para segurar FadeOut/Completed até WorldLifecycle reset concluir.
            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionCompletionGate>(out var completionGate) || completionGate == null)
            {
                completionGate = new WorldLifecycleResetCompletionGate(timeoutMs: 20000);
                DependencyManager.Provider.RegisterGlobal(completionGate, allowOverride: false);

                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[SceneFlow] ISceneTransitionCompletionGate registrado (WorldLifecycleResetCompletionGate).",
                    DebugUtility.Colors.Info);
            }

            var service = new SceneTransitionService(loaderAdapter, fadeAdapter, completionGate);
            DependencyManager.Provider.RegisterGlobal<ISceneTransitionService>(service);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                $"[SceneFlow] SceneTransitionService nativo registrado (Loader={loaderAdapter.GetType().Name}, FadeAdapter={fadeAdapter.GetType().Name}, Gate={completionGate.GetType().Name}).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameNavigationService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[Navigation] IGameNavigationService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                DebugUtility.LogWarning(typeof(GlobalBootstrap),
                    "[Navigation] ISceneTransitionService indisponível. IGameNavigationService não será registrado.");
                return;
            }

            var service = new GameNavigationService(sceneFlow);
            DependencyManager.Provider.RegisterGlobal<IGameNavigationService>(service);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Navigation] GameNavigationService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterExitToMenuNavigationBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<ExitToMenuNavigationBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[Navigation] ExitToMenuNavigationBridge ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bridge = new ExitToMenuNavigationBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Navigation] ExitToMenuNavigationBridge registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterInputModeSceneFlowBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<InputModeSceneFlowBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[InputMode] InputModeSceneFlowBridge ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bridge = new InputModeSceneFlowBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[InputMode] InputModeSceneFlowBridge registrado no DI global.",
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
            GameLoopBootstrap.Ensure();
            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[GameLoop] GameLoopBootstrap.EnsureRegistered() executado (serviço + bridge no escopo global).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunStatusService()
        {
            RegisterIfMissing<IGameRunStatusService>(() => new GameRunStatusService());

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[GameLoop] GameRunStatusService registrado no DI global.",
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

                DependencyManager.Provider.RegisterGlobal<IStateDependentService>(
                    new NewScriptsStateDependentService(),
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

        private static void RegisterGameLoopSceneFlowCoordinatorIfAvailable()
        {
            if (_sceneFlowCoordinator != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[GameLoopSceneFlow] Coordinator já está registrado (static reference).",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[GameLoopSceneFlow] ISceneTransitionService indisponível. Coordinator não será registrado.",
                    DebugUtility.Colors.Info);
                return;
            }

            // Plano de produção:
            // NewBootstrap -> (Fade) -> Load(Menu + UIGlobal) -> Active=Menu -> Unload(NewBootstrap) -> (FadeOut) -> Completed
            var startPlan = new SceneTransitionRequest(
                scenesToLoad: new[] { SceneMenu, SceneUIGlobal },
                scenesToUnload: new[] { SceneNewBootstrap },
                targetActiveScene: SceneMenu,
                useFade: true,
                transitionProfileName: StartProfileName);

            _sceneFlowCoordinator = new GameLoopSceneFlowCoordinator(sceneFlow, startPlan);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                $"[GameLoopSceneFlow] Coordinator registrado (startPlan production, profile='{StartProfileName}').",
                DebugUtility.Colors.Info);
        }
    }
}
