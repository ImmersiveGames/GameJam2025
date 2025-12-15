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
                request.attackerId,
                request.targetId,
                context.CalculatedDamage,
                context.TargetRuntimeAttribute,
                request.damageType,
                request.hitPosition
            );

            context.RaisedDamageEvent = damageEvent;
            DamageEventDispatcher.RaiseForParticipants(
                damageEvent,
                request.attackerId,
                request.targetId);

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
                damageEvent.attackerId,
                damageEvent.targetId,
                damageEvent.finalDamage,
                damageEvent.runtimeAttributeType,
                damageEvent.damageType,
                damageEvent.hitPosition
            );

            DamageEventDispatcher.RaiseForParticipants(
                revertedEvent,
                damageEvent.attackerId,
                damageEvent.targetId);

            context.RaisedDamageEvent = null;
        }
    }
}
