using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public interface ILevelSwapLocalService
    {
        Task SwapLocalAsync(LevelDefinitionAsset targetLevelRef, string reason = null, CancellationToken ct = default);
    }
}
