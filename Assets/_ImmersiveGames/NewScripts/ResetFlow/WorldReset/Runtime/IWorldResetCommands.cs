using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime
{
    public interface IWorldResetCommands
    {
        Task ResetMacroAsync(SceneRouteId macroRouteId, string reason, string macroSignature, CancellationToken ct);
    }
}

