using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.EaterSystem.Events
{
    
    public class EaterDeathEvent : IEvent {}

    public class DesireChangedEvent : IEvent {}
    public class EaterStarvedEvent : IEvent {}
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