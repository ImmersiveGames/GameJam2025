using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Flow;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Input;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Run;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Bootstrap
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

            EnsureInputCommandBridge();
            EnsureGameRunRuntimeServices();
            EnsureOutcomeEventInputBridge();
            EnsureRunEndEventBridge();
            EnsureDriver();
            EnsureSceneFlowSyncCoordinator(bootstrapConfig);

            _runtimeComposed = true;

            DebugUtility.LogVerbose(typeof(GameLoopBootstrap),
                "[GameLoop] Runtime composition concluida.",
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

        private static void EnsureInputCommandBridge()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameLoopInputCommandBridge>(out _))
            {
                return;
            }

            var bridge = new GameLoopInputCommandBridge();
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

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunResultSnapshotService>(out var status) || status == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameRunResultSnapshotService ausente no DI global antes da composicao runtime.");
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

        private static void EnsureSceneFlowSyncCoordinator(BootstrapConfigAsset bootstrapConfig)
        {
            if (_sceneFlowSyncCoordinator != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] ISceneTransitionService ausente no DI global antes de compor o SceneFlow sync.");
            }

            SceneRouteId bootStartRouteId = ResolveBootStartRouteIdOrFailFast(bootstrapConfig);
            StartupTransitionResolution startup = ResolveRequiredStartupTransition(bootstrapConfig);

            var startPlan = new SceneTransitionRequest(
                routeId: bootStartRouteId,
                transitionStyle: startup.StyleRef,
                payload: SceneTransitionPayload.Empty,
                transitionProfile: startup.Profile,
                useFade: startup.UseFade,
                requestedBy: "Boot/StartPlan",
                reason: "Boot/StartPlan");

            _sceneFlowSyncCoordinator = new GameLoopSceneFlowSyncCoordinator(sceneFlow, startPlan);

            DebugUtility.LogVerbose(typeof(GameLoopBootstrap),
                $"[GameLoopSceneFlow] Coordinator composto (startPlan production, routeId='{bootStartRouteId}', style='{startup.StyleLabel}', profile='{startup.ProfileLabel}', profileAsset='{startup.Profile.name}').",
                DebugUtility.Colors.Info);
        }

        private static SceneRouteId ResolveBootStartRouteIdOrFailFast(BootstrapConfigAsset bootstrap)
        {
            var navigationCatalog = bootstrap.NavigationCatalog;
            if (navigationCatalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] Boot/StartPlan exige NavigationCatalog no bootstrap.");
            }

            GameNavigationEntry menuEntry = navigationCatalog.ResolveCoreOrFail(GameNavigationIntentKind.Menu);
            SceneRouteId bootStartRouteId = menuEntry.RouteId;
            if (!bootStartRouteId.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] Boot/StartPlan routeId invalido/vazio.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneRouteResolver>(out var routeResolver) || routeResolver == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][GameLoop] Boot/StartPlan sem ISceneRouteResolver para routeId='{bootStartRouteId}'.");
            }

            if (!routeResolver.TryResolve(bootStartRouteId, out _))
            {
                throw new InvalidOperationException($"[FATAL][Config][GameLoop] Boot/StartPlan routeId nao encontrado no catalogo de rotas. routeId='{bootStartRouteId}'.");
            }

            return bootStartRouteId;
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

        private void Awake()
        {
            ComposeRuntime();
            DontDestroyOnLoad(gameObject);
        }
    }
}
