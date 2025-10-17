using _ImmersiveGames.Scripts.DamageSystem;

namespace _ImmersiveGames.Scripts.DamageSystem.Commands
{
    public class RaiseDamageEventsCommand : IDamageCommand
    {
        public bool Execute(DamageCommandContext context)
        {
            var request = context?.Request;
            if (request == null)
            {
                return false;
            }

            var damageEvent = new DamageEvent(
                request.AttackerId,
                request.TargetId,
                context.CalculatedDamage,
                context.TargetResource,
                request.DamageType,
                request.HitPosition
            );

            context.RaisedDamageEvent = damageEvent;
            DamageEventDispatcher.RaiseForParticipants(
                damageEvent,
                request.AttackerId,
                request.TargetId);

            return true;
        }

        public void Undo(DamageCommandContext context)
        {
            if (context == null || !context.RaisedDamageEvent.HasValue)
            {
                return;
            }

            var damageEvent = context.RaisedDamageEvent.Value;
            var revertedEvent = new DamageEventReverted(
                damageEvent.AttackerId,
                damageEvent.TargetId,
                damageEvent.FinalDamage,
                damageEvent.ResourceType,
                damageEvent.DamageType,
                damageEvent.HitPosition
            );

            DamageEventDispatcher.RaiseForParticipants(
                revertedEvent,
                damageEvent.AttackerId,
                damageEvent.TargetId);

            context.RaisedDamageEvent = null;
        }
    }
}
