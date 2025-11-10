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
        public readonly bool DisableSkin;
        public readonly bool TriggersGameOver;

        public DeathEvent(string entityId, ResourceType resourceType, bool disableSkin = true, bool triggersGameOver = false)
        {
            EntityId = entityId;
            ResourceType = resourceType;
            DisableSkin = disableSkin;
            TriggersGameOver = triggersGameOver;
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

    public struct DamagePipelineStarted : IEvent
    {
        public readonly string AttackerId;
        public readonly string TargetId;
        public readonly ResourceType ResourceType;
        public readonly DamageType DamageType;
        public readonly float RequestedDamage;

        public DamagePipelineStarted(string attackerId, string targetId, ResourceType resourceType,
            DamageType damageType, float requestedDamage)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            ResourceType = resourceType;
            DamageType = damageType;
            RequestedDamage = requestedDamage;
        }
    }

    public struct DamagePipelineCompleted : IEvent
    {
        public readonly string AttackerId;
        public readonly string TargetId;
        public readonly ResourceType ResourceType;
        public readonly DamageType DamageType;
        public readonly float RequestedDamage;
        public readonly float FinalDamage;

        public DamagePipelineCompleted(string attackerId, string targetId, ResourceType resourceType,
            DamageType damageType, float requestedDamage, float finalDamage)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            ResourceType = resourceType;
            DamageType = damageType;
            RequestedDamage = requestedDamage;
            FinalDamage = finalDamage;
        }
    }

    public struct DamagePipelineFailed : IEvent
    {
        public readonly string AttackerId;
        public readonly string TargetId;
        public readonly ResourceType ResourceType;
        public readonly DamageType DamageType;
        public readonly float RequestedDamage;
        public readonly string FailedCommandName;

        public DamagePipelineFailed(string attackerId, string targetId, ResourceType resourceType,
            DamageType damageType, float requestedDamage, string failedCommandName)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            ResourceType = resourceType;
            DamageType = damageType;
            RequestedDamage = requestedDamage;
            FailedCommandName = failedCommandName;
        }
    }

    public struct DamagePipelineUndone : IEvent
    {
        public readonly string AttackerId;
        public readonly string TargetId;
        public readonly ResourceType ResourceType;
        public readonly DamageType DamageType;
        public readonly float RestoredDamage;

        public DamagePipelineUndone(string attackerId, string targetId, ResourceType resourceType,
            DamageType damageType, float restoredDamage)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            ResourceType = resourceType;
            DamageType = damageType;
            RestoredDamage = restoredDamage;
        }
    }
}
