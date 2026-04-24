using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound
{
    /// <summary>
    /// Inbound operational port for late binding updates coming from runtime operational layers (Unity Input System, spawn bridges, etc.).
    /// This does not define semantic ownership.
    /// </summary>
    public interface IActorsOperationalBindingInPort
    {
        bool Upsert(ActorsOperationalBindingEntry entry);
        bool TrySetState(AxisActorId axisActorId, ActorsOperationalBindingState nextState, string reason = null, string source = null);
        void Clear(string reason = null);
    }

    /// <summary>
    /// Read/query boundary for current operational binding snapshot.
    /// </summary>
    public interface IActorsOperationalBindingQueryPort
    {
        ActorsOperationalBindingSnapshot Current { get; }
        bool TryGetCurrent(out ActorsOperationalBindingSnapshot snapshot);
        bool TryGetByAxisActorId(AxisActorId axisActorId, out ActorsOperationalBindingEntry entry);
        bool TryGetByParticipantId(string participantId, out ActorsOperationalBindingEntry entry);
        bool TryGetByRuntimeActorId(RuntimeActorId runtimeActorId, out ActorsOperationalBindingEntry entry);
        bool TryGetAll(List<ActorsOperationalBindingEntry> target);
    }
}
