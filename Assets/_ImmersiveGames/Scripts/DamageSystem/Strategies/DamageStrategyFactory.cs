using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    [Serializable]
    public class DamageStrategySelection
    {
        public DamageStrategyType type = DamageStrategyType.Basic;
        public CriticalDamageSettings criticalSettings = new();
        public DamageModifiers resistanceModifiers = new();

        public void EnsureInitialized()
        {
            criticalSettings ??= new CriticalDamageSettings();
            resistanceModifiers ??= new DamageModifiers();
        }
    }

    public static class DamageStrategyFactory
    {
        public static IDamageStrategy Create(DamageStrategySelection selection)
        {
            if (selection == null)
                return new BasicDamageStrategy();

            selection.EnsureInitialized();

            switch (selection.type)
            {
                case DamageStrategyType.Critical:
                    return new CriticalDamageStrategy
                    {
                        criticalChance = Mathf.Clamp01(selection.criticalSettings.criticalChance),
                        criticalMultiplier = Mathf.Max(1f, selection.criticalSettings.criticalMultiplier)
                    };

                case DamageStrategyType.Resistance:
                    return new ResistanceDamageStrategy
                    {
                        modifiers = selection.resistanceModifiers ?? new DamageModifiers()
                    };

                case DamageStrategyType.Basic:
                default:
                    return new BasicDamageStrategy();
            }
        }

        public static IDamageStrategy CreatePipeline(IReadOnlyList<DamageStrategySelection> selections)
        {
            if (selections == null || selections.Count == 0)
                return new BasicDamageStrategy();

            if (selections.Count == 1)
                return Create(selections[0]);

            var strategies = new List<IDamageStrategy>(selections.Count);
            strategies.AddRange(from selection in selections where selection != null select Create(selection) into strategy where strategy != null select strategy);

            return strategies.Count switch
            {
                0 => new BasicDamageStrategy(),
                1 => strategies[0],
                _ => new CompositeDamageStrategy(strategies)
            };

        }
    }
}
