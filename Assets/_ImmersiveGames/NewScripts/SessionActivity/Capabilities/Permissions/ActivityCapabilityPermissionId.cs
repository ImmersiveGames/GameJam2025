namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    public enum ActivityCapabilityPermissionId
    {
        Unknown = 0,
        ActivityGameplayControl = 1,
    }

    public static class ActivityCapabilityPermissionIds
    {
        public const string ActivityGameplayControl = "activity.gameplay.control";

        public static string ToToken(ActivityCapabilityPermissionId permissionId)
        {
            return permissionId switch
            {
                ActivityCapabilityPermissionId.ActivityGameplayControl => ActivityGameplayControl,
                _ => string.Empty,
            };
        }
    }
}
