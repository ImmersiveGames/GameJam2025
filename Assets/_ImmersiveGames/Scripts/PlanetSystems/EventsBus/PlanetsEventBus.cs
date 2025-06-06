﻿using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.EventsBus
{
    public class PlanetMarkedEvent : IEvent
    {
        public IPlanetInteractable PlanetMaster { get; }
        public PlanetMarkedEvent(IPlanetInteractable planetMaster) => PlanetMaster = planetMaster;
    }

    public class PlanetUnmarkedEvent : IEvent
    {
        public IPlanetInteractable PlanetMaster { get; }
        public PlanetUnmarkedEvent(IPlanetInteractable planetMaster) => PlanetMaster = planetMaster;
    }
    public class PlanetCreatedEvent : IEvent
    {
        public int PlanetId { get; }
        public PlanetData Data { get; }
        public PlanetResourcesSo Resources { get; }
        public GameObject PlanetObject { get; }

        public PlanetCreatedEvent(int planetId, PlanetData data, PlanetResourcesSo resources, GameObject planetObject)
        {
            PlanetId = planetId;
            Data = data;
            Resources = resources;
            PlanetObject = planetObject;
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
        public IPlanetInteractable PlanetMaster { get; }
        public PlanetConsumedEvent(IPlanetInteractable planetMaster)
        {
            PlanetMaster = planetMaster;
        }
    }
}