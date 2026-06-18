using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    [Serializable]
    public readonly struct ActorImpactTarget
    {
        public ActorImpactTarget(
            GameObject targetObject,
            Collider targetCollider,
            Actor targetActor,
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            string reason)
        {
            TargetObject = targetObject;
            TargetCollider = targetCollider;
            TargetActor = targetActor;
            TargetActorId = targetActorId;
            TargetActorInstanceRuntimeId = targetActorInstanceRuntimeId;
            Reason = reason.TrimToEmpty();
        }

        public GameObject TargetObject { get; }
        public Collider TargetCollider { get; }
        public Actor TargetActor { get; }
        public ActorId TargetActorId { get; }
        public ActorInstanceRuntimeId TargetActorInstanceRuntimeId { get; }
        public string Reason { get; }

        public bool HasTargetObject => TargetObject != null;
        public bool HasTargetCollider => TargetCollider != null;
        public bool HasTargetActor => TargetActor != null && TargetActorId.IsValid && TargetActorInstanceRuntimeId.IsValid;
        public bool IsResolved => HasTargetActor;
        public string TargetObjectName => TargetObject == null ? string.Empty : TargetObject.name;
        public string TargetColliderName => TargetCollider == null ? string.Empty : TargetCollider.name;

        public static ActorImpactTarget Unresolved(
            GameObject targetObject,
            Collider targetCollider,
            string reason)
        {
            return new ActorImpactTarget(
                targetObject,
                targetCollider,
                null,
                default,
                default,
                reason);
        }
    }
}
