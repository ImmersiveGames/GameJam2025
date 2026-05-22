namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class RouteCameraReleasedFact
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string SurfaceKind { get; }
        public string RequirementId { get; }
        public string Source { get; }
        public string Reason { get; }

        public RouteCameraReleasedFact(
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

        public static RouteCameraReleasedFact FromCommand(
            RouteCameraReleaseCommand command,
            string source,
            string reason)
        {
            return new RouteCameraReleasedFact(
                command != null ? command.RouteIdentity : string.Empty,
                command != null ? command.RouteOperationId : string.Empty,
                command != null ? command.TransitionId : string.Empty,
                command != null ? command.RouteSequence : 0,
                command != null ? command.SurfaceKind : string.Empty,
                command != null ? command.RequirementId : string.Empty,
                source,
                reason);
        }
    }
}
