using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    /// <summary>
    /// Política padrão de reset: prioriza metadado por rota e mantém fallback por profile.
    /// </summary>
    public sealed class SceneRouteResetPolicy : IRouteResetPolicy
    {
        private readonly ISceneRouteResolver _routeResolver;

        public SceneRouteResetPolicy(ISceneRouteResolver routeResolver = null)
        {
            _routeResolver = routeResolver;
        }

        public RouteResetDecision Resolve(SceneRouteId routeId, SceneTransitionContext context)
        {
            if (TryResolveRouteKindDecision(routeId, out var routeDecision))
            {
                return routeDecision;
            }

            bool shouldResetFromProfile = context.TransitionProfileId.IsGameplay;
            return new RouteResetDecision(
                shouldResetFromProfile,
                decisionSource: "profile:fallback",
                reason: shouldResetFromProfile
                    ? WorldResetReasons.SceneFlowScenesReady
                    : $"{WorldResetReasons.SkippedStartupOrFrontendPrefix}:profile={context.TransitionProfileName};route={routeId.Value}");
        }

        private bool TryResolveRouteKindDecision(SceneRouteId routeId, out RouteResetDecision decision)
        {
            decision = default;

            if (!routeId.IsValid)
            {
                return false;
            }

            if (_routeResolver == null)
            {
                return false;
            }

            if (!_routeResolver.TryResolve(routeId, out var routeDefinition))
            {
                return false;
            }

            if (routeDefinition.RouteKind == SceneRouteKind.Unspecified)
            {
                return false;
            }

            bool shouldReset = routeDefinition.RouteKind == SceneRouteKind.Gameplay;
            string reason = shouldReset
                ? WorldResetReasons.SceneFlowScenesReady
                : $"{WorldResetReasons.SkippedStartupOrFrontendPrefix}:routeKind={routeDefinition.RouteKind};route={routeId.Value}";

            decision = new RouteResetDecision(
                shouldReset,
                decisionSource: $"routeKind:{routeDefinition.RouteKind}",
                reason: reason);

            return true;
        }
    }
}
