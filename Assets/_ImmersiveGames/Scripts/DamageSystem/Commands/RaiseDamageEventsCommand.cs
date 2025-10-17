using _ImmersiveGames.Scripts.Utils.BusEventSystems;

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
            FilteredEventBus<DamageEvent>.RaiseFiltered(damageEvent, request.TargetId);

            if (!string.IsNullOrEmpty(request.AttackerId))
            {
                FilteredEventBus<DamageEvent>.RaiseFiltered(damageEvent, request.AttackerId);
            }

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

            FilteredEventBus<DamageEventReverted>.RaiseFiltered(revertedEvent, damageEvent.TargetId);

            if (!string.IsNullOrEmpty(damageEvent.AttackerId))
            {
                FilteredEventBus<DamageEventReverted>.RaiseFiltered(revertedEvent, damageEvent.AttackerId);
            }

            context.RaisedDamageEvent = null;
        }
    }
}
