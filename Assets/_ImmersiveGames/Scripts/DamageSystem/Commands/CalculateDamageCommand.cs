namespace _ImmersiveGames.Scripts.DamageSystem.Commands
{
    public class CalculateDamageCommand : IDamageCommand
    {
        public bool Execute(DamageCommandContext context)
        {
            if (context?.Request == null)
            {
                return false;
            }

            context.PreviousCalculatedDamage = context.CalculatedDamage;

            var strategy = context.Strategy;
            if (strategy == null)
            {
                context.CalculatedDamage = context.Request.DamageValue;
                context.DamageCalculated = true;
                return true;
            }

            context.CalculatedDamage = strategy.CalculateDamage(context.Request);
            context.DamageCalculated = true;
            return true;
        }

        public void Undo(DamageCommandContext context)
        {
            if (context == null || !context.DamageCalculated)
            {
                return;
            }

            context.CalculatedDamage = context.PreviousCalculatedDamage;
            context.DamageCalculated = false;
        }
    }
}
