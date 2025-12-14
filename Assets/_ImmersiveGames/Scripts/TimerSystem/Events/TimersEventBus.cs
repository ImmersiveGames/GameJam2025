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
    /// <summary>
    /// Evento explícito para resetar o cronômetro somente quando a FASE inteira é reiniciada.
    /// Não deve ser usado para reset "in-place" de Player.
    /// </summary>
    public class GamePhaseResetEvent : IEvent
    {
        public readonly string reason;

        public GamePhaseResetEvent(string reason)
        {
            this.reason = reason;
        }

        public override string ToString()
        {
            return $"GamePhaseResetEvent(reason='{reason ?? "null"}')";
        }
    }
}
