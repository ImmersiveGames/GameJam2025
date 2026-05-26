namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    public interface IActivityCapabilityPermissionReceiver
    {
        string ReceiverId { get; }

        void OnPermissionChanged(ActivityCapabilityPermissionFact fact);
    }
}
