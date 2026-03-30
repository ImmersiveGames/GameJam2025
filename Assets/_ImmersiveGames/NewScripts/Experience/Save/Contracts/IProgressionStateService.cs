using _ImmersiveGames.NewScripts.Experience.Save.Models;
namespace _ImmersiveGames.NewScripts.Experience.Save.Contracts
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
