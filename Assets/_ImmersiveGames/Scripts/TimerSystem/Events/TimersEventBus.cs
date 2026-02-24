using _ImmersiveGames.NewScripts.Core.Events;
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
    /// Evento explícito para resetar o cronômetro somente quando a etapa inteira é reiniciada.
    /// Não deve ser usado para reset "in-place" de Player.
    /// </summary>
    public class GameResetStepEvent : IEvent
    {
        public readonly string reason;

        public GameResetStepEvent(string reason)
        {
            this.reason = reason;
        }

        public override string ToString()
        {
            return $"GameResetStepEvent(reason='{reason ?? "null"}')";
        }
    }
}

