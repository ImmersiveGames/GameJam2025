namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class RouteCameraReleaseCommand
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string SurfaceKind { get; }
        public string RequirementId { get; }
        public string Source { get; }
        public string Reason { get; }

        public RouteCameraReleaseCommand(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string surfaceKind,
            string requirementId,
            string source,
            string reason)
        {
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            RouteSequence = routeSequence;
            SurfaceKind = surfaceKind;
            RequirementId = requirementId;
            Source = source;
            Reason = reason;
        }
    }
}
