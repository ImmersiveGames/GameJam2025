using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.ActorSystem.Contracts.Outbound
{
    /// <summary>
    /// Outbound read-only port for actor runtime presence.
    /// </summary>
    public interface IActorPresenceReadPort
    {
        bool TryGetAll(List<ActorRuntimePresenceSnapshot> target);
        bool TryGetByActorId(string actorId, out ActorRuntimePresenceSnapshot snapshot);
    }
}