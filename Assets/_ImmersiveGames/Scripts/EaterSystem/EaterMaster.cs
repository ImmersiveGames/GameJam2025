using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    public sealed class EaterMaster: ActorMaster
    {
        [SerializeField] private EaterConfigSo config;
        public EaterConfigSo GetConfig => config;
        
        public bool InHungry { get; set; }
        public bool IsEating { get; set; }
        public bool IsChasing { get; set; }
        
        protected internal event Action<PlanetsMaster> StartEatPlanetEvent;
        protected internal event Action StopEatPlanetEvent;
        protected event Action<PlanetsMaster> EaterLostDetectionEvent;
        protected event Action<PlanetsMaster> EaterDetectionEvent;
        public override void Reset()
        {
            IsEating = false;
            IsChasing = false;
            InHungry = false;
        }
        public void OnEatPlanetEvent(PlanetsMaster planetMaster)
        {
            IsEating = true;
            StartEatPlanetEvent?.Invoke(planetMaster);
        }
        public void OnStopEatPlanetEvent(PlanetsMaster planetMaster)
        {
            IsEating = false;
            StopEatPlanetEvent?.Invoke();
        }
        public void OnEaterLostDetectionEvent(PlanetsMaster obj)
        {
            IsEating = false;
            EaterLostDetectionEvent?.Invoke(obj);
        }
        public void OnEaterDetectionEvent(PlanetsMaster obj)
        {
            EaterDetectionEvent?.Invoke(obj);
        }
    }
}