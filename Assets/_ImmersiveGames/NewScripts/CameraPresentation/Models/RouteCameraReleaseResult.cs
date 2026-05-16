namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class RouteCameraReleaseResult
    {
        public bool Success { get; }
        public RouteCameraReleasedFact ReleasedFact { get; }
        public RouteCameraReleaseFailureFact FailureFact { get; }
        public string Reason { get; }

        private RouteCameraReleaseResult(
            bool success,
            RouteCameraReleasedFact releasedFact,
            RouteCameraReleaseFailureFact failureFact,
            string reason)
        {
            Success = success;
            ReleasedFact = releasedFact;
            FailureFact = failureFact;
            Reason = reason;
        }

        public static RouteCameraReleaseResult Released(
            RouteCameraReleasedFact releasedFact,
            string reason)
        {
            return new RouteCameraReleaseResult(
                true,
                releasedFact,
                null,
                reason);
        }

        public static RouteCameraReleaseResult Failed(
            RouteCameraReleaseFailureFact failureFact,
            string reason)
        {
            return new RouteCameraReleaseResult(
                false,
                null,
                failureFact,
                reason);
        }
    }
}
