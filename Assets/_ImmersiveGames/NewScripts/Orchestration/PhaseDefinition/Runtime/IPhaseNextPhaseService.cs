using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public interface IPhaseNextPhaseService
    {
        Task NextPhaseAsync(string reason = null, CancellationToken ct = default);
    }
}
