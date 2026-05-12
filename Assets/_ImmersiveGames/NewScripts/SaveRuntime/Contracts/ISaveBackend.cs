using _ImmersiveGames.NewScripts.SaveRuntime.Models;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Contracts
{
    public interface ISaveBackend
    {
        string BackendId { get; }

        bool TryLoad(
            SaveIdentity identity,
            out SaveRecord record,
            out string reason);

        bool TrySave(
            SaveRecord record,
            out string reason);

        bool TryExists(
            SaveIdentity identity,
            out bool exists,
            out string reason);

        bool TryDelete(
            SaveIdentity identity,
            out string reason);
    }
}

