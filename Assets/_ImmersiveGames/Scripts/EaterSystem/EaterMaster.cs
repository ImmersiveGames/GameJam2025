using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
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

        public event Action<IPlanetInteractable> StartEatPlanetEvent;
        public event Action<IPlanetInteractable> StopEatPlanetEvent;
        /*protected event Action<PlanetsMaster> EaterLostDetectionEvent;*/
        public event Action<IPlanetInteractable> EaterDetectionEvent;
        public override void Reset()
        {
            IsEating = false;
            IsChasing = false;
            InHungry = false;
        }
        public void OnEatPlanetEvent(IPlanetInteractable planetMaster)
        {
            IsEating = true;
            StartEatPlanetEvent?.Invoke(planetMaster);
        }
        public void OnStopEatPlanetEvent(IPlanetInteractable planetMaster)
        {
            IsEating = false;
            PlanetsManager.Instance.RemovePlanet(planetMaster);
            StopEatPlanetEvent?.Invoke(planetMaster);
        }
        /* public void OnEaterLostDetectionEvent(PlanetsMaster planetMaster)
         {
             IsEating = false;
             EaterLostDetectionEvent?.Invoke(planetMaster);
         }*/
        public void OnEaterDetectionEvent(IPlanetInteractable obj)
        {
            DebugUtility.LogVerbose<EaterMaster>($"Não Me Importo Com esse planeta: {obj.Name}", "yellow");
            EaterDetectionEvent?.Invoke(obj);
        }
    }
}