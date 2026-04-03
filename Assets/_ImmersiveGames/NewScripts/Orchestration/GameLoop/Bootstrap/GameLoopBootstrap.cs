using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate.Interop;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Audio.Bridges;
using _ImmersiveGames.NewScripts.Experience.Audio.Runtime.Core;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.Bridges;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.Commands;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunOutcome;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Fade.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.Bootstrap
{
    /// <summary>
    /// Runtime composer do GameLoop.
    ///
    /// Responsabilidade:
    /// - ativar o GameLoop depois que os installers relevantes concluíram;
    /// - compor bridges, driver e sync runtime do módulo;
    /// - não registrar contratos de boot.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameLoopBootstrap : MonoBehaviour
    {
        private const string DriverObjectName = "[NewScripts] GameLoopInputDriver";
        private const string RunEndBridgeObjectName = "[NewScripts] GameRunEndedEventBridge";

        private static bool _runtimeComposed;
        private static GameLoopSceneFlowSyncCoordinator _sceneFlowSyncCoordinator;

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(GameLoopBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] BootstrapConfigAsset obrigatorio ausente para compor o runtime.");
            }

            var gameLoopService = ResolveRequiredGameLoopService();
            gameLoopService.Initialize();

            EnsureAudioPauseDuckingBridge();
            EnsureIntroStageBridge();
            EnsurePauseBridge();
            EnsureGameRunRuntimeServices();
            EnsureOutcomeEventInputBridge();
            EnsureRunEndEventBridge();
            EnsureDriver();
            EnsureSceneFlowSyncCoordinator(bootstrapConfig, gameLoopService);
            EnsureGameLoopModuleComposition();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(GameLoopBootstrap),
                "[OBS][GameLoop][Operational] Runtime composition concluida.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime()
        {
            if (!DependencyManager.Provider.TryGetGlobal<BootstrapConfigAsset>(out var bootstrapConfig) || bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] BootstrapConfigAsset ausente no DI global para compor o runtime.");
            }

            ComposeRuntime(bootstrapConfig);
        }

        private static IGameLoopService ResolveRequiredGameLoopService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var service) && service != null)
            {
                return service;
            }

            throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameLoopService ausente no DI global antes da composicao runtime.");
        }

        private static void EnsureIntroStageBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameLoopIntroStageBridge>(out _))
            {
                return;
            }

            var bridge = new GameLoopIntroStageBridge();
            DependencyManager.Provider.RegisterGlobal(bridge);
        }

        private static void EnsureAudioPauseDuckingBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<AudioPauseDuckingBridge>(out _))
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IAudioBgmService>(out var bgmService) || bgmService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IAudioBgmService ausente no DI global antes de registrar o AudioPauseDuckingBridge.");
            }

            var bridge = AudioPauseDuckingBridge.EnsureCreated(bgmService);
            DependencyManager.Provider.RegisterGlobal(bridge);

            DebugUtility.LogVerbose(typeof(GameLoopBootstrap),
                "[Audio][BOOT] AudioPauseDuckingBridge composed after GameLoopService became available.",
                DebugUtility.Colors.Info);
        }

        private static void EnsurePauseBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GamePauseGateBridge>(out _))
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService) || gateService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] ISimulationGateService ausente no DI global antes de registrar o GamePauseGateBridge.");
            }

            var bridge = new GamePauseGateBridge(gateService);
            DependencyManager.Provider.RegisterGlobal(bridge);
        }

        private static void EnsureGameRunRuntimeServices()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var endRequest) || endRequest == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameRunEndRequestService ausente no DI global antes da composicao runtime.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunPlayingStateGuard>(out var gameplayStateGuard) || gameplayStateGuard == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameRunPlayingStateGuard ausente no DI global antes da composicao runtime.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcome) || outcome == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameRunOutcomeService ausente no DI global antes da composicao runtime.");
            }
        }

        private static void EnsureOutcomeEventInputBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameRunOutcomeRequestBridge>(out _))
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var outcomeService) || outcomeService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameRunOutcomeService ausente no DI global antes de registrar o GameRunOutcomeRequestBridge.");
            }

            var bridge = new GameRunOutcomeRequestBridge(outcomeService);
            DependencyManager.Provider.RegisterGlobal(bridge);
        }

        private static void EnsureRunEndEventBridge()
        {
            if (FindFirstObjectByType<GameRunEndedEventBridge>() != null)
            {
                return;
            }

            var go = new GameObject(RunEndBridgeObjectName);
            go.AddComponent<GameRunEndedEventBridge>();
            DontDestroyOnLoad(go);
        }

        private static void EnsureDriver()
        {
            if (FindFirstObjectByType<GameLoopInputDriver>() != null)
            {
                return;
            }

            var go = new GameObject(DriverObjectName);
            go.AddComponent<GameLoopInputDriver>();
            DontDestroyOnLoad(go);
        }

        private static void EnsureSceneFlowSyncCoordinator(BootstrapConfigAsset bootstrapConfig, IGameLoopService gameLoopService)
        {
            if (_sceneFlowSyncCoordinator != null)
            {
                return;
            }

            // Comentário: este coordinator pertence ao GameLoopBootstrap porque conecta
            // o owner de SceneFlow, já materializado nesta fase, com o GameLoopService
            // que nasce aqui. Antes disso, um dos dois owners ainda não existe.
            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] ISceneTransitionService ausente no DI global antes de compor o SceneFlow sync.");
            }

            var bootStartRoute = ResolveBootStartRouteOrFailFast(bootstrapConfig);
            StartupTransitionResolution startup = ResolveRequiredStartupTransition(bootstrapConfig);
            IFadeService fadeService = startup.UseFade ? ResolveRequiredFadeService() : null;

            var startPlan = new SceneTransitionRequest(
                bootStartRoute.ToDefinition(),
                routeId: bootStartRoute.RouteId,
                transitionStyle: startup.StyleRef,
                payload: SceneTransitionPayload.Empty,
                transitionProfile: startup.Profile,
                useFade: startup.UseFade,
                requestedBy: "Boot/StartPlan",
                reason: "Boot/StartPlan",
                resolvedRouteRef: bootStartRoute);

            _sceneFlowSyncCoordinator = new GameLoopSceneFlowSyncCoordinator(sceneFlow, gameLoopService, fadeService, startPlan);

            DebugUtility.LogVerbose(typeof(GameLoopBootstrap),
                $"[GameLoopSceneFlow][Operational] Coordinator composto para boot/start-plan compatibility (routeId='{bootStartRoute.RouteId}', routeRef='{bootStartRoute.name}', style='{startup.StyleLabel}', profile='{startup.ProfileLabel}', profileAsset='{startup.Profile.name}').",
                DebugUtility.Colors.Info);
        }

        private static void EnsureGameLoopModuleComposition()
        {
            RequireGlobal<IGameLoopService>("IGameLoopService");
            RequireGlobal<IPauseStateService>("IPauseStateService");
            RequireGlobal<IGameRunEndRequestService>("IGameRunEndRequestService");
            RequireGlobal<IGameRunPlayingStateGuard>("IGameRunPlayingStateGuard");
            RequireGlobal<IGameRunOutcomeService>("IGameRunOutcomeService");
            RequireGlobal<IGameLoopCommands>("IGameLoopCommands");
            RequireGlobal<IPauseCommands>("IPauseCommands");
            RequireGlobal<GameLoopIntroStageBridge>("GameLoopIntroStageBridge");
            RequireGlobal<GameRunOutcomeRequestBridge>("GameRunOutcomeRequestBridge");

            if (_sceneFlowSyncCoordinator == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] GameLoopSceneFlowSyncCoordinator nao foi composto.");
            }

            DebugUtility.Log(typeof(GameLoopBootstrap),
                "[OBS][GameLoop][Operational] Runtime composition consolidada. scope='flow/run/pause executor + bridges + sync'.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterIfMissing<T>(Func<T> factory, Type contextType, string alreadyRegisteredMessage, string registeredMessage)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(contextType, alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(contextType, registeredMessage, DebugUtility.Colors.Info);
        }

        private static SceneRouteDefinitionAsset ResolveBootStartRouteOrFailFast(BootstrapConfigAsset bootstrap)
        {
            var navigationCatalog = bootstrap.NavigationCatalog;
            if (navigationCatalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] Boot/StartPlan exige NavigationCatalog no bootstrap.");
            }

            GameNavigationEntry menuEntry = navigationCatalog.ResolveCoreOrFail(GameNavigationIntentKind.Menu);
            if (menuEntry.RouteRef == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] Boot/StartPlan routeRef ausente no intent core Menu.");
            }

            if (!menuEntry.RouteId.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] Boot/StartPlan routeId invalido/vazio.");
            }

            if (menuEntry.RouteRef.RouteId != menuEntry.RouteId)
            {
                throw new InvalidOperationException($"[FATAL][Config][GameLoop] Boot/StartPlan routeId inconsistente com routeRef. routeId='{menuEntry.RouteId}' routeRefRouteId='{menuEntry.RouteRef.RouteId}'.");
            }

            return menuEntry.RouteRef;
        }

        private static StartupTransitionResolution ResolveRequiredStartupTransition(BootstrapConfigAsset bootstrap)
        {
            TransitionStyleAsset styleRef = bootstrap.StartupTransitionStyleRef;
            if (styleRef == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] Startup transition ausente. Configure startupTransitionStyleRef obrigatorio no bootstrap.");
            }

            TransitionStyleDefinition definition = styleRef.ToDefinitionOrFail(nameof(GameLoopBootstrap), "Boot/StartPlan");
            return new StartupTransitionResolution(styleRef, definition.Profile, definition.UseFade);
        }

        private static IFadeService ResolveRequiredFadeService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IFadeService>(out var fadeService) && fadeService != null)
            {
                return fadeService;
            }

            throw new InvalidOperationException("[FATAL][Config][GameLoop] IFadeService ausente no DI global antes da composicao do SceneFlow sync.");
        }

        private static void RequireGlobal<T>(string serviceName)
            where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                return;
            }

            throw new InvalidOperationException($"[FATAL][Config][GameLoop] {serviceName} obrigatorio ausente para compor o GameLoop runtime.");
        }

        private readonly struct StartupTransitionResolution
        {
            public StartupTransitionResolution(TransitionStyleAsset styleRef, SceneTransitionProfile profile, bool useFade)
            {
                StyleRef = styleRef;
                Profile = profile;
                UseFade = useFade;
            }

            public TransitionStyleAsset StyleRef { get; }
            public SceneTransitionProfile Profile { get; }
            public bool UseFade { get; }
            public string StyleLabel => StyleRef != null ? StyleRef.StyleLabel : string.Empty;
            public string ProfileLabel => Profile != null ? Profile.name : string.Empty;
        }

    }
}
