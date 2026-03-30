namespace _ImmersiveGames.NewScripts.Modules.Save.Contracts
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
