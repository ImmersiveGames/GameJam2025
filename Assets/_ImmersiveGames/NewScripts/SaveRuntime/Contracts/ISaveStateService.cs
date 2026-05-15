using _ImmersiveGames.NewScripts.SaveRuntime.Models;

namespace _ImmersiveGames.NewScripts.SaveRuntime.Contracts
{
    public interface ISaveStateService
    {
        bool HasCurrent { get; }

        SaveCurrentState CurrentState { get; }

        bool TrySetCurrent(
            SaveCurrentState state,
            string reason,
            out string error);
    }
}
