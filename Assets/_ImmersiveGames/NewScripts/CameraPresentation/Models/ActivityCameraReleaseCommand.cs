namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraReleaseCommand
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string ActivityIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public ActivityCameraReleaseCommand(
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
    }
}
