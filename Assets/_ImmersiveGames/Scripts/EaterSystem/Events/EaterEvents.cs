using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.EaterSystem.Events
{
    public class EaterDeathEvent : IEvent {}

    public class DesireChangedEvent : IEvent {}

    public class EaterStarvedEvent : IEvent {}

    /// <summary>
    /// Evento global utilizado para informar mudanças no desejo atual do Eater.
    /// Permite que elementos de UI em cenas aditivas sincronizem o ícone exibido.
    /// </summary>
    public readonly struct EaterDesireInfoChangedEvent : IEvent
    {
        public EaterDesireInfoChangedEvent(EaterBehavior behavior, EaterDesireInfo info)
        {
            Behavior = behavior;
            Info = info;
        }

        public EaterBehavior Behavior { get; }

        public EaterDesireInfo Info { get; }

        public bool HasBehavior => Behavior != null;
    }

    // Novos eventos para animação

    public class EaterSatisfactionEvent : IEvent
    {
        public bool IsSatisfied { get; }

        public EaterSatisfactionEvent(bool isSatisfied)
        {
            IsSatisfied = isSatisfied;
        }
    }
}