using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.EventBus
{
    
    public class EaterDeathEvent : IEvent
    {
        public Vector3 Position { get; }
        public GameObject Eater { get; }
        public EaterDeathEvent(Vector3 position, GameObject eater)
        {
            Position = position;
            Eater = eater;
        }
    }
    
    public class DesireChangedEvent : IEvent
    {
        public PlanetResourcesSo DesiredResource { get; }
        public DesireChangedEvent(PlanetResourcesSo desiredResource)
        {
            DesiredResource = desiredResource;
        }
    }
    public class EaterStarvedEvent : IEvent { }
    // Novos eventos para animação

    public class EaterSatisfactionEvent : IEvent 
    {
        public bool IsSatisfected { get; }

        public EaterSatisfactionEvent(bool isSatisfected) 
        {
            IsSatisfected = isSatisfected;
        }
    }
}