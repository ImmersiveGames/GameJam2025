using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterMaster : ActorMaster, IEaterActor
    {
        [Header("Configuração")]
        [SerializeField] private EaterConfigSo config;

        public event Action<PlanetsMaster> EventStartEatPlanet;
        public event Action<PlanetsMaster> EventEndEatPlanet;
        public event Action<PlanetsMaster> EventEaterBite;

        public EaterConfigSo Config => config;

        public IActor EaterActor => this;

        public void OnEventStartEatPlanet(PlanetsMaster planet)
        {
            EventStartEatPlanet?.Invoke(planet);
        }

        public void OnEventEndEatPlanet(PlanetsMaster planet)
        {
            EventEndEatPlanet?.Invoke(planet);
        }

        public void OnEventEaterBite(PlanetsMaster planet)
        {
            EventEaterBite?.Invoke(planet);
        }
    }

    public interface IEaterActor
    {
        IActor EaterActor { get; }
    }
}