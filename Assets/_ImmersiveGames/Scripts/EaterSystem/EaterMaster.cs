using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterMaster: DetectorsMaster
    {
        [SerializeField] private EaterConfigSo config;
        public EaterConfigSo GetConfig => config;
        
        public bool InHungry { get; set; }
        public bool IsEating { get; set; }
        
        public event Action<IDetectable, SensorTypes> EventPlanetDetected; // Ação para quando um planeta é detectado
        public event Action<IDetectable> EventStartEatPlanet; // Ação para quando o Eater começa a comer um planeta
        public event Action<IDetectable> EventEndEatPlanet; // Ação para quando o Eater começa a comer um planeta
        public event Action<IDetectable, bool> EventConsumeResource; // Ação para quando um recurso é consumido

        public override void OnObjectDetected(IDetectable interactable, IDetector detector, SensorTypes sensorName)
        {
            if (!ReferenceEquals(detector, this)) return;
            base.OnObjectDetected(interactable, detector, sensorName);
            EventPlanetDetected?.Invoke(interactable, sensorName);
        }

        public override void OnPlanetLost(IDetectable interactable, IDetector detector, SensorTypes sensorName)
        {
            if (!ReferenceEquals(detector, this)) return;
            base.OnPlanetLost(interactable, detector, sensorName);
        }

        public override void Reset()
        {
            IsActive = true;
            IsEating = false;
            InHungry = false;
        }
        public void OnEventStartEatPlanet(IDetectable obj)
        {
            EventStartEatPlanet?.Invoke(obj);
        }
        public void OnEventEndEatPlanet(IDetectable obj)
        {
            EventEndEatPlanet?.Invoke(obj);
        }
        public void OnEventConsumeResource(IDetectable obj, bool desire)
        {
            EventConsumeResource?.Invoke(obj, desire);
        }
    }
}