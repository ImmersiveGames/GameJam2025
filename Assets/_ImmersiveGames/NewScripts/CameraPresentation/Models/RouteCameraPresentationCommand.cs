namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class RouteCameraPresentationCommand
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string SurfaceKind { get; }
        public RouteCameraPresentationRequirement Requirement { get; }
        public string Source { get; }
        public string Reason { get; }

        public RouteCameraPresentationCommand(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string surfaceKind,
            RouteCameraPresentationRequirement requirement,
            string source,
            string reason)
        {
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            RouteSequence = routeSequence;
            SurfaceKind = surfaceKind;
            Requirement = requirement;
            Source = source;
            Reason = reason;
        }
    }
}
