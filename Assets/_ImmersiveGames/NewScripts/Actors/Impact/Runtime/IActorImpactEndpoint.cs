using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    public interface IActorImpactEndpoint
    {
        ActorId ImpactActorId { get; }
        ActorInstanceRuntimeId ImpactActorInstanceRuntimeId { get; }
        ActorId OwnerActorId { get; }
        ActorInstanceRuntimeId OwnerActorInstanceRuntimeId { get; }
        string ImpactKind { get; }
        bool IsConfigured { get; }

        void Configure(
            ActorId impactActorId,
            ActorInstanceRuntimeId impactActorInstanceRuntimeId,
            ActorId ownerActorId,
            ActorInstanceRuntimeId ownerActorInstanceRuntimeId,
            string impactKind,
            string source,
            string reason);

        bool TryRegisterImpactFromGameObject(
            GameObject targetObject,
            string source,
            string reason,
            out ActorImpactResult result);
    }
}
