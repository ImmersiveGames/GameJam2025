using _ImmersiveGames.NewScripts.SaveRuntime.Models;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Contracts
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

