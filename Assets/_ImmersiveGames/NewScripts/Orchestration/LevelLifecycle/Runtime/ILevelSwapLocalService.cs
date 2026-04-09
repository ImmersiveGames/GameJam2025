using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public interface ILevelSwapLocalService
    {
        Task SwapLocalAsync(PhaseDefinitionSelectedEvent phaseSelection, string reason = null, CancellationToken ct = default);
    }
}
