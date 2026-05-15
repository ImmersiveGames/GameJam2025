using _ImmersiveGames.NewScripts.SaveRuntime.Models;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Contracts
{
    public interface IProgressionSnapshotReceiver
    {
        bool TryApplyProgressionSnapshot(
            ProgressionSnapshotEnvelope envelope,
            out string reason);
    }
}
