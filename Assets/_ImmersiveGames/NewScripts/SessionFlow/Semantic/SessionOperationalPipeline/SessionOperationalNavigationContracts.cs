using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    public readonly struct NavigateToRouteCommand : IEvent
    {
        public NavigateToRouteCommand(SceneRouteDefinitionAsset routeDefinition, string source, string reason)
        {
            RouteDefinition = routeDefinition;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public SceneRouteDefinitionAsset RouteDefinition { get; }
        public SceneRouteId RouteId => RouteDefinition != null ? RouteDefinition.RouteId : SceneRouteId.None;
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => RouteDefinition != null &&
                               RouteId.IsValid &&
                               !string.IsNullOrWhiteSpace(Source) &&
                               !string.IsNullOrWhiteSpace(Reason);
    }

    public readonly struct RouteResolvedFact : IEvent
    {
        public RouteResolvedFact(
            SessionOperationalResolvedRoute resolvedRoute,
            string source,
            string reason)
        {
            ResolvedRoute = resolvedRoute;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public SessionOperationalResolvedRoute ResolvedRoute { get; }
        public SceneRouteId RouteId => ResolvedRoute.RouteId;
        public string RouteProfileId => ResolvedRoute.RouteProfileId;
        public SceneRouteKind RouteKind => ResolvedRoute.RouteKind;
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => ResolvedRoute.IsValid &&
                               !string.IsNullOrWhiteSpace(Source);
    }

    public readonly struct RequestRouteTransitionCommand : IEvent
    {
        public RequestRouteTransitionCommand(
            SessionOperationalResolvedRoute resolvedRoute,
            string source,
            string reason)
        {
            ResolvedRoute = resolvedRoute;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public SessionOperationalResolvedRoute ResolvedRoute { get; }
        public SceneRouteId RouteId => ResolvedRoute.RouteId;
        public string RouteProfileId => ResolvedRoute.RouteProfileId;
        public SceneRouteKind RouteKind => ResolvedRoute.RouteKind;
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => ResolvedRoute.IsValid &&
                               !string.IsNullOrWhiteSpace(Source);
    }

    /// <summary>
    /// Rota semantica resolvida uma unica vez pelo SessionOperationalPipeline.
    /// Mantem identidade textual para logs e carrega referencias diretas para o executor tecnico.
    /// </summary>
    public readonly struct SessionOperationalResolvedRoute
    {
        public SessionOperationalResolvedRoute(
            SceneRouteId routeId,
            string routeProfileId,
            SceneRouteKind routeKind,
            string activeScene,
            SceneRouteDefinitionAsset routeDefinitionAsset,
            SceneRouteProfileAsset routeProfileAsset,
            TransitionStyleAsset transitionStyleAsset,
            SceneRouteDefinition routeDefinition,
            SceneRouteProfile routeProfile,
            TransitionStyleDefinition transitionStyleDefinition,
            SceneTransitionPayload payload,
            string source,
            string reason)
        {
            RouteId = routeId;
            RouteProfileId = string.IsNullOrWhiteSpace(routeProfileId) ? string.Empty : routeProfileId.Trim();
            RouteKind = routeKind;
            ActiveScene = string.IsNullOrWhiteSpace(activeScene) ? string.Empty : activeScene.Trim();
            RouteDefinitionAsset = routeDefinitionAsset;
            RouteProfileAsset = routeProfileAsset;
            TransitionStyleAsset = transitionStyleAsset;
            RouteDefinition = routeDefinition;
            RouteProfile = routeProfile;
            TransitionStyleDefinition = transitionStyleDefinition;
            Payload = payload ?? SceneTransitionPayload.Empty;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public SceneRouteId RouteId { get; }
        public string RouteProfileId { get; }
        public SceneRouteKind RouteKind { get; }
        public string ActiveScene { get; }
        public SceneRouteDefinitionAsset RouteDefinitionAsset { get; }
        public SceneRouteProfileAsset RouteProfileAsset { get; }
        public TransitionStyleAsset TransitionStyleAsset { get; }
        public SceneRouteDefinition RouteDefinition { get; }
        public SceneRouteProfile RouteProfile { get; }
        public TransitionStyleDefinition TransitionStyleDefinition { get; }
        public SceneTransitionPayload Payload { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => RouteId.IsValid &&
                               !string.IsNullOrWhiteSpace(RouteProfileId) &&
                               RouteKind != SceneRouteKind.Unspecified &&
                               !string.IsNullOrWhiteSpace(ActiveScene) &&
                               RouteDefinitionAsset != null &&
                               RouteProfileAsset != null &&
                               TransitionStyleAsset != null &&
                               RouteDefinition.RouteKind != SceneRouteKind.Unspecified &&
                               RouteProfile.IsValid &&
                               TransitionStyleDefinition.Profile != null &&
                               !string.IsNullOrWhiteSpace(Source) &&
                               !string.IsNullOrWhiteSpace(Reason);
    }
}
