using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.EventsBus
{
    public class PlanetMarkedEvent : IEvent
    {
        public IDetectable Detected { get; }
        public PlanetMarkedEvent(IDetectable detected) => Detected = detected;
    }

    public class PlanetUnmarkedEvent : IEvent
    {
        public IDetectable Detected { get; }
        public PlanetUnmarkedEvent(IDetectable detected) => Detected = detected;
    }
    public class PlanetCreatedEvent : IEvent
    {
        public PlanetsMaster PlanetsMaster { get; }

        public PlanetCreatedEvent(PlanetsMaster planetsMaster)
        {
            PlanetsMaster = planetsMaster;
        }
    }

    // Evento disparado quando um planeta é destruído
    public class PlanetDestroyedEvent : IEvent
    {
        public int PlanetId { get; }
        public GameObject PlanetObject { get; }

        public PlanetDestroyedEvent(int planetId, GameObject planetObject)
        {
            PlanetId = planetId;
            PlanetObject = planetObject;
        }
    }

    // Evento disparado quando um planeta morre
    public class PlanetDiedEvent: IEvent
    {
        public IDestructible Destructible { get; }
        public GameObject PlanetObject { get; }

        public PlanetDiedEvent(IDestructible destructible, GameObject planetObject)
        {
            Destructible = destructible;
            PlanetObject = planetObject;
        }
    }
    public class PlanetConsumedEvent : IEvent
    {
        public IDetectable Detected { get; }
        public PlanetConsumedEvent(IDetectable detected)
        {
            Detected = detected;
        }
    }
}