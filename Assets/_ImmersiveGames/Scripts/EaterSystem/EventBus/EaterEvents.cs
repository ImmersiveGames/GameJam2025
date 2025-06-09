using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.EventBus
{
    //Todo: Esse cara também é inutil remover mais pa frente
    public class EaterConsumptionSatisfiedEvent : IEvent
    {
        public PlanetResourcesSo ConsumedResource { get; }
        public float HungerRestored { get; }
        public EaterConsumptionSatisfiedEvent(PlanetResourcesSo consumedResource, float hungerRestored)
        {
            ConsumedResource = consumedResource;
            HungerRestored = hungerRestored;
        }
    }
    
    public class EaterChesedEvent : IEvent
    {
        public Transform PlanetTransform { get; }
        public EaterChesedEvent(Transform planet)
        {
            PlanetTransform = planet;
        }
    }
    
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
}