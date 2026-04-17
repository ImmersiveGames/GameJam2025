using System.Threading;
using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Experience.PostRun.Contracts;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime;

namespace ImmersiveGames.GameJam2025.Orchestration.Navigation.Runtime
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

