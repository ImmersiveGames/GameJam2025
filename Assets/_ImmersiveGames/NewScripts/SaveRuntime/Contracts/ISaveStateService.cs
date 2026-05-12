using _ImmersiveGames.NewScripts.SaveRuntime.Models;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Contracts
{
    public interface ISaveStateService
    {
        bool HasCurrent { get; }

        SaveRecord CurrentRecord { get; }

        bool TrySetCurrent(
            SaveRecord record,
            string reason,
            out string error);
    }
}

