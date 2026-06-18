using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    [Serializable]
    public readonly struct ActorImpactIntent
    {
        public ActorImpactIntent(
            ActorId impactActorId,
            ActorInstanceRuntimeId impactActorInstanceRuntimeId,
            ActorId ownerActorId,
            ActorInstanceRuntimeId ownerActorInstanceRuntimeId,
            ActorId targetActorId,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId,
            string impactKind,
            string targetObjectName,
            string targetColliderName,
            ActorImpactContact contact,
            string source,
            string reason)
        {
            ImpactActorId = impactActorId;
            ImpactActorInstanceRuntimeId = impactActorInstanceRuntimeId;
            OwnerActorId = ownerActorId;
            OwnerActorInstanceRuntimeId = ownerActorInstanceRuntimeId;
            TargetActorId = targetActorId;
            TargetActorInstanceRuntimeId = targetActorInstanceRuntimeId;
            ImpactKind = impactKind.TrimToEmpty();
            TargetObjectName = targetObjectName.TrimToEmpty();
            TargetColliderName = targetColliderName.TrimToEmpty();
            Contact = contact;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public ActorId ImpactActorId { get; }
        public ActorInstanceRuntimeId ImpactActorInstanceRuntimeId { get; }
        public ActorId OwnerActorId { get; }
        public ActorInstanceRuntimeId OwnerActorInstanceRuntimeId { get; }
        public ActorId TargetActorId { get; }
        public ActorInstanceRuntimeId TargetActorInstanceRuntimeId { get; }
        public string ImpactKind { get; }
        public string TargetObjectName { get; }
        public string TargetColliderName { get; }
        public ActorImpactContact Contact { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasOwnerActor => OwnerActorId.IsValid && OwnerActorInstanceRuntimeId.IsValid;
        public bool HasTargetActor => TargetActorId.IsValid && TargetActorInstanceRuntimeId.IsValid;
        public bool IsValid =>
            ImpactActorId.IsValid &&
            ImpactActorInstanceRuntimeId.IsValid &&
            !string.IsNullOrWhiteSpace(ImpactKind) &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        public Vector3 ContactPoint => Contact.Point;
        public Vector3 ContactNormal => Contact.Normal;
}
}
