using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.TimerSystem.Events
{
    public class EventTimerStarted : IEvent
    {
        public float Duration { get; }

        public EventTimerStarted(float duration)
        {
            Duration = duration;
        }
    }
    public class EventTimeEnded : IEvent
    {
        public float Duration { get; }

        public EventTimeEnded(float duration)
        {
            Duration = duration;
        }
    }
}
