using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
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

            var bootstrap = GetRequiredBootstrapConfig(out _);
            SceneRouteId bootStartRouteId = ResolveBootStartRouteIdOrFailFast(bootstrap);
            SceneTransitionProfile profile = ResolveRequiredStartupProfile(bootstrap);

            var startPlan = new SceneTransitionRequest(
                routeId: bootStartRouteId,
                styleId: TransitionStyleId.None,
                payload: SceneTransitionPayload.Empty,
                transitionProfile: profile,
                transitionProfileId: StartProfileId,
                useFade: true,
                requestedBy: "Boot/StartPlan",
                reason: "Boot/StartPlan");

            _sceneFlowCoordinator = new GameLoopSceneFlowCoordinator(sceneFlow, startPlan);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[GameLoopSceneFlow] Coordinator registrado (startPlan production, routeId='{bootStartRouteId}', profile='{StartProfileId}', profileAsset='{profile.name}').",
                DebugUtility.Colors.Info);
        }

        private static SceneRouteId ResolveBootStartRouteIdOrFailFast(Infrastructure.Config.NewScriptsBootstrapConfigAsset bootstrap)
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
                FailFast("[FATAL][Config] Boot/StartPlan routeId inválido/vazio.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneRouteResolver>(out var routeResolver) || routeResolver == null)
            {
                FailFast($"[FATAL][Config] Boot/StartPlan sem ISceneRouteResolver para routeId='{bootStartRouteId}'.");
            }

            if (!routeResolver.TryResolve(bootStartRouteId, out _))
            {
                FailFast($"[FATAL][Config] Boot/StartPlan routeId não encontrado no catálogo de rotas. routeId='{bootStartRouteId}'.");
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Navigation] BootStartRouteResolvedVia intent -> entry -> routeRef. intentKind='{GameNavigationIntentKind.Menu}', routeId='{bootStartRouteId}'.",
                DebugUtility.Colors.Info);

            return bootStartRouteId;
        }

        private static SceneTransitionProfile ResolveRequiredStartupProfile(Infrastructure.Config.NewScriptsBootstrapConfigAsset bootstrap)
        {
            var transitionCatalog = bootstrap.TransitionProfileCatalog;
            if (transitionCatalog == null)
            {
                FailFast("[FATAL][Config] Missing required NewScriptsBootstrapConfigAsset.transitionProfileCatalog for startup transition plan.");
            }

            if (!transitionCatalog.TryGetProfile(StartProfileId, out var profile) || profile == null)
            {
                FailFast($"[FATAL][Config] Startup profile missing in transitionProfileCatalog. profileId='{StartProfileId}'.");
            }

            return profile;
        }
    }
}
