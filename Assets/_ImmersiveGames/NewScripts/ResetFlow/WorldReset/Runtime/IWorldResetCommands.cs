using System.Threading;
using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Navigation.Runtime;
namespace ImmersiveGames.GameJam2025.Orchestration.WorldReset.Runtime
{
    public interface IWorldResetCommands
    {
        Task ResetMacroAsync(SceneRouteId macroRouteId, string reason, string macroSignature, CancellationToken ct);
    }
}

