namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class RouteCameraFailureFact
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string SurfaceKind { get; }
        public string RequirementId { get; }
        public string FailureReason { get; }
        public string Source { get; }
        public string Reason { get; }

        public RouteCameraFailureFact(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string surfaceKind,
            string requirementId,
            string failureReason,
            string source,
            string reason)
        {
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            RouteSequence = routeSequence;
            SurfaceKind = surfaceKind;
            RequirementId = requirementId;
            FailureReason = failureReason;
            Source = source;
            Reason = reason;
        }

        public static RouteCameraFailureFact FromResult(
            RouteCameraBindingResult result,
            string failureReason,
            string source,
            string reason)
        {
            RouteCameraPresentationCommand command = result?.Command;

            RouteCameraPresentationRequirement requirement = command?.Requirement;

            return new RouteCameraFailureFact(
                command != null ? command.RouteIdentity : string.Empty,
                command != null ? command.RouteOperationId : string.Empty,
                command != null ? command.TransitionId : string.Empty,
                command?.RouteSequence ?? 0,
                command != null ? command.SurfaceKind : string.Empty,
                requirement != null ? requirement.RequirementId : string.Empty,
                failureReason,
                source,
                reason);
        }
    }
}
