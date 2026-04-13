using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.Navigation.Runtime
{
    public interface IGameplaySessionFlowContinuityService
    {
        Task RestartGameplayAsync(RunRestart restart, CancellationToken ct = default);
        Task RestartFromFirstPhaseAsync(string reason = null, CancellationToken ct = default);
        Task ResetCurrentPhaseAsync(string reason = null, CancellationToken ct = default);
        Task<PhaseNavigationResult> NavigatePhaseAsync(PhaseNavigationRequest request, CancellationToken ct = default);
        Task<PhaseNavigationResult> NextPhaseAsync(string reason = null, CancellationToken ct = default);
        Task<PhaseNavigationResult> PreviousPhaseAsync(string reason = null, CancellationToken ct = default);
        Task ExitToMenuAsync(string reason = null, CancellationToken ct = default);
    }
}
