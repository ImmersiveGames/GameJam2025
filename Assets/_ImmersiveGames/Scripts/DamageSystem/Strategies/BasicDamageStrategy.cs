using System;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.DamageSystem;

namespace _ImmersiveGames.Scripts.DamageSystem.Strategies
{
    /// <summary>
    /// Estratégia simples — aplica o valor puro sem modificadores.
    /// </summary>
    [Serializable]
    public class BasicDamageStrategy : IDamageStrategy
    {
        public float CalculateDamage(DamageContext ctx) => ctx.DamageValue;
    }

    /// <summary>
    /// Adiciona chance de crítico configurável.
    /// </summary>
    [Serializable]
    public class CriticalDamageStrategy : IDamageStrategy
    {
        [Range(0f, 1f)] public float criticalChance = 0.2f;
        [Range(1f, 3f)] public float criticalMultiplier = 2f;

        public float CalculateDamage(DamageContext ctx)
        {
            bool isCritical = UnityEngine.Random.value <= Mathf.Clamp01(criticalChance);
            float multiplier = Mathf.Max(1f, criticalMultiplier);
            return ctx.DamageValue * (isCritical ? multiplier : 1f);
        }
    }

    /// <summary>
    /// Aplica modificadores baseados em resistências e vulnerabilidades.
    /// </summary>
    [Serializable]
    public class ResistanceDamageStrategy : IDamageStrategy
    {
        public DamageModifiers modifiers = new();

        public float CalculateDamage(DamageContext ctx)
        {
            var table = modifiers ?? new DamageModifiers();
            float multiplier = table.GetModifier(ctx.DamageType);
            return ctx.DamageValue * multiplier;
        }
    }

    /// <summary>
    /// Permite combinar múltiplas estratégias (executadas em sequência).
    /// </summary>
    [Serializable]
    public class CompositeDamageStrategy : IDamageStrategy
    {
        private readonly List<IDamageStrategy> _strategies;

        public CompositeDamageStrategy(IEnumerable<IDamageStrategy> strategies)
        {
            _strategies = new List<IDamageStrategy>();

            if (strategies == null)
                return;

            foreach (var strategy in strategies)
            {
                if (strategy == null)
                    continue;

                _strategies.Add(strategy);
            }
        }

        public float CalculateDamage(DamageContext ctx)
        {
            if (_strategies == null || _strategies.Count == 0)
                return ctx.DamageValue;

            float current = ctx.DamageValue;

            foreach (var strategy in _strategies)
            {
                var strategyContext = new DamageContext(
                    ctx.AttackerId,
                    ctx.TargetId,
                    current,
                    ctx.TargetResource,
                    ctx.DamageType,
                    ctx.HitPosition,
                    ctx.HitNormal
                );

                current = strategy.CalculateDamage(strategyContext);
            }

            return current;
        }
    }
}
