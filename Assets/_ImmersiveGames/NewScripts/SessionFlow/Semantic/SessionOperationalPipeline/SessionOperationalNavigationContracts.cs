using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    public readonly struct NavigateToRouteCommand : IEvent
    {
        public NavigateToRouteCommand(SceneRouteId routeId, string source, string reason)
        {
            RouteId = routeId;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public SceneRouteId RouteId { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => RouteId.IsValid && !string.IsNullOrWhiteSpace(Source) && !string.IsNullOrWhiteSpace(Reason);
    }

    public readonly struct RouteResolvedFact : IEvent
    {
        public RouteResolvedFact(
            SceneRouteId routeId,
            string routeProfileId,
            SceneRouteKind routeKind,
            string source,
            string reason)
        {
            RouteId = routeId;
            RouteProfileId = string.IsNullOrWhiteSpace(routeProfileId) ? string.Empty : routeProfileId.Trim();
            RouteKind = routeKind;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public SceneRouteId RouteId { get; }
        public string RouteProfileId { get; }
        public SceneRouteKind RouteKind { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => RouteId.IsValid &&
                               !string.IsNullOrWhiteSpace(RouteProfileId) &&
                               RouteKind != SceneRouteKind.Unspecified &&
                               !string.IsNullOrWhiteSpace(Source);
    }

    public readonly struct RequestRouteTransitionCommand : IEvent
    {
        public RequestRouteTransitionCommand(
            SceneRouteId routeId,
            string routeProfileId,
            SceneRouteKind routeKind,
            string source,
            string reason)
        {
            RouteId = routeId;
            RouteProfileId = string.IsNullOrWhiteSpace(routeProfileId) ? string.Empty : routeProfileId.Trim();
            RouteKind = routeKind;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public SceneRouteId RouteId { get; }
        public string RouteProfileId { get; }
        public SceneRouteKind RouteKind { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => RouteId.IsValid &&
                               !string.IsNullOrWhiteSpace(RouteProfileId) &&
                               RouteKind != SceneRouteKind.Unspecified &&
                               !string.IsNullOrWhiteSpace(Source);
    }
}
