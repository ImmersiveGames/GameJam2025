namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraFailureFact
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string ActivityIdentity { get; }
        public string RequirementId { get; }
        public string FailureReason { get; }
        public string Source { get; }
        public string Reason { get; }

        public ActivityCameraFailureFact(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string activityIdentity,
            string requirementId,
            string failureReason,
            string source,
            string reason)
        {
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            RouteSequence = routeSequence;
            ActivityIdentity = activityIdentity;
            RequirementId = requirementId;
            FailureReason = failureReason;
            Source = source;
            Reason = reason;
        }

        public static ActivityCameraFailureFact FromResult(
            ActivityCameraBindingResult result,
            string source,
            string reason)
        {
            return new ActivityCameraFailureFact(
                result.RouteIdentity,
                result.RouteOperationId,
                result.TransitionId,
                result.RouteSequence,
                result.ActivityIdentity,
                result.RequirementId,
                result.Reason,
                source,
                reason);
        }
    }
}
