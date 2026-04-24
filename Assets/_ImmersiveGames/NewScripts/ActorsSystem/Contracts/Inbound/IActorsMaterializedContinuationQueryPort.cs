using _ImmersiveGames.NewScripts.ActorsSystem.Models;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound
{
    /// <summary>
    /// Read port explicita para continuidade materializada entre entradas de fase.
    /// </summary>
    public interface IActorsMaterializedContinuationQueryPort
    {
        bool TryGetCurrent(out ActorsParticipantRuntimeMappingSnapshot snapshot);
        bool TryGetByAxisActorId(AxisActorId axisActorId, out ActorsParticipantRuntimeMappingEntry entry);
    }
}
