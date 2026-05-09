using _ImmersiveGames.NewScripts.ActorsSystem.Models;
namespace _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound
{
    /// <summary>
    /// Inbound read port for semantic participation owned by the canonical pipeline/session activity layer.
    /// </summary>
    public interface IActorsSemanticParticipationInPort
    {
        bool TryGetCurrent(out ActorsSemanticParticipationSnapshot snapshot);
    }
}
