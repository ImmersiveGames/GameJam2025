namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    public interface IActivityPermissionReceiverProvider
    {
        ActivityCapabilityPermissionReceiverId ReceiverId { get; }

        bool TryCreateReceiver(out IActivityCapabilityPermissionReceiver receiver);
    }
}
