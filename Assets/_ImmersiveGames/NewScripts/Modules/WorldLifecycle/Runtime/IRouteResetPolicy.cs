using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    /// <summary>
    /// Resolve de política de reset para transições do SceneFlow.
    /// </summary>
    public interface IRouteResetPolicy
    {
        RouteResetDecision Resolve(SceneRouteId routeId, SceneTransitionContext context);
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
