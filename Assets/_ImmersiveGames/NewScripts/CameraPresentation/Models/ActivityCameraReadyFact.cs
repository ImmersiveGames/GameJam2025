namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraReadyFact
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string ActivityIdentity { get; }
        public string RequirementId { get; }
        public ActivityCameraBindingHandle Handle { get; }
        public string Source { get; }
        public string Reason { get; }

        public ActivityCameraReadyFact(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string activityIdentity,
            string requirementId,
            ActivityCameraBindingHandle handle,
            string source,
            string reason)
        {
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            RouteSequence = routeSequence;
            ActivityIdentity = activityIdentity;
            RequirementId = requirementId;
            Handle = handle;
            Source = source;
            Reason = reason;
        }

        public static ActivityCameraReadyFact FromResult(
            ActivityCameraBindingResult result,
            string source,
            string reason)
        {
            return new ActivityCameraReadyFact(
                result.RouteIdentity,
                result.RouteOperationId,
                result.TransitionId,
                result.RouteSequence,
                result.ActivityIdentity,
                result.RequirementId,
                result.Handle,
                source,
                reason);
        }
    }
}
