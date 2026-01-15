// Assets/_ImmersiveGames/NewScripts/Infrastructure/GlobalBootstrap.cs

/*
 * ChangeLog
 * - Registrado IPhaseContextService (PhaseContextService) no DI global (ADR-0016).
 * - Adicionado PhaseContextSceneFlowBridge para limpar Pending no SceneTransitionStarted (Baseline 3B).
 * - Adicionado GamePauseGateBridge para refletir pause/resume no SimulationGate sem congelar física.
 * - StateDependentService agora usa apenas NewScriptsStateDependentService (legacy removido).
 * - Entrada de infraestrutura mínima (Gate/WorldLifecycle/DI/Câmera/StateBridge) para NewScripts.
 * - (Opção B) Registrado GameLoopSceneFlowCoordinator para coordenar Start via SceneFlow
 *   (GameStartRequestedEvent -> Transition -> ScenesReady -> RequestStart/Ready).
 *
 * Ajustes (jan/2026):
 * - Reduzidas resoluções repetidas no DI global (evita warnings de "chamada repetida" no frame 0):
 *   - Resolve IGameLoopService uma vez e injeta nos registradores de GameRunStatus/Outcome.
 *   - Resolve ISimulationGateService uma vez e injeta em GameReadinessService e PauseBridge.
 * - Removido registro duplicado de WorldLifecycleRuntimeCoordinator (centralizado em RegisterSceneFlowNative()).
 *
 * Nota (QA):
 * - O coordinator NÃO deve cachear IGameLoopService; deve resolver no momento do sync
 *   para que overrides de QA no DI sejam observados.
 *
 * Reorganização (jan/2026):
 * - Arquivo reordenado por seções (Init -> Pipeline -> Registradores -> Helpers), sem mudar assinaturas.
 */
using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Gameplay.Phases;
using _ImmersiveGames.NewScripts.Gameplay.PostGame;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.Baseline;
using _ImmersiveGames.NewScripts.Infrastructure.Cameras;
using _ImmersiveGames.NewScripts.Infrastructure.Debug;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Gameplay;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
using _ImmersiveGames.NewScripts.Infrastructure.InputSystems;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Fade;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
using _ImmersiveGames.NewScripts.QA.Pregame;
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
        // --------------------------------------------------------------------
        // State / Constants
        // --------------------------------------------------------------------

        private static bool _initialized;
        private static GameReadinessService _gameReadinessService;

        // Opção B: mantém referência viva do coordinator (evita GC / descarte prematuro).
        private static GameLoopSceneFlowCoordinator _sceneFlowCoordinator;

        // Profile fixo do start (para filtrar ScenesReady/ResetCompleted).
        private static readonly SceneFlowProfileId StartProfileId = SceneFlowProfileId.Startup;

        // Scene names (Unity: SceneManager.GetActiveScene().name)
        private const string SceneNewBootstrap = "NewBootstrap";
        private const string SceneMenu = "MenuScene";
        private const string SceneUIGlobal = "UIGlobalScene";

        // --------------------------------------------------------------------
        // Entry
        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // Main registration pipeline (order matters)
        // --------------------------------------------------------------------

        private static void RegisterEssentialServicesOnly()
        {
            PrimeEventSystems();

            RegisterIfMissing<IUniqueIdFactory>(() => new NewUniqueIdFactory());
            RegisterIfMissing<ISimulationGateService>(() => new SimulationGateService());

            // Resolve ISimulationGateService UMA vez para os consumidores (reduz repetição de TryGetGlobal).
            DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService);

            // ADR-0009: Fade module NewScripts (precisa estar antes do SceneFlowNative para o adapter resolver).
            RegisterSceneFlowFadeModule();

            RegisterPauseBridge(gateService);

            RegisterGameLoop();
            RegisterPregameCoordinator();
            RegisterPregameControlService();
            RegisterGameplaySceneClassifier();
            RegisterPregamePolicyResolver();
            RegisterDefaultPregameStep();

            // Resolve IGameLoopService UMA vez para serviços dependentes.
            DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService);

            RegisterGameRunEndRequestService();
            RegisterGameCommands();
            RegisterGameRunStatusService(gameLoopService);
            RegisterGameRunOutcomeService(gameLoopService);
            RegisterGameRunOutcomeEventInputBridge();
            RegisterPostPlayOwnershipService();

            // NewScripts standalone: registra sempre o SceneFlow nativo (sem bridge/adapters legados).
            RegisterSceneFlowNative();
            RegisterSceneFlowSignatureCache();
            RegisterWorldResetRequestService();

            RegisterGameNavigationService();
            RegisterExitToMenuNavigationBridge();
            RegisterRestartNavigationBridge();

            RegisterSceneFlowLoadingIfAvailable();

            RegisterInputModeSceneFlowBridge();
            RegisterPhaseStartPhaseCommitBridge();

            RegisterStateDependentService();
            RegisterIfMissing<ICameraResolver>(() => new CameraResolverService());
            // ADR-0016: PhaseContext precisa existir no DI global.
            RegisterIfMissing<IPhaseContextService>(() => new PhaseContextService());

            RegisterDebugOverlay();
            RegisterPregameQaInstaller();

            // Baseline 3B: Pending NÃO pode atravessar transição.
            RegisterPhaseContextSceneFlowBridge();

            RegisterPhaseTransitionIntentRegistry();

            // PhaseChange depende de PhaseContext + SceneFlow/WorldReset (para "in place" vs "transition").
            RegisterPhaseChangeService();

