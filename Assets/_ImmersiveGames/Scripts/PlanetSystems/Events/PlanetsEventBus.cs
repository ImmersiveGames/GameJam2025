using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Events
{
    public struct PlanetMarkedEvent : IEvent
    {
        public IActor PlanetActor { get; }
        public GameObject PlanetObject { get; }
        public MarkPlanet MarkPlanet { get; }

        public PlanetMarkedEvent(IActor planetActor, GameObject planetObject, MarkPlanet markPlanet)
        {
            PlanetActor = planetActor;
            PlanetObject = planetObject;
            MarkPlanet = markPlanet;
        }
    }

    public struct PlanetUnmarkedEvent : IEvent
    {
        public IActor PlanetActor { get; }
        public GameObject PlanetObject { get; }
        public MarkPlanet MarkPlanet { get; }

        public PlanetUnmarkedEvent(IActor planetActor, GameObject planetObject, MarkPlanet markPlanet)
        {
            PlanetActor = planetActor;
            PlanetObject = planetObject;
            MarkPlanet = markPlanet;
        }
    }

    public struct PlanetMarkingChangedEvent : IEvent
    {
        public IActor NewMarkedPlanet { get; }
        public IActor PreviousMarkedPlanet { get; }

        public PlanetMarkingChangedEvent(IActor newMarkedPlanet, IActor previousMarkedPlanet)
        {
            NewMarkedPlanet = newMarkedPlanet;
            PreviousMarkedPlanet = previousMarkedPlanet;
        }
    }

    public class PlanetCreatedEvent : IEvent
    {
        public IDetectable Detected { get; }

        public PlanetCreatedEvent(IDetectable detected)
        {
            Detected = detected;
        }
    }

    public class PlanetDestroyedEvent : IEvent
    {
        public IDetectable Detected { get; }
        public IActor ByActor { get; }
        public PlanetDestroyedEvent(IDetectable detected, IActor byActor)
        {
            Detected = detected;
            ByActor = byActor;
        }
    }

    public readonly struct PlanetResourceAssignedEvent : IEvent
    {
        public PlanetsMaster Planet { get; }
        public PlanetResourcesSo Resource { get; }

        public PlanetResourceAssignedEvent(PlanetsMaster planet, PlanetResourcesSo resource)
        {
            Planet = planet;
            Resource = resource;
        }
    }

    public readonly struct PlanetResourceClearedEvent : IEvent
    {
        public PlanetsMaster Planet { get; }
        public PlanetResourcesSo PreviousResource { get; }

        public PlanetResourceClearedEvent(PlanetsMaster planet, PlanetResourcesSo previousResource)
        {
            Planet = planet;
            PreviousResource = previousResource;
        }
    }
}
