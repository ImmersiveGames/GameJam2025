using _ImmersiveGames.NewScripts.SaveRuntime.Models;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Contracts
{
    public interface ICheckpointService
    {
        string BackendId { get; }

        bool IsBackendAvailable { get; }

        CheckpointIdentity RequiredIdentity { get; }

        bool HasSnapshot { get; }

        CheckpointSnapshot CurrentSnapshot { get; }

        void SetCurrent(
            CheckpointSnapshot snapshot,
            string reason);

        bool TryLoad(
            CheckpointIdentity identity,
            out CheckpointSnapshot snapshot,
            out string reason);

        bool TrySaveCurrent(out string reason);

        bool TrySave(
            CheckpointSnapshot snapshot,
            out string reason);
    }
}

