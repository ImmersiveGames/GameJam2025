using ImmersiveGames.GameJam2025.Experience.Save.Models;
namespace ImmersiveGames.GameJam2025.Experience.Save.Contracts
{
    public interface ICheckpointBackend
    {
        string BackendId { get; }

        bool IsBackendAvailable { get; }

        bool TryLoad(
            CheckpointIdentity identity,
            out CheckpointSnapshot snapshot,
            out string reason);

        bool TrySave(
            CheckpointSnapshot snapshot,
            out string reason);
    }
}

