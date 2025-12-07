namespace _ImmersiveGames.Scripts.DamageSystem.Commands
{
    public class DamageCooldownCommand : IDamageCommand
    {
        public bool Execute(DamageCommandContext context)
        {
            var cooldownModule = context?.CooldownModule;
            var request = context?.Request;
            if (cooldownModule == null || request == null)
            {
                return request != null;
            }

            context.PreviousCooldownTimestamp = cooldownModule.PeekCooldown(request.attackerId, request.targetId);

            bool canDeal = cooldownModule.CanDealDamage(request.attackerId, request.targetId);
            context.CooldownRegistered = canDeal;

            if (!canDeal)
            {
                context.PreviousCooldownTimestamp = null;
            }

            return canDeal;
        }

        public void Undo(DamageCommandContext context)
        {
            var cooldownModule = context?.CooldownModule;
            var request = context?.Request;
            if (cooldownModule == null || request == null || !context.CooldownRegistered)
            {
                return;
            }

            cooldownModule.RestoreCooldown(request.attackerId, request.targetId, context.PreviousCooldownTimestamp);
            context.CooldownRegistered = false;
            context.PreviousCooldownTimestamp = null;
        }
    }
}
