using ImmersiveGames.GameJam2025.Experience.Save.Models;
namespace ImmersiveGames.GameJam2025.Experience.Save.Contracts
{
    public interface IProgressionStateService
    {
        bool HasSnapshot { get; }

        ProgressionSnapshot CurrentSnapshot { get; }

        void SetCurrent(
            ProgressionSnapshot snapshot,
            string reason);
    }
}

