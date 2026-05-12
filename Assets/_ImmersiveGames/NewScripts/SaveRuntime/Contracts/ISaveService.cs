using _ImmersiveGames.NewScripts.SaveRuntime.Models;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Contracts
{
    public interface ISaveService
    {
        bool TryLoad(
            SaveIdentity identity,
            out SaveRecord record,
            out string reason);

        bool TrySave(
            SaveRecord record,
            out string reason);

        bool TrySaveCurrent(out string reason);
    }
}

