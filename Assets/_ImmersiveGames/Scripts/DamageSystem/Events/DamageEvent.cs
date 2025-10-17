using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public struct DamageEvent : IEvent
    {
        public string AttackerId;
        public string TargetId;
        public float FinalDamage;
        public ResourceType ResourceType;
        public DamageType DamageType;
        public Vector3 HitPosition;
    
        public DamageEvent(string attackerId, string targetId, float finalDamage,
            ResourceType resourceType, DamageType damageType, Vector3 hitPosition)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            FinalDamage = finalDamage;
            ResourceType = resourceType;
            DamageType = damageType;
            HitPosition = hitPosition;
        }
    }

    public struct DamageEventReverted : IEvent
    {
        public string AttackerId;
        public string TargetId;
        public float RestoredDamage;
        public ResourceType ResourceType;
        public DamageType DamageType;
        public Vector3 HitPosition;

        public DamageEventReverted(string attackerId, string targetId, float restoredDamage,
            ResourceType resourceType, DamageType damageType, Vector3 hitPosition)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            RestoredDamage = restoredDamage;
            ResourceType = resourceType;
            DamageType = damageType;
            HitPosition = hitPosition;
        }
    }

    public struct DeathEvent : IEvent
    {
        public readonly string EntityId;
        public readonly ResourceType ResourceType;

        public DeathEvent(string entityId, ResourceType resourceType)
        {
            EntityId = entityId;
            ResourceType = resourceType;
        }
    }

    public struct ReviveEvent: IEvent
    {
        public readonly string EntityId;
        public ReviveEvent(string id) => EntityId = id;
    }

    public struct ResetEvent: IEvent
    {
        public readonly string EntityId;
        public ResetEvent(string id) => EntityId = id;
    }
}
