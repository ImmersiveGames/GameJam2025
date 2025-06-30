using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.SpawnSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    [DebugLevel(DebugLevel.Logs)]
    public class SensorTrigger : TimedTrigger
    {
        private readonly SensorTypes _sensorType;
        private IDetectable _detectedPlanet;
        private IDetector _detector;
        private Vector3? _targetPosition;
        private EventBinding<SensorDetectedEvent> _detectedBinding;
        private EventBinding<SensorLostEvent> _lostBinding;

        public SensorTrigger(EnhancedTriggerData data) : base(data)
        {
            _sensorType = data.GetProperty("sensorType", SensorTypes.OtherSensor);
        }

        public override void Initialize(SpawnPoint spawnPointRef)
        {
            base.Initialize(spawnPointRef);
            _detectedBinding = new EventBinding<SensorDetectedEvent>(HandleSensorDetected);
            _lostBinding = new EventBinding<SensorLostEvent>(HandleSensorLost);
            SensorFilteredEventBus.RegisterDetected(_detectedBinding, spawnPoint);
            SensorFilteredEventBus.RegisterLost(_lostBinding, spawnPoint);
            _detectedPlanet = null;
            _detector = null;
            _targetPosition = null;
            isActive = false;
        }

        protected override bool OnCheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            sourceObject = _detectedPlanet?.Detectable.Transform.gameObject ?? spawnPoint.gameObject;
            triggerPosition = null;

            if (!isActive || _detectedPlanet is not { Detectable: { IsActive: true } } || _detector == null)
            {
                DebugUtility.LogVerbose<SensorTrigger>($"Trigger inativo: IsActive={isActive}, DetectedPlanet={_detectedPlanet?.Detectable.Name}, PlanetActive={_detectedPlanet?.Detectable.IsActive}, DetectorController={_detector?.Owner.Name}", "cyan");
                return false;
            }

            _targetPosition = _detectedPlanet.Detectable.Transform.position;
            triggerPosition = _targetPosition;
            return true;
        }

        private void HandleSensorDetected(SensorDetectedEvent evt)
        {
            if (evt.SensorName != _sensorType || !isRearmed || !evt.Planet.Detectable.IsActive) return;

            if (_detectedPlanet == null || _detectedPlanet.Detectable != evt.Planet.Detectable)
            {
                _detectedPlanet = evt.Planet;
                _detector = evt.Owner;
                _targetPosition = evt.Planet.Detectable.Transform.position;
                isActive = true;

                if (_detectedPlanet.Detectable.Transform.gameObject.TryGetComponent(out PlanetsMaster planetsMaster))
                {
                    planetsMaster.AddDetector(evt.Owner);
                    DebugUtility.LogVerbose<SensorTrigger>($"DetectorController '{evt.Owner.Owner.Name}' adicionado ao PlanetsMaster do planeta '{evt.Planet.Detectable.Name}'.", "blue");
                }
                else
                {
                    DebugUtility.LogWarning<SensorTrigger>($"PlanetsMaster não encontrado no planeta '{evt.Planet.Detectable.Name}'.", evt.Planet.Detectable.Transform.gameObject);
                }
                DebugUtility.Log<SensorTrigger>($"Planeta '{evt.Planet.Detectable.Name}' detectado por '{evt.Owner?.Owner.Name}' com sensor '{_sensorType}'. Ativando trigger.", "green");
            }
        }

        private void HandleSensorLost(SensorLostEvent evt)
        {
            if (evt.SensorName != _sensorType || _detectedPlanet == null || evt.Planet.Detectable != _detectedPlanet.Detectable) return;

            if (_detectedPlanet.Detectable.Transform.gameObject.TryGetComponent(out PlanetsMaster planetsMaster))
            {
                planetsMaster.RemoveDetector(evt.Owner);
                DebugUtility.LogVerbose<SensorTrigger>($"DetectorController '{evt.Owner.Owner.Name}' removido do PlanetsMaster do planeta '{evt.Planet.Detectable.Name}'.", "yellow");
            }

            isActive = false;
            _detectedPlanet = null;
            _detector = null;
            _targetPosition = null;
            DebugUtility.Log<SensorTrigger>($"Planeta '{evt.Planet.Detectable.Name}' perdido por '{_sensorType}'. Desativando trigger.", "yellow");
        }

        public override void Reset()
        {
            base.Reset();
            isActive = _detectedPlanet != null && _detectedPlanet.Detectable.IsActive;
            DebugUtility.LogVerbose<SensorTrigger>($"Resetado para '{spawnPoint?.name}'. Ativo: {isActive}.", "yellow", spawnPoint);
        }

        public override void OnDisable()
        {
            SensorFilteredEventBus.Unregister(spawnPoint);
            if (_detectedPlanet != null && _detector != null &&
                _detectedPlanet.Detectable.Transform.gameObject.TryGetComponent(out PlanetsMaster planetsMaster))
            {
                planetsMaster.RemoveDetector(_detector);
                DebugUtility.LogVerbose<SensorTrigger>($"DetectorController '{_detector.Owner.Name}' removido do PlanetsMaster ao desativar componente.", "yellow");
            }
            DebugUtility.LogVerbose<SensorTrigger>($"OnDisable chamado para '{spawnPoint?.name}'.", "yellow", spawnPoint);
        }
    }
}