using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.LoadingFade.Fade.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Bindings;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Commands;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.InputModes;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset.Installers;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Ownership;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.RunResultStage.GameLoopRunOutcome;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Installers
{
    /// <summary>
    /// Runtime composer do GameLoop.
    ///
    /// Responsabilidade:
    /// - ativar o GameLoop depois que os installers relevantes concluÃ­ram;
    /// - delegar composicao de Run/Intro/StartupRoute para composers nomeados;
    /// - nao registrar contratos de boot.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameLoopBootstrap : MonoBehaviour
    {
        private const string DriverObjectName = "[NewScripts] GameLoopInputDriver";

        private static bool _runtimeComposed;
        private static SessionOperationalStartupRouteAdapter _startupRouteAdapter;

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

            GameLoopCoreInstaller.Install();
            var gameLoopService = ResolveRequiredGameLoopService();
            gameLoopService.Initialize();

            LegacyPauseCompatibilityInstaller.Install();
            RunPipelineBridgeInstaller.Install();
            IntroStageIntegrationInstaller.Install();
            SessionOperationalStartupRouteInstaller.Install();

            EnsureDriver();
            LegacyPauseCompatibilityInstaller.ComposeRuntime();
            RunPipelineRuntimeBridgeComposer.ComposeRuntime();
            _startupRouteAdapter = SessionOperationalStartupRouteInstaller.ComposeRuntime(bootstrapConfig, gameLoopService);
            EnsureGameLoopModuleComposition();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(GameLoopBootstrap),
                "[OBS][GameLoop][Core] Runtime composition concluida. scope='core executor + legacy pause compat + delegated run pipeline runtime bridge composer + startup-route adapter'.",
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

        private static void EnsureDriver()
        {
            if (FindFirstObjectByType<GameLoopInputDriver>() != null)
            {
                return;
            }

            var go = new GameObject(DriverObjectName);
            go.AddComponent<GameLoopInputDriver>();
            DontDestroyOnLoad(go);

            DebugUtility.Log(typeof(GameLoopBootstrap),
                "[OBS][GameLoop][Core] GameLoopInputDriver composto.",
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
            RequireGlobal<GameRunOutcomeRequestBridge>("GameRunOutcomeRequestBridge");

            if (_startupRouteAdapter == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameLoop] SessionOperationalStartupRouteAdapter nao foi composto.");
            }

            DebugUtility.Log(typeof(GameLoopBootstrap),
                "[OBS][GameLoop][Core] Runtime composition consolidada. scope='core executor + compatibility + delegated run pipeline runtime bridge composer + startup-route adapter'.",
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
