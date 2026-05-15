namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraPreparationResult
    {
        public bool Success { get; }
        public ActivityCameraReadyFact ReadyFact { get; }
        public ActivityCameraFailureFact FailureFact { get; }
        public ActivityCameraBindingHandle Handle => ReadyFact != null ? ReadyFact.Handle : null;
        public string Reason { get; }

        private ActivityCameraPreparationResult(
            bool success,
            ActivityCameraReadyFact readyFact,
            ActivityCameraFailureFact failureFact,
            string reason)
        {
            Success = success;
            ReadyFact = readyFact;
            FailureFact = failureFact;
            Reason = reason;
        }

        public static ActivityCameraPreparationResult Ready(
            ActivityCameraReadyFact readyFact,
            string reason)
        {
            return new ActivityCameraPreparationResult(
                true,
                readyFact,
                null,
                reason);
        }

        public static ActivityCameraPreparationResult Failed(
            ActivityCameraFailureFact failureFact,
            string reason)
        {
            return new ActivityCameraPreparationResult(
                false,
                null,
                failureFact,
                reason);
        }
    }
}
