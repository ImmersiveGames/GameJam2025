namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraReleasedFact
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string ActivityIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public ActivityCameraReleasedFact(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string activityIdentity,
            string source,
            string reason)
        {
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            RouteSequence = routeSequence;
            ActivityIdentity = activityIdentity;
            Source = source;
            Reason = reason;
        }

        public static ActivityCameraReleasedFact FromCommand(
            ActivityCameraReleaseCommand command,
            string source,
            string reason)
        {
            return new ActivityCameraReleasedFact(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.ActivityIdentity,
                source,
                reason);
        }
    }
}
