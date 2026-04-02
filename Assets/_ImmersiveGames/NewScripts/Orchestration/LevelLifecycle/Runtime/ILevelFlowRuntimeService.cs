using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public interface ILevelFlowRuntimeService
    {
        Task StartGameplayDefaultAsync(string reason = null, CancellationToken ct = default);
        Task RestartLastGameplayAsync(string reason = null, CancellationToken ct = default);
        Task RestartFromFirstLevelAsync(string reason = null, CancellationToken ct = default);
        Task ResetCurrentLevelAsync(string reason = null, CancellationToken ct = default);
        Task SwapLevelLocalAsync(LevelDefinitionAsset levelRef, string reason = null, CancellationToken ct = default);
    }
}
