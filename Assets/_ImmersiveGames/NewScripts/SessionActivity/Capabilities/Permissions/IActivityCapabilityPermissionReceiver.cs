namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    public interface IActivityCapabilityPermissionReceiver
    {
        ActivityCapabilityPermissionReceiverId ReceiverId { get; }

        void OnPermissionChanged(ActivityCapabilityPermissionFact fact);
    }
}
