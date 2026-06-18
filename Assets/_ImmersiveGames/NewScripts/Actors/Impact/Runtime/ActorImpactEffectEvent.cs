using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    [Serializable]
    public readonly struct ActorImpactEffectEvent
    {
        public ActorImpactEffectEvent(
            ActorImpactResult impactResult,
            float rawDamageAmount,
            bool damageApplicationRequested,
            bool damageApplicationCompleted,
            bool returnRequestEligible,
            string source,
            string reason)
        {
            ActorImpactIntent intent = impactResult.Intent;
            ImpactActorId = intent.ImpactActorId;
            ImpactActorInstanceRuntimeId = intent.ImpactActorInstanceRuntimeId;
            OwnerActorId = intent.OwnerActorId;
            OwnerActorInstanceRuntimeId = intent.OwnerActorInstanceRuntimeId;
            TargetActorId = intent.TargetActorId;
            TargetActorInstanceRuntimeId = intent.TargetActorInstanceRuntimeId;
            ImpactKind = Normalize(intent.ImpactKind);
            TargetObjectName = Normalize(intent.TargetObjectName);
            TargetColliderName = Normalize(intent.TargetColliderName);
            HasContact = intent.Contact.HasContact;
            ContactPoint = intent.Contact.Point;
            ContactNormal = intent.Contact.Normal;
            RawDamageAmount = rawDamageAmount;
            DamageApplicationRequested = damageApplicationRequested;
            DamageApplicationCompleted = damageApplicationCompleted;
            ReturnRequestEligible = returnRequestEligible;
            Source = Normalize(source);
            Reason = Normalize(reason);
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
        public bool HasContact { get; }
        public Vector3 ContactPoint { get; }
        public Vector3 ContactNormal { get; }
        public float RawDamageAmount { get; }
        public bool DamageApplicationRequested { get; }
        public bool DamageApplicationCompleted { get; }
        public bool ReturnRequestEligible { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            ImpactActorId.IsValid &&
            ImpactActorInstanceRuntimeId.IsValid &&
            !string.IsNullOrWhiteSpace(ImpactKind);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
