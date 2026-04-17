using System.Threading;
using System.Threading.Tasks;

namespace ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime
{
    public interface IPhaseNextPhaseService
    {
        Task<PhaseNavigationResult> NavigateAsync(PhaseNavigationRequest request, CancellationToken ct = default);
        Task<PhaseNavigationResult> NextPhaseAsync(string reason = null, CancellationToken ct = default);
        Task<PhaseNavigationResult> PreviousPhaseAsync(string reason = null, CancellationToken ct = default);
        Task<PhaseNavigationResult> GoToSpecificPhaseAsync(string phaseId, string reason = null, CancellationToken ct = default);
        Task<PhaseNavigationResult> RestartCatalogAsync(string reason = null, CancellationToken ct = default);
    }
}

