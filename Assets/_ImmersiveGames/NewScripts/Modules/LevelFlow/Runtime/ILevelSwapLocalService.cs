using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public interface ILevelSwapLocalService
    {
        Task SwapLocalAsync(LevelDefinitionAsset targetLevelRef, string reason = null, CancellationToken ct = default);

        [System.Obsolete("Legacy LevelId swap path is disabled in canonical LevelFlow.")]
        Task SwapLocalAsync(LevelId levelId, string reason = null, CancellationToken ct = default);
    }
}
