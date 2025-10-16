using System;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    [Serializable]
    public class DamageContext
    {
        public readonly string AttackerId;
        public readonly string TargetId;
        public readonly float DamageValue;
        public readonly ResourceType TargetResource;
        public readonly DamageType DamageType;
        public readonly Vector3 HitPosition;
        public readonly Vector3 HitNormal;
        public readonly float Timestamp;

        public DamageContext(string attackerId, string targetId, float damageValue, ResourceType targetResource,
            DamageType damageType = DamageType.Physical,
            Vector3? hitPosition = null, Vector3? hitNormal = null)
        {
            AttackerId = attackerId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            DamageValue = damageValue;
            TargetResource = targetResource;
            DamageType = damageType;
            HitPosition = hitPosition ?? Vector3.zero;
            HitNormal = hitNormal ?? Vector3.zero;
            Timestamp = Time.time;
        }
    }
}