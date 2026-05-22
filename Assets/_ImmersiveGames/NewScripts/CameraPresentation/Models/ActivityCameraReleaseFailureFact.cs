namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraReleaseFailureFact
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string ActivityIdentity { get; }
        public string FailureReason { get; }
        public string Source { get; }
        public string Reason { get; }

        public ActivityCameraReleaseFailureFact(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string activityIdentity,
            string failureReason,
            string source,
            string reason)
        {
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            RouteSequence = routeSequence;
            ActivityIdentity = activityIdentity;
            FailureReason = failureReason;
            Source = source;
            Reason = reason;
        }

        public static ActivityCameraReleaseFailureFact FromCommand(
            ActivityCameraReleaseCommand command,
            string failureReason,
            string source,
            string reason)
        {
            return new ActivityCameraReleaseFailureFact(
                command != null ? command.RouteIdentity : string.Empty,
                command != null ? command.RouteOperationId : string.Empty,
                command != null ? command.TransitionId : string.Empty,
                command != null ? command.RouteSequence : 0,
                command != null ? command.ActivityIdentity : string.Empty,
                failureReason,
                source,
                reason);
        }
    }
}
