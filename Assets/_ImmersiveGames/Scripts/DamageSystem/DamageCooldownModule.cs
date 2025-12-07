using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    /// <summary>
    /// Controla o cooldown de dano entre pares de atacantes e alvos.
    /// Cada DamageReceiver possui sua própria instância.
    /// </summary>
    public class DamageCooldownModule
    {
        private readonly float _cooldownTime;
        private readonly Dictionary<(string attackerId, string targetId), float> _cooldowns = new();

        public DamageCooldownModule(float cooldownTime)
        {
            _cooldownTime = cooldownTime;
        }

        public bool CanDealDamage(string attackerId, string targetId)
        {
            if (string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(targetId))
                return false;

            var key = (attackerId, targetId);
            if (_cooldowns.TryGetValue(key, out float lastHit))
            {
                if (Time.time - lastHit < _cooldownTime)
                    return false;
            }

            _cooldowns[key] = Time.time;
            return true;
        }

        public float? PeekCooldown(string attackerId, string targetId)
        {
            if (string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(targetId))
                return null;

            var key = (attackerId, targetId);
            return _cooldowns.TryGetValue(key, out float value) ? value : null;
        }

        public void RestoreCooldown(string attackerId, string targetId, float? timestamp)
        {
            if (string.IsNullOrEmpty(attackerId) || string.IsNullOrEmpty(targetId))
                return;

            var key = (attackerId, targetId);

            if (timestamp.HasValue)
            {
                _cooldowns[key] = timestamp.Value;
            }
            else
            {
                _cooldowns.Remove(key);
            }
        }

        public void ClearForActor(string actorId)
        {
            var keysToRemove = (from kvp in _cooldowns where kvp.Key.attackerId == actorId || kvp.Key.targetId == actorId select kvp.Key).ToList();

            foreach (var key in keysToRemove)
                _cooldowns.Remove(key);
        }

        public void Clear() => _cooldowns.Clear();
    }
}