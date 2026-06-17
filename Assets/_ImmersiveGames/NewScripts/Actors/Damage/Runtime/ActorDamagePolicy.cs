using System;

namespace _ImmersiveGames.NewScripts.Actors.Damage.Runtime
{
    public static class ActorDamagePolicy
    {
        public static bool TryResolveEffectiveDamage(
            ActorDamageIntent intent,
            out float effectiveDamage,
            out string reason)
        {
            effectiveDamage = 0f;
            reason = string.Empty;

            if (!intent.IsValid)
            {
                reason = "damage_intent_invalid";
                return false;
            }

            if (!IsFinite(intent.RawDamageAmount))
            {
                reason = "damage_amount_not_finite";
                return false;
            }

            if (intent.RawDamageAmount <= 0f)
            {
                reason = "damage_amount_not_positive";
                return false;
            }

            // Policy mínima: dano efetivo ainda é igual ao dano bruto.
            // Resistência, armor, cooldown, invulnerabilidade e friendly-fire ficam fora do E3A.
            effectiveDamage = intent.RawDamageAmount;
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
