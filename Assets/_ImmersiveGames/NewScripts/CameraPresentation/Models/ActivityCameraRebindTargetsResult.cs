namespace _ImmersiveGames.NewScripts.CameraPresentation.Models
{
    public sealed class ActivityCameraRebindTargetsResult
    {
        private ActivityCameraRebindTargetsResult(
            bool success,
            string activityIdentity,
            string reason)
        {
            Success = success;
            ActivityIdentity = activityIdentity;
            Reason = reason;
        }

        public bool Success { get; }
        public string ActivityIdentity { get; }
        public string Reason { get; }

        public static ActivityCameraRebindTargetsResult Bound(string activityIdentity, string reason)
        {
            return new ActivityCameraRebindTargetsResult(true, activityIdentity, reason);
        }

        public static ActivityCameraRebindTargetsResult Failed(string activityIdentity, string reason)
        {
            return new ActivityCameraRebindTargetsResult(false, activityIdentity, reason);
        }
    }
}
