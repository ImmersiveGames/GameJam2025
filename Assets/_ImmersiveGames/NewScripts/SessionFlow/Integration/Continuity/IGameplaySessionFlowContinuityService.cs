using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity
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

