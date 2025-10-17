using System.Collections.Generic;
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

        public void ClearForActor(string actorId)
        {
            var keysToRemove = new List<(string, string)>();
            foreach (var kvp in _cooldowns)
            {
                if (kvp.Key.attackerId == actorId || kvp.Key.targetId == actorId)
                    keysToRemove.Add(kvp.Key);
            }

            foreach (var key in keysToRemove)
                _cooldowns.Remove(key);
        }

        public void Clear() => _cooldowns.Clear();
    }
}