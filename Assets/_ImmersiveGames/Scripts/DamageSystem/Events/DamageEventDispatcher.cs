using _ImmersiveGames.NewScripts.Core.Events;
namespace _ImmersiveGames.Scripts.DamageSystem.Events
{
    public static class DamageEventDispatcher
    {
        public static void RaiseForParticipants<TEvent>(TEvent payload, string attackerId, string targetId)
            where TEvent : struct, IEvent
        {
            if (!string.IsNullOrEmpty(targetId))
            {
                FilteredEventBus<TEvent>.RaiseFiltered(payload, targetId);
            }

            if (!string.IsNullOrEmpty(attackerId))
            {
                FilteredEventBus<TEvent>.RaiseFiltered(payload, attackerId);
            }
        }
    }
}

