namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class RouteCameraPresentationResult
    {
        public bool Success { get; }
        public RouteCameraReadyFact ReadyFact { get; }
        public RouteCameraFailureFact FailureFact { get; }
        public string Reason { get; }

        private RouteCameraPresentationResult(
            bool success,
            RouteCameraReadyFact readyFact,
            RouteCameraFailureFact failureFact,
            string reason)
        {
            Success = success;
            ReadyFact = readyFact;
            FailureFact = failureFact;
            Reason = reason;
        }

        public static RouteCameraPresentationResult Ready(
            RouteCameraReadyFact readyFact,
            string reason)
        {
            return new RouteCameraPresentationResult(
                true,
                readyFact,
                null,
                reason);
        }

        public static RouteCameraPresentationResult Failed(
            RouteCameraFailureFact failureFact,
            string reason)
        {
            return new RouteCameraPresentationResult(
                false,
                null,
                failureFact,
                reason);
        }
    }
}
