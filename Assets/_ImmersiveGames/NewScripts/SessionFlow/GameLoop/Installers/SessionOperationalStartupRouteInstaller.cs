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
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow;

namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Installers
{
    /// <summary>
    /// Installer temporario do startup route operacional.
    /// </summary>
    public static class SessionOperationalStartupRouteInstaller
    {
        private static bool _installed;
        private static bool _runtimeComposed;
        private static SessionOperationalStartupRouteAdapter _startupRouteAdapter;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            RegisterStartupRouteSyncDecisionService();

            _installed = true;

            DebugUtility.Log(typeof(SessionOperationalStartupRouteInstaller),
                "[OBS][SessionOperationalPipeline][StartupRoute] Installer concluido.",
                DebugUtility.Colors.Info);
        }

        public static SessionOperationalStartupRouteAdapter ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            if (_runtimeComposed)
            {
                return _startupRouteAdapter;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] BootstrapConfigAsset obrigatorio ausente para compor o startup route adapter.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] ISceneTransitionService ausente no DI global antes de compor o startup route adapter.");
            }

            var bootStartRoute = ResolveBootStartRouteOrFailFast(bootstrapConfig);
            StartupTransitionResolution startup = ResolveRequiredStartupTransition(bootstrapConfig);
            IFadeService fadeService = startup.UseFade ? ResolveRequiredFadeService() : null;
            ISessionOperationalStartupRouteDecisionService syncDecisionService = ResolveRequiredStartupRouteSyncDecisionService();

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

            _startupRouteAdapter = new SessionOperationalStartupRouteAdapter(sceneFlow, fadeService, syncDecisionService, startPlan);

            _runtimeComposed = true;

            DebugUtility.LogVerbose(typeof(SessionOperationalStartupRouteInstaller),
                $"[OBS][SessionOperationalPipeline][StartupRoute] startupRouteRequested source='runtime_composition' routeId='{bootStartRoute.RouteId}' routeRef='{bootStartRoute.name}' style='{startup.StyleLabel}' profile='{startup.ProfileLabel}' profileAsset='{startup.Profile.name}'.",
                DebugUtility.Colors.Info);

            return _startupRouteAdapter;
        }

        private static void RegisterStartupRouteSyncDecisionService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISessionOperationalStartupRouteDecisionService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(SessionOperationalStartupRouteInstaller),
                    "[OBS][SessionOperationalPipeline][StartupRoute] ISessionOperationalStartupRouteDecisionService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<ISessionOperationalStartupRouteDecisionService>(
                new SessionOperationalStartupRouteDecisionService());

            DebugUtility.LogVerbose(typeof(SessionOperationalStartupRouteInstaller),
                "[OBS][SessionOperationalPipeline][StartupRoute] Sync decision service registrado no DI global.",
                DebugUtility.Colors.Info);
        }

        private static ISessionOperationalStartupRouteDecisionService ResolveRequiredStartupRouteSyncDecisionService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISessionOperationalStartupRouteDecisionService>(out var syncDecisionService) && syncDecisionService != null)
            {
                return syncDecisionService;
            }

            throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] ISessionOperationalStartupRouteDecisionService ausente no DI global antes de compor o startup route adapter.");
        }

        private static SceneRouteDefinitionAsset ResolveBootStartRouteOrFailFast(BootstrapConfigAsset bootstrap)
        {
            var navigationCatalog = bootstrap.NavigationCatalog;
            if (navigationCatalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] Boot/StartPlan exige NavigationCatalog no bootstrap.");
            }

            GameNavigationEntry menuEntry = navigationCatalog.ResolveCoreOrFail(GameNavigationIntentKind.Menu);
            if (menuEntry.RouteRef == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] Boot/StartPlan routeRef ausente no intent core Menu.");
            }

            if (!menuEntry.RouteId.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] Boot/StartPlan routeId invalido/vazio.");
            }

            if (menuEntry.RouteRef.RouteId != menuEntry.RouteId)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline] Boot/StartPlan routeId inconsistente com routeRef. routeId='{menuEntry.RouteId}' routeRefRouteId='{menuEntry.RouteRef.RouteId}'.");
            }

            return menuEntry.RouteRef;
        }

        private static StartupTransitionResolution ResolveRequiredStartupTransition(BootstrapConfigAsset bootstrap)
        {
            TransitionStyleAsset styleRef = bootstrap.StartupTransitionStyleRef;
            if (styleRef == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] Startup transition ausente. Configure startupTransitionStyleRef obrigatorio no bootstrap.");
            }

            TransitionStyleDefinition definition = styleRef.ToDefinitionOrFail(nameof(SessionOperationalStartupRouteInstaller), "Boot/StartPlan");
            return new StartupTransitionResolution(styleRef, definition.Profile, definition.UseFade);
        }

        private static IFadeService ResolveRequiredFadeService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IFadeService>(out var fadeService) && fadeService != null)
            {
                return fadeService;
            }

            throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] IFadeService ausente no DI global antes da composicao do startup route adapter.");
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
