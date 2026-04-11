using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Orchestration.Navigation.Runtime
{
    public interface IGameplaySessionFlowContinuityService
    {
        Task RestartGameplayAsync(string reason = null, CancellationToken ct = default);
        Task RestartFromFirstPhaseAsync(string reason = null, CancellationToken ct = default);
        Task ResetCurrentPhaseAsync(string reason = null, CancellationToken ct = default);
        Task NextPhaseAsync(string reason = null, CancellationToken ct = default);
        Task ExitToMenuAsync(string reason = null, CancellationToken ct = default);
    }
}
