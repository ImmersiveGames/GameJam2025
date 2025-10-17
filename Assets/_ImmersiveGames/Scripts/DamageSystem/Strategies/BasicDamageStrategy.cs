using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DamageSystem.Strategies
{
    /// <summary>
    /// Classe base serializável para cálculo de dano.
    /// Permite subclasses aparecerem no Inspector.
    /// </summary>
    [System.Serializable]
    public abstract class DamageStrategy
    {
        public abstract float CalculateDamage(DamageContext ctx);
    }
    /// <summary>
    /// Estratégia simples — aplica o valor puro sem modificadores.
    /// </summary>
    [System.Serializable]
    public class BasicDamageStrategy : DamageStrategy
    {
        public override float CalculateDamage(DamageContext ctx) => ctx.DamageValue;
    }
    
    /// <summary>
    /// Adiciona chance de crítico configurável.
    /// </summary>
    [System.Serializable]
    public class CriticalDamageStrategy : DamageStrategy
    {
        [Range(0f, 1f)] public float criticalChance = 0.2f;
        [Range(1f, 3f)] public float criticalMultiplier = 2f;

        public override float CalculateDamage(DamageContext ctx)
        {
            bool isCritical = Random.value <= criticalChance;
            return ctx.DamageValue * (isCritical ? criticalMultiplier : 1f);
        }
    }
    
    /// <summary>
    /// Aplica modificadores baseados em resistências e vulnerabilidades.
    /// </summary>
    [System.Serializable]
    public class ResistanceDamageStrategy : DamageStrategy
    {
        public DamageModifiers modifiers;

        public override float CalculateDamage(DamageContext ctx)
        {
            float multiplier = modifiers.GetModifier(ctx.DamageType);
            return ctx.DamageValue * multiplier;
        }
    }
    /// <summary>
    /// Permite combinar múltiplas estratégias (executadas em sequência).
    /// </summary>
    [System.Serializable]
    public class CompositeDamageStrategy : DamageStrategy
    {
        [SerializeReference]
        private List<DamageStrategy> strategies = new();

        public override float CalculateDamage(DamageContext ctx)
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