#if NEWSCRIPTS_BASELINE_ASSERTS
            RegisterBaselineAsserter();
#endif

            InitializeReadinessGate(gateService);
            RegisterGameLoopSceneFlowCoordinatorIfAvailable();
        }

        // --------------------------------------------------------------------
        // Event systems
        // --------------------------------------------------------------------

        private static void PrimeEventSystems()
        {
            EventBus<GameStartRequestedEvent>.Clear();
            EventBus<GamePauseCommandEvent>.Clear();
            EventBus<GameResumeRequestedEvent>.Clear();
            EventBus<GameExitToMenuRequestedEvent>.Clear();
            EventBus<GameResetRequestedEvent>.Clear();
            EventBus<GameLoopActivityChangedEvent>.Clear();
            EventBus<GameRunStartedEvent>.Clear();
            EventBus<GameRunEndedEvent>.Clear();
            EventBus<GameRunEndRequestedEvent>.Clear();
            EventBus<PhaseCommittedEvent>.Clear();
            EventBus<PhasePendingSetEvent>.Clear();
            EventBus<PhasePendingClearedEvent>.Clear();

            // Scene Flow (NewScripts): evita bindings duplicados quando domain reload está desativado.
            EventBus<SceneTransitionStartedEvent>.Clear();
            EventBus<SceneTransitionFadeInCompletedEvent>.Clear();
            EventBus<SceneTransitionScenesReadyEvent>.Clear();
            EventBus<SceneTransitionBeforeFadeOutEvent>.Clear();
            EventBus<SceneTransitionCompletedEvent>.Clear();

            // WorldLifecycle (NewScripts): reset completion gate depende deste evento.
            EventBus<WorldLifecycleResetCompletedEvent>.Clear();

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[EventBus] EventBus inicializado (GameLoop + SceneFlow + WorldLifecycle).",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
        // Fade / Loading
        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // Pause / Readiness gate
        // --------------------------------------------------------------------

        private static void RegisterPauseBridge(ISimulationGateService gateService)
        {
            if (DependencyManager.Provider.TryGetGlobal<GamePauseGateBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[Pause] GamePauseGateBridge já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (gateService == null)
            {
                if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out gateService) || gateService == null)
                {
                    DebugUtility.LogError(typeof(GlobalBootstrap),
                        "[Pause] ISimulationGateService indisponível; GamePauseGateBridge não pôde ser inicializado.");
                    return;
                }
            }

            var bridge = new GamePauseGateBridge(gateService);
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Pause] GamePauseGateBridge registrado (EventBus → SimulationGate).",
                DebugUtility.Colors.Info);
        }

        private static void InitializeReadinessGate(ISimulationGateService gateService)
        {
            if (gateService == null)
            {
                // fallback: tenta resolver aqui (best-effort)
                if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out gateService) || gateService == null)
                {
                    DebugUtility.LogError(typeof(GlobalBootstrap),
                        "[Readiness] ISimulationGateService indisponível. Scene Flow readiness ficará sem proteção de gate.");
                    return;
                }
            }

            if (DependencyManager.Provider.TryGetGlobal<GameReadinessService>(out var registered) && registered != null)
            {
                _gameReadinessService = registered;
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[Readiness] GameReadinessService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            _gameReadinessService = new GameReadinessService(gateService);
            DependencyManager.Provider.RegisterGlobal(_gameReadinessService);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Readiness] GameReadinessService inicializado e registrado no DI global (Scene Flow → SimulationGate).",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
        // GameLoop / GameRun
        // --------------------------------------------------------------------

        private static void RegisterGameLoop()
        {
            GameLoopBootstrap.Ensure();
            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[GameLoop] GameLoopBootstrap.EnsureRegistered() executado (serviço + bridge no escopo global).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunEndRequestService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[GameLoop] IGameRunEndRequestService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterIfMissing<IGameRunEndRequestService>(() => new GameRunEndRequestService());

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[GameLoop] GameRunEndRequestService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameCommands()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameCommands>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[GameCommands] IGameCommands já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var runEndRequestService);

            RegisterIfMissing<IGameCommands>(() => new GameCommands(runEndRequestService));

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[GameCommands] GameCommands registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunStatusService(IGameLoopService gameLoopService)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunStatusService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[GameLoop] IGameRunStatusService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            // Mantém compatibilidade com a assinatura atual do GameRunStatusService (injeção por construtor).
            RegisterIfMissing<IGameRunStatusService>(() => new GameRunStatusService(gameLoopService));

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[GameLoop] GameRunStatusService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeService(IGameLoopService gameLoopService)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[GameLoop] IGameRunOutcomeService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            // Mantém compatibilidade com a assinatura atual do GameRunOutcomeService (injeção por construtor).
            RegisterIfMissing<IGameRunOutcomeService>(() => new GameRunOutcomeService(gameLoopService));

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[GameLoop] GameRunOutcomeService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeEventInputBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameRunOutcomeEventInputBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[GameLoop] GameRunOutcomeEventInputBridge já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcomeService) || outcomeService == null)
            {
                DebugUtility.LogWarning(typeof(GlobalBootstrap),
                    "[GameLoop] Não foi possível registrar GameRunOutcomeEventInputBridge: IGameRunOutcomeService não disponível.");
                return;
            }

            RegisterIfMissing(() => new GameRunOutcomeEventInputBridge(outcomeService));

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[GameLoop] GameRunOutcomeEventInputBridge registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPostPlayOwnershipService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPostPlayOwnershipService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[PostPlay] IPostPlayOwnershipService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IPostPlayOwnershipService>(
                new PostPlayOwnershipService(),
                allowOverride: false);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[PostPlay] PostPlayOwnershipService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPregameCoordinator()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPregameCoordinator>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[IntroStage] IPregameCoordinator já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IPregameCoordinator>(
                new PregameCoordinator(),
                allowOverride: false);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[IntroStage] PregameCoordinator registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPregameControlService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPregameControlService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[IntroStage] IPregameControlService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IPregameControlService>(
                new PregameControlService(),
                allowOverride: false);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[IntroStage] PregameControlService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPregamePolicyResolver()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPregamePolicyResolver>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[IntroStage] IPregamePolicyResolver já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var classifier = DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var resolved) && resolved != null
                ? resolved
                : new DefaultGameplaySceneClassifier();

            DependencyManager.Provider.RegisterGlobal<IPregamePolicyResolver>(
                new DefaultPregamePolicyResolver(classifier),
                allowOverride: false);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[IntroStage] DefaultPregamePolicyResolver registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplaySceneClassifier()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[Gameplay] IGameplaySceneClassifier já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IGameplaySceneClassifier>(
                new DefaultGameplaySceneClassifier(),
                allowOverride: false);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Gameplay] IGameplaySceneClassifier registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterDefaultPregameStep()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPregameStep>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[IntroStage] IPregameStep já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IPregameStep>(
                new ConfirmToStartPregameStep(),
                allowOverride: false);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[IntroStage] ConfirmToStartPregameStep registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
        // SceneFlow / WorldLifecycle
        // --------------------------------------------------------------------

        private static void RegisterSceneFlowNative()
        {
            // Runtime coordinator precisa estar vivo quando o completion gate estiver ativo.
            RegisterIfMissing(() => new WorldLifecycleRuntimeCoordinator());

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
                var resetGate = new WorldLifecycleResetCompletionGate(timeoutMs: 20000);
                completionGate = resetGate;
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

        private static void RegisterSceneFlowSignatureCache()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowSignatureCache>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[SceneFlow] ISceneFlowSignatureCache já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<ISceneFlowSignatureCache>(
                new SceneFlowSignatureCache(),
                allowOverride: false);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[SceneFlow] SceneFlowSignatureCache registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterWorldResetRequestService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IWorldResetRequestService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[WorldLifecycle] IWorldResetRequestService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<WorldLifecycleRuntimeCoordinator>(out var coordinator) || coordinator == null)
            {
                DebugUtility.LogWarning(typeof(GlobalBootstrap),
                    "[WorldLifecycle] WorldLifecycleRuntimeCoordinator indisponível. IWorldResetRequestService não será registrado.");
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IWorldResetRequestService>(coordinator, allowOverride: false);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[WorldLifecycle] IWorldResetRequestService registrado no DI global (via WorldLifecycleRuntimeCoordinator).",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
        // Navigation / InputMode
        // --------------------------------------------------------------------

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

        private static void RegisterRestartNavigationBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<RestartNavigationBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[Navigation] RestartNavigationBridge ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bridge = new RestartNavigationBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[Navigation] RestartNavigationBridge registrado no DI global.",
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

        private static void RegisterPhaseStartPhaseCommitBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<PhaseStartPhaseCommitBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[PhaseStart] PhaseStartPhaseCommitBridge já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bridge = new PhaseStartPhaseCommitBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[PhaseStart] PhaseStartPhaseCommitBridge registrado no DI global (PhaseCommitted -> IntroStage).",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
        // PhaseContext (Baseline 3B)
        // --------------------------------------------------------------------

        private static void RegisterPhaseContextSceneFlowBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<PhaseContextSceneFlowBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[PhaseContext] PhaseContextSceneFlowBridge já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bridge = new PhaseContextSceneFlowBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[PhaseContext] PhaseContextSceneFlowBridge registrado no DI global (SceneFlow -> ClearPending).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPregameQaInstaller()
        {
            try
            {
                PregameQaInstaller.EnsureInstalled();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(GlobalBootstrap),
                    $"[QA][IntroStage] Falha ao instalar PregameQaContextMenu no bootstrap. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static void RegisterDebugOverlay()
        {
#if NEWSCRIPTS_MODE
            DebugOverlayController.EnsureInstalled();
#endif
        }

        // --------------------------------------------------------------------
        // StateDependent / Camera
        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // Coordinator (production start)
        // --------------------------------------------------------------------

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
                transitionProfileId: StartProfileId);

            _sceneFlowCoordinator = new GameLoopSceneFlowCoordinator(sceneFlow, startPlan);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                $"[GameLoopSceneFlow] Coordinator registrado (startPlan production, profile='{StartProfileId}').",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
        // Baseline (optional)
        // --------------------------------------------------------------------

#if NEWSCRIPTS_BASELINE_ASSERTS
        private static void RegisterBaselineAsserter()
        {
            if (BaselineInvariantAsserter.TryInstall())
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[Baseline] BaselineInvariantAsserter ativo (NEWSCRIPTS_BASELINE_ASSERTS).",
                    DebugUtility.Colors.Info);
            }
        }
