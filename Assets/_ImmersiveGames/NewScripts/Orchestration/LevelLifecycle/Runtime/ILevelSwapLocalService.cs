using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public interface ILevelSwapLocalService
    {
        Task SwapLocalAsync(LevelDefinitionAsset targetLevelRef, string reason = null, CancellationToken ct = default);
    }
}
