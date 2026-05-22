namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraReleaseResult
    {
        public bool Success { get; }
        public ActivityCameraReleasedFact ReleasedFact { get; }
        public ActivityCameraReleaseFailureFact FailureFact { get; }
        public string Reason { get; }

        private ActivityCameraReleaseResult(
            bool success,
            ActivityCameraReleasedFact releasedFact,
            ActivityCameraReleaseFailureFact failureFact,
            string reason)
        {
            Success = success;
            ReleasedFact = releasedFact;
            FailureFact = failureFact;
            Reason = reason;
        }

        public static ActivityCameraReleaseResult Released(
            ActivityCameraReleasedFact releasedFact,
            string reason)
        {
            return new ActivityCameraReleaseResult(
                true,
                releasedFact,
                null,
                reason);
        }

        public static ActivityCameraReleaseResult Failed(
            ActivityCameraReleaseFailureFact failureFact,
            string reason)
        {
            return new ActivityCameraReleaseResult(
                false,
                null,
                failureFact,
                reason);
        }
    }
}
