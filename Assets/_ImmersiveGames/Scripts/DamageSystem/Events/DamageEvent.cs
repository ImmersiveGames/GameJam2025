using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public struct DamageEvent : IEvent
    {
        public readonly string attackerId;
        public readonly string targetId;
        public readonly float finalDamage;
        public readonly RuntimeAttributeType runtimeAttributeType;
        public readonly DamageType damageType;
        public Vector3 hitPosition;
    
        public DamageEvent(string attackerId, string targetId, float finalDamage,
            RuntimeAttributeType runtimeAttributeType, DamageType damageType, Vector3 hitPosition)
        {
            this.attackerId = attackerId;
            this.targetId = targetId;
            this.finalDamage = finalDamage;
            this.runtimeAttributeType = runtimeAttributeType;
            this.damageType = damageType;
            this.hitPosition = hitPosition;
        }
    }

    public struct DamageEventReverted : IEvent
    {
        public string attackerId;
        public string targetId;
        public float restoredDamage;
        public RuntimeAttributeType runtimeAttributeType;
        public DamageType damageType;
        public Vector3 hitPosition;

        public DamageEventReverted(string attackerId, string targetId, float restoredDamage,
            RuntimeAttributeType runtimeAttributeType, DamageType damageType, Vector3 hitPosition)
        {
            this.attackerId = attackerId;
            this.targetId = targetId;
            this.restoredDamage = restoredDamage;
            this.runtimeAttributeType = runtimeAttributeType;
            this.damageType = damageType;
            this.hitPosition = hitPosition;
        }
    }

    public struct DeathEvent : IEvent
    {
        public readonly string entityId;
        public readonly RuntimeAttributeType runtimeAttributeType;
        public readonly bool disableSkin;
        public readonly bool triggersGameOver;

        public DeathEvent(string entityId, RuntimeAttributeType runtimeAttributeType, bool disableSkin = true, bool triggersGameOver = false)
        {
            this.entityId = entityId;
            this.runtimeAttributeType = runtimeAttributeType;
            this.disableSkin = disableSkin;
            this.triggersGameOver = triggersGameOver;
        }
    }

    public struct ReviveEvent: IEvent
    {
        public readonly string entityId;
        public ReviveEvent(string id) => entityId = id;
    }

    public struct ResetEvent: IEvent
    {
        public readonly string entityId;
        public ResetEvent(string id) => entityId = id;
    }

    public struct DamagePipelineStarted : IEvent
    {
        public readonly string attackerId;
        public readonly string targetId;
        public readonly RuntimeAttributeType runtimeAttributeType;
        public readonly DamageType damageType;
        public readonly float requestedDamage;

        public DamagePipelineStarted(string attackerId, string targetId, RuntimeAttributeType runtimeAttributeType,
            DamageType damageType, float requestedDamage)
        {
            this.attackerId = attackerId;
            this.targetId = targetId;
            this.runtimeAttributeType = runtimeAttributeType;
            this.damageType = damageType;
            this.requestedDamage = requestedDamage;
        }
    }

    public struct DamagePipelineCompleted : IEvent
    {
        public readonly string attackerId;
        public readonly string targetId;
        public readonly RuntimeAttributeType runtimeAttributeType;
        public readonly DamageType damageType;
        public readonly float requestedDamage;
        public readonly float finalDamage;

        public DamagePipelineCompleted(string attackerId, string targetId, RuntimeAttributeType runtimeAttributeType,
            DamageType damageType, float requestedDamage, float finalDamage)
        {
            this.attackerId = attackerId;
            this.targetId = targetId;
            this.runtimeAttributeType = runtimeAttributeType;
            this.damageType = damageType;
            this.requestedDamage = requestedDamage;
            this.finalDamage = finalDamage;
        }
    }

    public struct DamagePipelineFailed : IEvent
    {
        public readonly string attackerId;
        public readonly string targetId;
        public readonly RuntimeAttributeType runtimeAttributeType;
        public readonly DamageType damageType;
        public readonly float requestedDamage;
        public readonly string failedCommandName;

        public DamagePipelineFailed(string attackerId, string targetId, RuntimeAttributeType runtimeAttributeType,
            DamageType damageType, float requestedDamage, string failedCommandName)
        {
            this.attackerId = attackerId;
            this.targetId = targetId;
            this.runtimeAttributeType = runtimeAttributeType;
            this.damageType = damageType;
            this.requestedDamage = requestedDamage;
            this.failedCommandName = failedCommandName;
        }
    }

    public struct DamagePipelineUndone : IEvent
    {
        public readonly string attackerId;
        public readonly string targetId;
        public readonly RuntimeAttributeType runtimeAttributeType;
        public readonly DamageType damageType;
        public readonly float restoredDamage;

        public DamagePipelineUndone(string attackerId, string targetId, RuntimeAttributeType runtimeAttributeType,
            DamageType damageType, float restoredDamage)
        {
            this.attackerId = attackerId;
            this.targetId = targetId;
            this.runtimeAttributeType = runtimeAttributeType;
            this.damageType = damageType;
            this.restoredDamage = restoredDamage;
        }
    }
}
