using _ImmersiveGames.NewScripts.Core.Events;

namespace _ImmersiveGames.Scripts.EaterSystem.Events
{
    /// <summary>
    /// Evento global utilizado para informar mudanças no desejo atual do Eater.
    /// Permite que elementos de UI em cenas aditivas sincronizem o ícone exibido.
    /// </summary>
    public readonly struct EaterDesireInfoChangedEvent : IEvent
    {
        public EaterDesireInfoChangedEvent(Behavior.EaterBehavior behavior, EaterDesireInfo info)
        {
            Behavior = behavior;
            Info = info;
        }

        public Behavior.EaterBehavior Behavior { get; }

        public EaterDesireInfo Info { get; }

        public bool HasBehavior => Behavior != null;
    }
}
