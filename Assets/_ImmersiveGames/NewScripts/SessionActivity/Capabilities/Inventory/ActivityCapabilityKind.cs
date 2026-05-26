namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public enum ActivityCapabilityKind
    {
        Unknown = 0,
        PermissionTarget = 1,
        MovementEndpoint = 2,
        CameraEndpoint = 3,
        CameraTarget = 11,
        ResetEndpoint = 4,
        SnapshotProvider = 5,
        SnapshotRestoreEndpoint = 6,
        ReleaseEndpoint = 7,
        InteractionEndpoint = 8,
        PresentationEndpoint = 9,
        AttributeEndpoint = 10,
        Custom = 99,
    }
}
