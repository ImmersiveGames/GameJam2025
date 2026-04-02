using _ImmersiveGames.NewScripts.Experience.Save.Models;
namespace _ImmersiveGames.NewScripts.Experience.Save.Contracts
{
    public interface IProgressionBackend
    {
        string BackendId { get; }

        bool IsBackendAvailable { get; }

        bool TryLoad(
            string profileId,
            string slotId,
            out ProgressionSnapshot snapshot,
            out string reason);

        bool TrySave(
            ProgressionSnapshot snapshot,
            out string reason);
    }
}
