namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    public interface IActivityPermissionReceiverProvider
    {
        string ReceiverId { get; }

        bool TryCreateReceiver(out IActivityCapabilityPermissionReceiver receiver);
    }
}
