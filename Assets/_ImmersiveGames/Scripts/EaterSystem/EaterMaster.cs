using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
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
        public bool IsChasing { get; set; }
        
        public event Action<IDetectable,SensorTypes> EventEaterDetect = delegate { };
        public event Action<IDetectable, SensorTypes> EventEaterLostDetections = delegate { };
        
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding; // Binding para evento de marcação
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding; // Binding para evento de desmarcação

        private void OnEnable()
        {
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnPlanetMarked);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);
            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnPlanetUnmarked);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }
        
        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        public override void OnObjectDetected(IDetectable interactable, IDetector detector, SensorTypes sensorName)
        {
            if (!ReferenceEquals(detector, this)) return;
            base.OnObjectDetected(interactable, detector, sensorName);
            
            OnEventEaterDetect(interactable, sensorName);
        }

        public override void OnPlanetLost(IDetectable interactable, IDetector detector, SensorTypes sensorName)
        {
            if (!ReferenceEquals(detector, this)) return;
            base.OnPlanetLost(interactable, detector, sensorName);
   
            OnEventEaterLostDetections(interactable, sensorName);
        }

        public override void Reset()
        {
            IsActive = true;
            IsEating = false;
            IsChasing = false;
            InHungry = false;
        }
        private void OnEventEaterDetect(IDetectable obj, SensorTypes sensor)
        {
            DebugUtility.LogVerbose<EaterMaster>($"Eater detectou: {obj.Name} com sensor {sensor}, executar ações internas", "green");
            EventEaterDetect(obj, sensor);
        }
        private void OnEventEaterLostDetections(IDetectable obj, SensorTypes sensor)
        {
            DebugUtility.LogVerbose<EaterMaster>($"Eater perdeu detecções de: {obj.Name} com sensor {sensor}, executar ações internas", "red");
            EventEaterLostDetections(obj,sensor);
        }
        private void OnPlanetUnmarked(PlanetUnmarkedEvent obj)
        {
            DebugUtility.LogVerbose<EaterMaster>($"O Eater reconheceu que um planeta foi desmarcado: {obj.PlanetMaster.Name}", "red");
        }
        private void OnPlanetMarked(PlanetMarkedEvent obj)
        {
            DebugUtility.LogVerbose<EaterMaster>($"O Eater reconheceu que um planeta foi marcado: {obj.PlanetMaster.Name}", "green");
        }
    }
}