using System;
using UnityEngine;
using _ImmersiveGames.Scripts.DamageSystem;

namespace _ImmersiveGames.Scripts.DamageSystem.Strategies
{
    public enum DamageStrategyType
    {
        Basic,
        Critical,
        Resistance
    }

    [Serializable]
    public class CriticalDamageSettings
    {
        [Range(0f, 1f)] public float criticalChance = 0.2f;
        [Range(1f, 3f)] public float criticalMultiplier = 2f;
    }

    public static class DamageStrategyFactory
    {
        public static IDamageStrategy Create(
            DamageStrategyType type,
            CriticalDamageSettings criticalSettings,
            DamageModifiers resistanceModifiers
        )
        {
            switch (type)
            {
                case DamageStrategyType.Critical:
                    return new CriticalDamageStrategy
                    {
                        criticalChance = Mathf.Clamp01(criticalSettings?.criticalChance ?? 0.2f),
                        criticalMultiplier = Mathf.Max(1f, criticalSettings?.criticalMultiplier ?? 2f)
                    };

                case DamageStrategyType.Resistance:
                    return new ResistanceDamageStrategy
                    {
                        modifiers = resistanceModifiers ?? new DamageModifiers()
                    };

                case DamageStrategyType.Basic:
                default:
                    return new BasicDamageStrategy();
            }
        }
    }
}
