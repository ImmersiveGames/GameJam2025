using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
namespace _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound
{
    /// <summary>
    /// Inbound port for authoritative participant/runtime materialization mapping updates.
    /// </summary>
    public interface IActorsParticipantRuntimeMappingInPort
    {
        bool Upsert(ActorsParticipantRuntimeMappingEntry entry);
        void Clear(string reason = null);
    }

    /// <summary>
    /// Query/read port for authoritative participant/runtime materialization mappings.
    /// </summary>
    public interface IActorsParticipantRuntimeMappingQueryPort
    {
        ActorsParticipantRuntimeMappingSnapshot Current { get; }
        bool TryGetCurrent(out ActorsParticipantRuntimeMappingSnapshot snapshot);
        bool TryGetByParticipantId(string participantId, out ActorsParticipantRuntimeMappingEntry entry);
        bool TryGetByAxisActorId(AxisActorId axisActorId, out ActorsParticipantRuntimeMappingEntry entry);
        bool TryGetByRuntimeActorId(RuntimeActorId runtimeActorId, out ActorsParticipantRuntimeMappingEntry entry);
        bool TryGetAll(List<ActorsParticipantRuntimeMappingEntry> target);
    }
}
