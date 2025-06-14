using _ImmersiveGames.Scripts.DetectionsSystems;
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
    
    public class PlanetDestroyedEvent : IEvent
    {
        public Vector3 Position { get; }
        public GameObject Planet { get; }

        public PlanetDestroyedEvent(Vector3 position, GameObject planet)
        {
            Position = position;
            Planet = planet;
        }
    }
    
}