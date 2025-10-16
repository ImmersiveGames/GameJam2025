using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DamageSystem
{
    public class DamageSystem
    {
        private static readonly Dictionary<(string, string), float> _lastHit = new();
        private const float DamageCooldown = 0.5f;

        public static bool CanApply(string attackerId, string targetId)
        {
            var key = (attackerId, targetId);
            if (_lastHit.TryGetValue(key, out var lastTime))
            {
                if (Time.time - lastTime < DamageCooldown)
                    return false;
            }
            _lastHit[key] = Time.time;
            return true;
        }
    }
}