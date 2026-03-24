using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Flow;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
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

        private static void RegisterGameLoopSceneFlowCoordinatorIfAvailable()
        {
            if (_sceneFlowSyncCoordinator != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoopSceneFlow] Coordinator ja esta registrado (static reference).",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[GameLoopSceneFlow] ISceneTransitionService indisponivel. Coordinator nao sera registrado.",
                    DebugUtility.Colors.Info);
                return;
            }

            var bootstrap = GetRequiredBootstrapConfig(out _);
            SceneRouteId bootStartRouteId = ResolveBootStartRouteIdOrFailFast(bootstrap);
            StartupTransitionResolution startup = ResolveRequiredStartupTransition(bootstrap);

            var startPlan = new SceneTransitionRequest(
                routeId: bootStartRouteId,
                transitionStyle: startup.StyleRef,
                payload: SceneTransitionPayload.Empty,
                transitionProfile: startup.Profile,
                useFade: startup.UseFade,
                requestedBy: "Boot/StartPlan",
                reason: "Boot/StartPlan");

            _sceneFlowSyncCoordinator = new GameLoopSceneFlowSyncCoordinator(sceneFlow, startPlan);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[GameLoopSceneFlow] Coordinator registrado (startPlan production, routeId='{bootStartRouteId}', style='{startup.StyleLabel}', profile='{startup.ProfileLabel}', profileAsset='{startup.Profile.name}').",
                DebugUtility.Colors.Info);
        }

        private static SceneRouteId ResolveBootStartRouteIdOrFailFast(Config.BootstrapConfigAsset bootstrap)
        {
            var navigationCatalog = bootstrap.NavigationCatalog;
            if (navigationCatalog == null)
            {
                FailFast("[FATAL][Config] Boot/StartPlan exige NavigationCatalog no bootstrap.");
            }

            GameNavigationEntry menuEntry = navigationCatalog.ResolveCoreOrFail(GameNavigationIntentKind.Menu);
            SceneRouteId bootStartRouteId = menuEntry.RouteId;
            if (!bootStartRouteId.IsValid)
            {
                FailFast("[FATAL][Config] Boot/StartPlan routeId invalido/vazio.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneRouteResolver>(out var routeResolver) || routeResolver == null)
            {
                FailFast($"[FATAL][Config] Boot/StartPlan sem ISceneRouteResolver para routeId='{bootStartRouteId}'.");
            }

            if (routeResolver != null && !routeResolver.TryResolve(bootStartRouteId, out _))
            {
                FailFast($"[FATAL][Config] Boot/StartPlan routeId nao encontrado no catalogo de rotas. routeId='{bootStartRouteId}'.");
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Navigation] BootStartRouteResolvedVia intent -> entry -> routeRef. intentKind='{GameNavigationIntentKind.Menu}', routeId='{bootStartRouteId}'.",
                DebugUtility.Colors.Info);

            return bootStartRouteId;
        }

        private static StartupTransitionResolution ResolveRequiredStartupTransition(Config.BootstrapConfigAsset bootstrap)
        {
            TransitionStyleAsset styleRef = bootstrap.StartupTransitionStyleRef;
            if (styleRef == null)
            {
                FailFast("[FATAL][Config] Startup transition ausente. Configure startupTransitionStyleRef obrigatorio no bootstrap.");
            }

            TransitionStyleDefinition definition = styleRef.ToDefinitionOrFail(nameof(GlobalCompositionRoot), "Boot/StartPlan");

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][SceneFlow] Startup transition resolved via bootstrap direct style reference. styleAsset='{styleRef.name}', style='{definition.StyleLabel}', profile='{definition.ProfileLabel}', profileAsset='{definition.Profile.name}'.",
                DebugUtility.Colors.Info);

            return new StartupTransitionResolution(styleRef, definition.Profile, definition.UseFade);
        }
    }
}

