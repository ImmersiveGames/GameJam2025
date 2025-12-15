using System;
using ImmersiveGames.RuntimeAttributes.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    [Serializable]
    public class DamageContext
    {
        public readonly string attackerId;
        public readonly string targetId;
        public readonly float damageValue;
        public readonly RuntimeAttributeType targetRuntimeAttribute;
        public readonly DamageType damageType;
        public readonly Vector3 hitPosition;
        public readonly Vector3 hitNormal;
        public readonly bool hasHitPosition;
        public readonly float timestamp;

        public DamageContext(string attackerId, string targetId, float damageValue, RuntimeAttributeType targetRuntimeAttribute,
            DamageType damageType = DamageType.Physical,
            Vector3? hitPosition = null, Vector3? hitNormal = null)
        {
            this.attackerId = attackerId ?? string.Empty;
            this.targetId = targetId ?? string.Empty;
            this.damageValue = damageValue;
            this.targetRuntimeAttribute = targetRuntimeAttribute;
            this.damageType = damageType;
            if (hitPosition.HasValue)
            {
                this.hitPosition = hitPosition.Value;
                hasHitPosition = true;
            }
            else
            {
                this.hitPosition = Vector3.zero;
                hasHitPosition = false;
            }

            this.hitNormal = hitNormal ?? Vector3.zero;
            timestamp = Time.time;
        }
    }
}