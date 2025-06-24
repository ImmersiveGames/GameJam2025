using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class SensorTrigger : BaseTrigger
    {
        private readonly SensorTypes _sensorType;
        private IDetectable _detectedPlanet;
        private Vector3? _targetPosition;
        private EventBinding<SensorDetectedEvent> _detectedBinding;
        private EventBinding<SensorLostEvent> _lostBinding;

        public SensorTrigger(EnhancedTriggerData data) : base(data)
        {
            _sensorType = data.GetProperty("sensorType", SensorTypes.OtherSensor);
        }

        protected override void OnInitialize()
        {
            _detectedBinding = new EventBinding<SensorDetectedEvent>(HandleSensorDetected);
            _lostBinding = new EventBinding<SensorLostEvent>(HandleSensorLost);
            EventBus<SensorDetectedEvent>.Register(_detectedBinding);
            EventBus<SensorLostEvent>.Register(_lostBinding);
            _detectedPlanet = null;
            _targetPosition = null;
            _isActive = false;
        }

        protected override bool OnCheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            sourceObject = _spawnPoint.gameObject;
            triggerPosition = null;

            if (!_isActive || _detectedPlanet == null || !_detectedPlanet.IsActive)
            {
                _detectedPlanet = null;
                _targetPosition = null;
                return false;
            }

            _targetPosition = _detectedPlanet.Transform.position;
            triggerPosition = _targetPosition;
            return true;
        }

        private void HandleSensorDetected(SensorDetectedEvent evt)
        {
            if (evt.SensorName != _sensorType || !_isRearmed || !evt.Planet.IsActive || 
                (_detectedPlanet != null && _detectedPlanet.Name == evt.Planet.Name) || _isActive) // Adiciona verificação de _isActive
                return;

            _detectedPlanet = evt.Planet;
            _targetPosition = evt.Planet.Transform.position;
            _isActive = true;
        }

        private void HandleSensorLost(SensorLostEvent evt)
        {
            if (evt.SensorName != _sensorType || _detectedPlanet == null || evt.Planet.Name != _detectedPlanet.Name)
                return;

            _isActive = false;
            _detectedPlanet = null;
            _targetPosition = null;
        }

        protected override void OnDeactivate()
        {
            _detectedPlanet = null;
            _targetPosition = null;
        }

        public override void ReArm()
        {
            base.ReArm();
            _detectedPlanet = null;
            _targetPosition = null;
        }

        protected void OnDisable()
        {
            EventBus<SensorDetectedEvent>.Unregister(_detectedBinding);
            EventBus<SensorLostEvent>.Unregister(_lostBinding);
        }
    }
}