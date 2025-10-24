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
        public event Action<PlanetsMaster, bool, IActor> EventConsumeResource;
        public event Action<PlanetsMaster> EventEaterBite;
        public event Action<IActor> EventEaterTakeDamage;

        public EaterConfigSo Config => config;
        public EaterConfigSo GetConfig => config;

        public bool InHungry { get; internal set; }
        public bool IsEating { get; internal set; }

        public IActor EaterActor => this;

        public void OnEventStartEatPlanet(PlanetsMaster planet)
        {
            EventStartEatPlanet?.Invoke(planet);
        }

        public void OnEventEndEatPlanet(PlanetsMaster planet)
        {
            EventEndEatPlanet?.Invoke(planet);
        }

        public void OnEventConsumeResource(PlanetsMaster planet, bool desireSatisfied, IActor byActor)
        {
            EventConsumeResource?.Invoke(planet, desireSatisfied, byActor);
        }

        public void OnEventEaterBite(PlanetsMaster planet)
        {
            EventEaterBite?.Invoke(planet);
        }

        public void OnEventEaterTakeDamage(IActor byActor)
        {
            EventEaterTakeDamage?.Invoke(byActor);
        }
    }

    public interface IEaterActor
    {
        IActor EaterActor { get; }
    }
}