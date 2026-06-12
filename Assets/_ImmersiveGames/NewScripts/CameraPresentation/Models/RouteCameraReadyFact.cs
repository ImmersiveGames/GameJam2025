namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class RouteCameraReadyFact
    {
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string SurfaceKind { get; }
        public string RequirementId { get; }
        public string OutputCameraName { get; }
        public string PresentationRigName { get; }
        public string Source { get; }
        public string Reason { get; }

        public RouteCameraReadyFact(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string surfaceKind,
            string requirementId,
            string outputCameraName,
            string presentationRigName,
            string source,
            string reason)
        {
            RouteIdentity = routeIdentity;
            RouteOperationId = routeOperationId;
            TransitionId = transitionId;
            RouteSequence = routeSequence;
            SurfaceKind = surfaceKind;
            RequirementId = requirementId;
            OutputCameraName = outputCameraName;
            PresentationRigName = presentationRigName;
            Source = source;
            Reason = reason;
        }

        public static RouteCameraReadyFact FromResult(
            RouteCameraBindingResult result,
            string source,
            string reason)
        {
            var command = result?.Command;

            var handle = result?.Handle;

            return new RouteCameraReadyFact(
                command != null ? command.RouteIdentity : string.Empty,
                command != null ? command.RouteOperationId : string.Empty,
                command != null ? command.TransitionId : string.Empty,
                command?.RouteSequence ?? 0,
                command != null ? command.SurfaceKind : string.Empty,
                handle != null ? handle.RequirementId : string.Empty,
                handle != null && handle.UnityCamera != null ? handle.UnityCamera.name : string.Empty,
                handle != null && handle.PresentationRigInstance != null ? handle.PresentationRigInstance.name : string.Empty,
                source,
                reason);
        }
    }
}
