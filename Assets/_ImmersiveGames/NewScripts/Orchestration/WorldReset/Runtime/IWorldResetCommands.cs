using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime
{
    public interface IWorldResetCommands
    {
        Task ResetMacroAsync(SceneRouteId macroRouteId, string reason, string macroSignature, CancellationToken ct);
        Task ResetLevelAsync(PhaseResetContext resetContext, string reason, CancellationToken ct);
    }
}
