// Assets/_ImmersiveGames/NewScripts/Gameplay/Phases/IPhaseChangeService.cs
#nullable enable
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    /// <summary>
    /// API de produção para solicitar “nova fase”.
    /// - InPlace: fase + hard reset na mesma cena.
    /// - WithTransition: fase + SceneFlow (fade/loading) + reset após ScenesReady.
    /// </summary>
    public interface IPhaseChangeService
    {
        Task RequestPhaseInPlaceAsync(PhasePlan plan, string reason);
        Task RequestPhaseInPlaceAsync(PhasePlan plan, string reason, PhaseChangeOptions? options);
        Task RequestPhaseInPlaceAsync(string phaseId, string reason, PhaseChangeOptions? options = null);

        Task RequestPhaseWithTransitionAsync(PhasePlan plan, SceneTransitionRequest transition, string reason);
        Task RequestPhaseWithTransitionAsync(PhasePlan plan, SceneTransitionRequest transition, string reason, PhaseChangeOptions? options);
        Task RequestPhaseWithTransitionAsync(string phaseId, SceneTransitionRequest transition, string reason, PhaseChangeOptions? options = null);
    }
}
