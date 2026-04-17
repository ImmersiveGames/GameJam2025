using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Navigation.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition.Runtime;
namespace ImmersiveGames.GameJam2025.Orchestration.WorldReset.Policies
{
    public interface IRouteResetPolicy
    {
        RouteResetDecision Resolve(SceneRouteId routeId, SceneRouteDefinition? routeDefinition, SceneTransitionContext context);
    }

    public readonly struct RouteResetDecision
    {
        public bool ShouldReset { get; }
        public string DecisionSource { get; }
        public string Reason { get; }

        public RouteResetDecision(bool shouldReset, string decisionSource, string reason)
        {
            ShouldReset = shouldReset;
            DecisionSource = string.IsNullOrWhiteSpace(decisionSource) ? string.Empty : decisionSource.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }
    }
}

