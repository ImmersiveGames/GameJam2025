using _ImmersiveGames.NewScripts.SaveRuntime.Models;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Contracts
{
    public interface ISaveService
    {
        bool TryLoad(
            SaveAddress address,
            out SaveResult result,
            out string reason);

        bool TrySave(
            SaveRequest request,
            out SaveResult result,
            out string reason);

        bool TryDelete(
            SaveAddress address,
            out SaveResult result,
            out string reason);
    }
}
