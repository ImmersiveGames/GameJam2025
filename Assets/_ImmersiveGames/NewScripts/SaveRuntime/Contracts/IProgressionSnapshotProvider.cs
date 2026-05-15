using _ImmersiveGames.NewScripts.SaveRuntime.Models;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Contracts
{
    public interface IProgressionSnapshotProvider
    {
        bool TryCaptureProgressionSnapshot(
            ProgressionSlotContext slotContext,
            out ProgressionSnapshotEnvelope envelope,
            out string reason);
    }
}
