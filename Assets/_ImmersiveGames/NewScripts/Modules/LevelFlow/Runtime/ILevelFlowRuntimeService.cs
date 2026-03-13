using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public interface ILevelFlowRuntimeService
    {
        Task StartGameplayDefaultAsync(string reason = null, CancellationToken ct = default);
        Task RestartLastGameplayAsync(string reason = null, CancellationToken ct = default);
        Task SwapLevelLocalAsync(LevelDefinitionAsset levelRef, string reason = null, CancellationToken ct = default);
    }
}
