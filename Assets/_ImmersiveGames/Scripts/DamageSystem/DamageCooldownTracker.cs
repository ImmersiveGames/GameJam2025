using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Gerencia cooldown entre pares (attacker -> target)
    /// </summary>
    public class DamageCooldownTracker
    {
        private readonly Dictionary<string, float> _cooldowns = new();
        private readonly float _cooldownTime;

        public DamageCooldownTracker(float cooldownTime = 0.5f)
        {
            _cooldownTime = cooldownTime;
        }

        public bool CanDealDamage(string attackerId, string targetId)
        {
            string key = $"{attackerId}_{targetId}";
            if (!_cooldowns.TryGetValue(key, out float lastTime))
            {
                _cooldowns[key] = Time.time;
                return true;
            }

            if (Time.time - lastTime >= _cooldownTime)
            {
                _cooldowns[key] = Time.time;
                return true;
            }

            return false;
        }
    }
}