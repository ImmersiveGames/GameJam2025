namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    public enum PermissionOutcomeKind
    {
        Unknown = 0,
        AcceptedStateChanged = 1,
        AcceptedIdempotentNoop = 2,
        RejectedMissingRequiredReceiver = 3,
        RejectedForeignIdentity = 4,
        RejectedStaleIdentity = 5,
        RejectedInvalidCommand = 6,
        Failed = 7
    }
}
