using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    public interface IActorImpactTargetResolver
    {
        bool TryResolveImpactTarget(
            GameObject targetObject,
            Collider targetCollider,
            string source,
            string reason,
            out ActorImpactTarget target);
    }
}
