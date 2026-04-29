using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity
{
    public interface IGameplaySessionFlowContinuityService
    {
        Task<PhaseResetExecutionResult> RestartFromFirstPhaseAsync(string reason = null, CancellationToken ct = default);
        Task<PhaseResetExecutionResult> ResetCurrentPhaseAsync(string reason = null, CancellationToken ct = default);
        Task ExitToMenuAsync(string reason = null, CancellationToken ct = default);
    }
}

