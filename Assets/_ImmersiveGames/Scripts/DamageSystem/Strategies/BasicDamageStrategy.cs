using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.DamageSystem;
namespace _ImmersiveGames.Scripts.DamageSystem.Strategies
{
    /// <summary>
    /// Estratégia simples — aplica o valor puro sem modificadores.
    /// </summary>
    [System.Serializable]
    public class BasicDamageStrategy : IDamageStrategy
    {
        public float CalculateDamage(DamageContext ctx) => ctx.DamageValue;
    }
    
    /// <summary>
    /// Adiciona chance de crítico configurável.
    /// </summary>
    [System.Serializable]
    public class CriticalDamageStrategy : IDamageStrategy
    {
        [Range(0f, 1f)] public float criticalChance = 0.2f;
        [Range(1f, 3f)] public float criticalMultiplier = 2f;

        public float CalculateDamage(DamageContext ctx)
        {
            bool isCritical = Random.value <= criticalChance;
            return ctx.DamageValue * (isCritical ? criticalMultiplier : 1f);
        }
    }
    
    /// <summary>
    /// Aplica modificadores baseados em resistências e vulnerabilidades.
    /// </summary>
    [System.Serializable]
    public class ResistanceDamageStrategy : IDamageStrategy
    {
        public DamageModifiers modifiers;

        public float CalculateDamage(DamageContext ctx)
        {
            float multiplier = modifiers.GetModifier(ctx.DamageType);
            return ctx.DamageValue * multiplier;
        }
    }
    /// <summary>
    /// Permite combinar múltiplas estratégias (executadas em sequência).
    /// </summary>
    [System.Serializable]
    public class CompositeDamageStrategy : IDamageStrategy
    {
        [SerializeReference]
        private List<IDamageStrategy> strategies = new();

        public float CalculateDamage(DamageContext ctx)
        {
            float current = ctx.DamageValue;

            foreach (var s in strategies)
            {
                if (s == null) continue;
                current = s.CalculateDamage(
                    new DamageContext(
                        ctx.AttackerId,
                        ctx.TargetId,
                        current,
                        ctx.TargetResource,
                        ctx.DamageType,
                        ctx.HitPosition,
                        ctx.HitNormal
                    )
                );
            }

            return current;
        }
    }
}