#endif

        // --------------------------------------------------------------------
        // DI helper
        // --------------------------------------------------------------------

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
        private static void RegisterPhaseChangeService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPhaseChangeService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[PhaseChange] IPhaseChangeService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseContextService>(out var phaseContext) || phaseContext == null)
            {
                DebugUtility.LogWarning(typeof(GlobalBootstrap),
                    "[PhaseChange] IPhaseContextService indisponível. IPhaseChangeService não será registrado.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IWorldResetRequestService>(out var worldReset) || worldReset == null)
            {
                DebugUtility.LogWarning(typeof(GlobalBootstrap),
                    "[PhaseChange] IWorldResetRequestService indisponível. IPhaseChangeService não será registrado.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                DebugUtility.LogWarning(typeof(GlobalBootstrap),
                    "[PhaseChange] ISceneTransitionService indisponível. IPhaseChangeService não será registrado.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseTransitionIntentRegistry>(out var intentRegistry) || intentRegistry == null)
            {
                DebugUtility.LogWarning(typeof(GlobalBootstrap),
                    "[PhaseChange] IPhaseTransitionIntentRegistry indisponível. IPhaseChangeService não será registrado.");
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IPhaseChangeService>(
                new PhaseChangeService(phaseContext, worldReset, sceneFlow, intentRegistry),
                allowOverride: false);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[PhaseChange] PhaseChangeService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPhaseTransitionIntentRegistry()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPhaseTransitionIntentRegistry>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                    "[PhaseIntent] IPhaseTransitionIntentRegistry já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IPhaseTransitionIntentRegistry>(
                new PhaseTransitionIntentRegistry(),
                allowOverride: false);

            DebugUtility.LogVerbose(typeof(GlobalBootstrap),
                "[PhaseIntent] PhaseTransitionIntentRegistry registrado no DI global.",
                DebugUtility.Colors.Info);
        }

    }
}
