using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.EventBus
{
    public class EaterDesireChangedEvent : IEvent
    {
        public PlanetResourcesSo DesiredResource { get; }
        public EaterDesireChangedEvent(PlanetResourcesSo desiredResource)
        {
            DesiredResource = desiredResource;
        }
    }

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

    public class EaterConsumptionUnsatisfiedEvent : IEvent
    {
        public PlanetResourcesSo ConsumedResource { get; }
        public float HungerRestored { get; }
        public EaterConsumptionUnsatisfiedEvent(PlanetResourcesSo consumedResource, float hungerRestored)
        {
            ConsumedResource = consumedResource;
            HungerRestored = hungerRestored;
        }
    }
    public class DesireUnlockedEvent :IEvent{ } // Novo evento para notificar destravamento
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
    public class DesireActivatedEvent : IEvent
    {
        // Disparado quando o sistema de desejo é ativado (fome <= 50%)
    }

    public class DesireDeactivatedEvent : IEvent
    {
        // Disparado quando o sistema de desejo é desativado (fome > 50%)
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
}