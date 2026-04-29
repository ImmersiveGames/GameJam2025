using _ImmersiveGames.NewScripts.SaveRuntime.Models;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Contracts
{
    public interface IProgressionSaveService
    {
        string BackendId { get; }

        bool IsBackendAvailable { get; }

        bool TryLoad(
            string profileId,
            string slotId,
            out ProgressionSnapshot snapshot,
            out string reason);

        bool TrySaveCurrent(out string reason);

        bool TrySave(
            ProgressionSnapshot snapshot,
            out string reason);
    }
}

