/*
 * ChangeLog
 * - Registrado IContentSwapContextService (ContentSwapContextService) no DI global (ADR-0016).
 * - ContentSwap permanece InPlace-only (sem integração com SceneFlow).
 * - Adicionado GamePauseGateBridge para refletir pause/resume no SimulationGate sem congelar física.
 * - StateDependentService agora usa apenas StateDependentService (legacy removido).
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
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Identifiers;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.ContentSwap;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Dev;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Dev.Runtime;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Bindings.Bootstrap;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Commands;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Dev;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Services;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actions.States;
using _ImmersiveGames.NewScripts.Modules.Gameplay.View;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.Gates.Interop;
using _ImmersiveGames.NewScripts.Modules.InputModes.Interop;
using _ImmersiveGames.NewScripts.Modules.Levels;
using _ImmersiveGames.NewScripts.Modules.Levels.Dev;
using _ImmersiveGames.NewScripts.Modules.Levels.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.PostGame;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Dev;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Application;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Policies;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    /// <summary>
    /// Entry point for the NewScripts project area.
    /// Commit 1: minimal global infrastructure (no gameplay, no spawn, no scene transitions).
    /// </summary>
    public static class GlobalCompositionRoot
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
            DebugUtility.Log(typeof(GlobalCompositionRoot),
                "NEWSCRIPTS_MODE desativado: GlobalCompositionRoot ignorado.");
            return;
#else
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            InitializeLogging();
            EnsureDependencyProvider();
            RegisterEssentialServicesOnly();

            DebugUtility.Log(
                typeof(GlobalCompositionRoot),
                "✅ NewScripts global infrastructure initialized (Commit 1 minimal).",
                DebugUtility.Colors.Success);
#endif
        }

        private static void InitializeLogging()
        {
            DebugUtility.SetDefaultDebugLevel(DebugLevel.Verbose);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot), "NewScripts logging configured.");
        }

        private static void EnsureDependencyProvider()
        {
            if (DependencyManager.HasInstance)
            {
                return;
            }

            _ = DependencyManager.Provider;
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot), "DependencyManager created for global scope.");
        }

        // --------------------------------------------------------------------
        // Main registration pipeline (order matters)
        // --------------------------------------------------------------------

        private static void RegisterEssentialServicesOnly()
        {
            PrimeEventSystems();

            RegisterRuntimePolicyServices();

            RegisterIfMissing<IUniqueIdFactory>(() => new UniqueIdFactory());
            RegisterIfMissing<ISimulationGateService>(() => new SimulationGateService());

            // Resolve ISimulationGateService UMA vez para os consumidores (reduz repetição de TryGetGlobal).
            DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService);

            // ADR-0009: Fade module NewScripts (precisa estar antes do SceneFlowNative para o adapter resolver).
            RegisterSceneFlowFadeModule();

            RegisterPauseBridge(gateService);

            RegisterGameLoop();
            RegisterIntroStageCoordinator();
            RegisterIntroStageControlService();
            RegisterGameplaySceneClassifier();
            RegisterIntroStagePolicyResolver();
            RegisterDefaultIntroStageStep();

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

            RegisterIfMissing(() => new WorldLifecycleSceneFlowResetDriver());
            RegisterIfMissing(() => new WorldResetService());
            RegisterIfMissing<IWorldResetRequestService>(() => new WorldResetRequestService(gateService));


            RegisterGameNavigationService();
            RegisterExitToMenuNavigationBridge();
            RegisterRestartNavigationBridge();

            RegisterSceneFlowLoadingIfAvailable();

            RegisterInputModeSceneFlowBridge();
            RegisterStateDependentService();
            RegisterIfMissing<ICameraResolver>(() => new CameraResolverService());
            // ADR-0016: ContentSwapContext precisa existir no DI global.
            RegisterIfMissing<IContentSwapContextService>(() => new ContentSwapContextService());

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterIntroStageQaInstaller();
            RegisterContentSwapQaInstaller();
            RegisterSceneFlowQaInstaller();
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterIntroStageRuntimeDebugGui();
#endif

            // ContentSwapChange (InPlace-only): usa apenas ContentSwapContext e commit imediato.
            RegisterContentSwapChangeService();
            RegisterLevelServices();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterLevelQaInstaller();
#endif

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
            EventBus<ContentSwapCommittedEvent>.Clear();
            EventBus<ContentSwapPendingSetEvent>.Clear();
            EventBus<ContentSwapPendingClearedEvent>.Clear();

            // Scene Flow (NewScripts): evita bindings duplicados quando domain reload está desativado.
            EventBus<SceneTransitionStartedEvent>.Clear();
            EventBus<SceneTransitionFadeInCompletedEvent>.Clear();
            EventBus<SceneTransitionScenesReadyEvent>.Clear();
            EventBus<SceneTransitionBeforeFadeOutEvent>.Clear();
            EventBus<SceneTransitionCompletedEvent>.Clear();

            // WorldLifecycle (NewScripts): reset completion gate depende deste evento.
            EventBus<WorldLifecycleResetCompletedEvent>.Clear();

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[EventBus] EventBus inicializado (GameLoop + SceneFlow + WorldLifecycle).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterRuntimePolicyServices()
        {
            // RuntimeModeConfig (opcional) via Resources.
            // Contrato: ausência de config não deve quebrar o jogo.
            var config = RuntimeModeConfigLoader.LoadOrNull();

            var provider = DependencyManager.Provider;

            // (Opcional) expõe a config no DI global para inspeção/QA.
            // Importante: não registrar nulo.
            if (config != null)
            {
                if (!provider.TryGetGlobal<RuntimeModeConfig>(out var existingConfig) || existingConfig == null)
                {
                    provider.RegisterGlobal(config, allowOverride: false);

                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        $"[RuntimePolicy] RuntimeModeConfig carregado (asset='{config.name}').",
                        DebugUtility.Colors.Info);
                }
            }

            // Provider configurável: usa o config se existir, senão cai no comportamento atual (UnityRuntimeModeProvider).
            RegisterIfMissing<IRuntimeModeProvider>(() =>
                new ConfigurableRuntimeModeProvider(new UnityRuntimeModeProvider(), config));

            provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeModeProvider);
            if (runtimeModeProvider == null)
            {
                runtimeModeProvider = new UnityRuntimeModeProvider();
            }

            // Reporter configurável (dedupe/summary/limites via config, se existir).
            RegisterIfMissing<IDegradedModeReporter>(() =>
                new DegradedModeReporter(runtimeModeProvider, config));

            provider.TryGetGlobal<IDegradedModeReporter>(out var degradedReporter);

            RegisterIfMissing<IWorldResetPolicy>(() =>
                new ProductionWorldResetPolicy(runtimeModeProvider, degradedReporter));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[RuntimePolicy] IRuntimeModeProvider + IDegradedModeReporter + IWorldResetPolicy registrados no DI global.",
                DebugUtility.Colors.Info);
        }


// --------------------------------------------------------------------
        // Fade / Loading
        // --------------------------------------------------------------------

        private static void RegisterSceneFlowFadeModule()
        {
            // Registra o serviço de fade NewScripts no DI global.
            RegisterIfMissing<IFadeService>(() => new FadeService());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Fade] IFadeService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowLoadingIfAvailable()
        {
            // ADR-0010: LoadingHudService depende da policy Strict/Release + reporter de degraded.
            // Mantemos best-effort: se por algum motivo os serviços não estiverem disponíveis,
            // ainda assim injetamos nulls e deixamos o próprio serviço decidir como degradar.
            DependencyManager.Provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeMode);
            DependencyManager.Provider.TryGetGlobal<IDegradedModeReporter>(out var degradedReporter);

            RegisterIfMissing<ILoadingHudService>(() => new LoadingHudService(runtimeMode, degradedReporter));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Loading] ILoadingHudService registrado no DI global.",
                DebugUtility.Colors.Info);

            if (DependencyManager.Provider.TryGetGlobal<LoadingHudOrchestrator>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Loading] LoadingHudOrchestrator já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterIfMissing(() => new LoadingHudOrchestrator());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Loading] LoadingHudOrchestrator registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
        // Pause / Readiness gate
        // --------------------------------------------------------------------

        private static void RegisterPauseBridge(ISimulationGateService gateService)
        {
            if (DependencyManager.Provider.TryGetGlobal<GamePauseGateBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Pause] GamePauseGateBridge já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (gateService == null)
            {
                if (!DependencyManager.Provider.TryGetGlobal(out gateService) || gateService == null)
                {
                    DebugUtility.LogError(typeof(GlobalCompositionRoot),
                        "[Pause] ISimulationGateService indisponível; GamePauseGateBridge não pôde ser inicializado.");
                    return;
                }
            }

            var bridge = new GamePauseGateBridge(gateService);
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Pause] GamePauseGateBridge registrado (EventBus → SimulationGate).",
                DebugUtility.Colors.Info);
        }

        private static void InitializeReadinessGate(ISimulationGateService gateService)
        {
            if (gateService == null)
            {
                // fallback: tenta resolver aqui (best-effort)
                if (!DependencyManager.Provider.TryGetGlobal(out gateService) || gateService == null)
                {
                    DebugUtility.LogError(typeof(GlobalCompositionRoot),
                        "[Readiness] ISimulationGateService indisponível. Scene Flow readiness ficará sem proteção de gate.");
                    return;
                }
            }

            if (DependencyManager.Provider.TryGetGlobal<GameReadinessService>(out var registered) && registered != null)
            {
                _gameReadinessService = registered;
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Readiness] GameReadinessService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            _gameReadinessService = new GameReadinessService(gateService);
            DependencyManager.Provider.RegisterGlobal(_gameReadinessService);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Readiness] GameReadinessService inicializado e registrado no DI global (Scene Flow → SimulationGate).",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
        // GameLoop / GameRun
        // --------------------------------------------------------------------

        private static void RegisterGameLoop()
        {
            GameLoopBootstrap.Ensure(includeGameRunServices: false, includeOutcomeEventInputBridge: false);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameLoopBootstrap.EnsureRegistered() executado (serviço + bridge no escopo global).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunEndRequestService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] IGameRunEndRequestService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterIfMissing<IGameRunEndRequestService>(() => new GameRunEndRequestService());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunEndRequestService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameCommands()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameCommands>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameCommands] IGameCommands já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var runEndRequestService);

            RegisterIfMissing<IGameCommands>(() => new GameCommands(runEndRequestService));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameCommands] GameCommands registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunStatusService(IGameLoopService gameLoopService)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunStateService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] IGameRunStateService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            // Mantém compatibilidade com a assinatura atual do GameRunStateService (injeção por construtor).
            RegisterIfMissing<IGameRunStateService>(() => new GameRunStateService(gameLoopService));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunStateService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeService(IGameLoopService gameLoopService)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] IGameRunOutcomeService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            // Mantém compatibilidade com a assinatura atual do GameRunOutcomeService (injeção por construtor).
            RegisterIfMissing<IGameRunOutcomeService>(() => new GameRunOutcomeService(gameLoopService));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunOutcomeService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameRunOutcomeEventInputBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameRunOutcomeCommandBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoop] GameRunOutcomeCommandBridge já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcomeService) || outcomeService == null)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    "[GameLoop] Não foi possível registrar GameRunOutcomeCommandBridge: IGameRunOutcomeService não disponível.");
                return;
            }

            RegisterIfMissing(() => new GameRunOutcomeCommandBridge(outcomeService));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[GameLoop] GameRunOutcomeCommandBridge registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPostPlayOwnershipService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPostGameOwnershipService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[PostPlay] IPostGameOwnershipService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IPostGameOwnershipService>(
                new PostGameOwnershipService());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[PostPlay] PostGameOwnershipService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStageCoordinator()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[IntroStageController] IIntroStageCoordinator já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IIntroStageCoordinator>(
                new IntroStageCoordinator());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[IntroStageController] IntroStageCoordinator registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStageControlService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[IntroStageController] IIntroStageControlService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IIntroStageControlService>(
                new IntroStageControlService());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[IntroStageController] IntroStageControlService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStagePolicyResolver()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStagePolicyResolver>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[IntroStageController] IIntroStagePolicyResolver já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var classifier = DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var resolved) && resolved != null
                ? resolved
                : new DefaultGameplaySceneClassifier();

            DependencyManager.Provider.RegisterGlobal<IIntroStagePolicyResolver>(
                new DefaultIntroStagePolicyResolver(classifier));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[IntroStageController] DefaultIntroStagePolicyResolver registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplaySceneClassifier()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Gameplay] IGameplaySceneClassifier já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IGameplaySceneClassifier>(
                new DefaultGameplaySceneClassifier());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Gameplay] IGameplaySceneClassifier registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterDefaultIntroStageStep()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageStep>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[IntroStageController] IIntroStageStep já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IIntroStageStep>(
                new ConfirmToStartIntroStageStep());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[IntroStageController] ConfirmToStartIntroStageStep registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
        // SceneFlow / WorldLifecycle
        // --------------------------------------------------------------------

        private static void RegisterSceneFlowNative()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] SceneTransitionService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            // Loader/Fade (NewScripts standalone)
            var loaderAdapter = SceneFlowAdapterFactory.CreateLoaderAdapter();
            var fadeAdapter = SceneFlowAdapterFactory.CreateFadeAdapter(DependencyManager.Provider);

            // Gate para segurar FadeOut/Completed até WorldLifecycle reset concluir.
            ISceneTransitionCompletionGate completionGate = null;
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionCompletionGate>(out var existingGate) && existingGate != null)
            {
                completionGate = existingGate;
            }

            if (completionGate is not WorldLifecycleResetCompletionGate)
            {
                if (completionGate != null)
                {
                    DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                        $"[SceneFlow] ISceneTransitionCompletionGate não é WorldLifecycleResetCompletionGate (tipo='{completionGate.GetType().Name}'). Substituindo para cumprir o contrato SceneFlow/WorldLifecycle (completion gate).");
                }

                completionGate = new WorldLifecycleResetCompletionGate(timeoutMs: 20000);
                DependencyManager.Provider.RegisterGlobal(completionGate);

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] ISceneTransitionCompletionGate registrado (WorldLifecycleResetCompletionGate).",
                    DebugUtility.Colors.Info);
            }

            var service = new SceneTransitionService(loaderAdapter, fadeAdapter, completionGate);
            DependencyManager.Provider.RegisterGlobal<ISceneTransitionService>(service);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[SceneFlow] SceneTransitionService nativo registrado (Loader={loaderAdapter.GetType().Name}, FadeAdapter={fadeAdapter.GetType().Name}, Gate={completionGate.GetType().Name}).",
                DebugUtility.Colors.Info);
        }

        private static void RegisterSceneFlowSignatureCache()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowSignatureCache>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[SceneFlow] ISceneFlowSignatureCache já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<ISceneFlowSignatureCache>(
                new SceneFlowSignatureCache());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[SceneFlow] SceneFlowSignatureCache registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
        // Navigation / InputMode
        // --------------------------------------------------------------------

        private static void RegisterGameNavigationService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Navigation] IGameNavigationService já registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    "[Navigation] ISceneTransitionService indisponível. IGameNavigationService não será registrado.");
                return;
            }

            var service = new GameNavigationService(sceneFlow);
            DependencyManager.Provider.RegisterGlobal<IGameNavigationService>(service);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Navigation] GameNavigationService registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterExitToMenuNavigationBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<ExitToMenuNavigationBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Navigation] ExitToMenuNavigationBridge ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bridge = new ExitToMenuNavigationBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Navigation] ExitToMenuNavigationBridge registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterRestartNavigationBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<RestartNavigationBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[Navigation] RestartNavigationBridge ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bridge = new RestartNavigationBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[Navigation] RestartNavigationBridge registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterInputModeSceneFlowBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<SceneFlowInputModeBridge>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[InputMode] SceneFlowInputModeBridge ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bridge = new SceneFlowInputModeBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[InputMode] SceneFlowInputModeBridge registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIntroStageQaInstaller()
        {
            try
            {
                IntroStageDevInstaller.EnsureInstalled();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[QA][IntroStageController] Falha ao instalar IntroStageDevContextMenu no bootstrap. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static void RegisterContentSwapQaInstaller()
        {
            try
            {
                ContentSwapDevInstaller.EnsureInstalled();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[QA][ContentSwap] Falha ao instalar ContentSwapDevContextMenu no bootstrap. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static void RegisterSceneFlowQaInstaller()
        {
            try
            {
                SceneFlowDevInstaller.EnsureInstalled();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[QA][SceneFlow] Falha ao instalar SceneFlowDevContextMenu no bootstrap. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void RegisterIntroStageRuntimeDebugGui()
        {
            IntroStageRuntimeDebugGui.EnsureInstalled();
        }
#endif

        // --------------------------------------------------------------------
        // StateDependent / Camera
        // --------------------------------------------------------------------

        private static void RegisterStateDependentService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IStateDependentService>(out var existing) && existing != null)
            {
                if (existing is StateDependentService)
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        "[StateDependent] StateDependentService já registrado no DI global.",
                        DebugUtility.Colors.Info);
                    return;
                }

                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[StateDependent] Serviço registrado ({existing.GetType().Name}) não usa gate; substituindo por StateDependentService.");

                DependencyManager.Provider.RegisterGlobal<IStateDependentService>(
                    new StateDependentService());

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[StateDependent] Registrado StateDependentService (gate-aware) como IStateDependentService.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterIfMissing<IStateDependentService>(() => new StateDependentService());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[StateDependent] Registrado StateDependentService (gate-aware) como IStateDependentService.",
                DebugUtility.Colors.Info);
        }

        // --------------------------------------------------------------------
        // Coordinator (production start)
        // --------------------------------------------------------------------

        private static void RegisterGameLoopSceneFlowCoordinatorIfAvailable()
        {
            if (_sceneFlowCoordinator != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoopSceneFlow] Coordinator já está registrado (static reference).",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
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

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
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
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
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
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot), $"Global service already present: {typeof(T).Name}.");
                return;
            }

            var instance = factory();
            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot), $"Registered global service: {typeof(T).Name}.");
        }

        private static void RegisterContentSwapChangeService()
        {
            var provider = DependencyManager.Provider;

            if (provider.TryGetGlobal<IContentSwapChangeService>(out var existing) && existing != null)
            {
                return;
            }

            if (!provider.TryGetGlobal<IContentSwapContextService>(out var contextService) || contextService == null)
            {
                throw new InvalidOperationException(
                    "IContentSwapContextService is not registered. Ensure GlobalCompositionRoot registered it before ContentSwapChangeService.");
            }

            provider.RegisterGlobal<IContentSwapChangeService>(new InPlaceContentSwapService(contextService));
            DebugUtility.Log(typeof(GlobalCompositionRoot),
                "[ContentSwap] Registered IContentSwapChangeService (InPlaceOnly).",
                DebugUtility.Colors.Success);
        }

        private static void RegisterLevelServices()
        {
            var provider = DependencyManager.Provider;

            if (provider.TryGetGlobal<ILevelManager>(out var existing) && existing != null)
            {
                return;
            }

            LevelManagerInstaller.EnsureRegistered(fromBootstrap: true);
        }

        private static void RegisterLevelQaInstaller()
        {
            try
            {
                LevelDevInstaller.EnsureInstalled();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[QA][Level] Falha ao instalar LevelDevContextMenu no bootstrap. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

    }
}



