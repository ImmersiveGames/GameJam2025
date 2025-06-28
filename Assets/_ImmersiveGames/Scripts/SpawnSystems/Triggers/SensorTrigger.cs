using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public class SensorTrigger : BaseTrigger
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

        protected override void OnInitialize()
        {
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

            if (!isActive || _detectedPlanet is not { Detectable:{IsActive:true} } || _detector == null)
            {
                DebugUtility.Log<SensorTrigger>($"Trigger inativo: IsActive={isActive}, DetectedPlanet={_detectedPlanet?.Detectable.Name}, PlanetActive={_detectedPlanet?.Detectable.IsActive}, DetectorController={_detector?.Owner.Name}", "cyan");
                return false;
            }

            _targetPosition = _detectedPlanet.Detectable.Transform.position;
            triggerPosition = _targetPosition;
            return true;
        }

        private void HandleSensorDetected(SensorDetectedEvent evt)
        {
            if (evt.SensorName != _sensorType || !isRearmed || !evt.Planet.Detectable.IsActive)
            {
                DebugUtility.Log<SensorTrigger>($"Evento ignorado: SensorName={evt.SensorName}, Expected={_sensorType}, IsRearmed={isRearmed}, PlanetActive={evt.Planet.Detectable.IsActive}", "cyan");
                return;
            }

            if (_detectedPlanet == null || _detectedPlanet.Detectable != evt.Planet.Detectable)
            {
                _detectedPlanet = evt.Planet;
                _detector = evt.Owner;
                _targetPosition = evt.Planet.Detectable.Transform.position;
                isActive = true;

                if (_detectedPlanet.Detectable.Transform.gameObject.TryGetComponent(out PlanetsMaster planetsMaster))
                {
                    planetsMaster.AddDetector(evt.Owner);
                    DebugUtility.Log<SensorTrigger>($"DetectorController '{evt.Owner.Owner.Name}' adicionado ao PlanetsMaster do planeta '{evt.Planet.Detectable.Name}'.", "blue");
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
            if (evt.SensorName != _sensorType || _detectedPlanet == null || evt.Planet.Detectable != _detectedPlanet.Detectable)
            {
                DebugUtility.Log<SensorTrigger>($"Evento de perda ignorado: SensorName={evt.SensorName}, Expected={_sensorType}, DetectedPlanet={_detectedPlanet?.Detectable.Name}, EventPlanet={evt.Planet.Detectable.Name}", "cyan");
                return;
            }

            if (_detectedPlanet.Detectable.Transform.gameObject.TryGetComponent(out PlanetsMaster planetsMaster))
            {
                planetsMaster.RemoveDetector(evt.Owner);
                DebugUtility.Log<SensorTrigger>($"DetectorController '{evt.Owner.Owner.Name}' removido do PlanetsMaster do planeta '{evt.Planet.Detectable.Name}'.", "yellow");
            }

            isActive = false;
            _detectedPlanet = null;
            _detector = null;
            _targetPosition = null;
            DebugUtility.Log<SensorTrigger>($"Planeta '{evt.Planet.Detectable.Name}' perdido por '{_sensorType}'. Desativando trigger.", "yellow");
        }

        protected override void OnDeactivate()
        {
            isActive = false;
            _detectedPlanet = null;
            _detector = null;
            _targetPosition = null;
            DebugUtility.Log<SensorTrigger>($"Trigger desativado para '{spawnPoint.name}'.", "yellow");
        }

        public override void ReArm()
        {
            base.ReArm();
            isActive = _detectedPlanet != null && _detectedPlanet.Detectable.IsActive;
            DebugUtility.Log<SensorTrigger>($"Trigger rearmado para sensor '{_sensorType}'. Ativo: {isActive}.");
        }

        private void OnDisable()
        {
            SensorFilteredEventBus.Unregister(spawnPoint);

            if (_detectedPlanet != null && _detector != null &&
                _detectedPlanet.Detectable.Transform.gameObject.TryGetComponent(out PlanetsMaster planetsMaster))
            {
                planetsMaster.RemoveDetector(_detector);
                DebugUtility.Log<SensorTrigger>($"DetectorController '{_detector.Owner.Name}' removido do PlanetsMaster ao desativar componente.", "yellow");
            }
        }
    }
}