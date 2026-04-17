namespace ImmersiveGames.GameJam2025.Experience.Save.Contracts
{
    /// <summary>
    /// Placeholder seam for future manual checkpoint requests.
    /// </summary>
    public interface IManualCheckpointRequestService
    {
        bool TryRequestManualCheckpoint(
            string reason,
            out string response);
    }
}

