using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
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
            EventBus<SensorDetectedEvent>.Register(_detectedBinding);
            EventBus<SensorLostEvent>.Register(_lostBinding);
            _detectedPlanet = null;
            _detector = null;
            _targetPosition = null;
            isActive = false;
        }

        protected override bool OnCheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            // Definir o sourceObject como o GameObject do planeta detectado ou o SpawnPoint
            sourceObject = _detectedPlanet?.Transform.gameObject ?? spawnPoint.gameObject;
            triggerPosition = null;

            if (!isActive || _detectedPlanet == null || !_detectedPlanet.IsActive || _detector == null)
            {
                DebugUtility.Log<SensorTrigger>($"Trigger inativo: IsActive={isActive}, DetectedPlanet={_detectedPlanet?.Name}, PlanetActive={_detectedPlanet?.IsActive}, Detector={_detector?.GameObject.name}", "cyan");
                return false;
            }

            _targetPosition = _detectedPlanet.Transform.position;
            triggerPosition = _targetPosition;
            return true;
        }

        private void HandleSensorDetected(SensorDetectedEvent evt)
        {
            if (evt.SensorName != _sensorType || !isRearmed || !evt.Planet.IsActive)
            {
                DebugUtility.Log<SensorTrigger>($"Evento ignorado: SensorName={evt.SensorName}, Expected={_sensorType}, IsRearmed={isRearmed}, PlanetActive={evt.Planet.IsActive}", "cyan");
                return;
            }

            // Verificar se o SpawnPoint está associado ao planeta detectado
            bool isValidPlanet = evt.Planet.Transform.gameObject == spawnPoint.gameObject ||
                                 evt.Planet.Transform.gameObject.GetComponentInParent<SpawnPoint>() == spawnPoint ||
                                 evt.Planet.Transform.gameObject.GetComponentInChildren<SpawnPoint>() == spawnPoint;

            if (!isValidPlanet)
            {
                DebugUtility.Log<SensorTrigger>($"Planeta '{evt.Planet.Name}' não está associado ao SpawnPoint '{spawnPoint.name}'. Ignorando evento.", "cyan");
                return;
            }

            if (_detectedPlanet == null || _detectedPlanet.Name != evt.Planet.Name)
            {
                _detectedPlanet = evt.Planet;
                _detector = evt.Detector;
                _targetPosition = evt.Planet.Transform.position;
                isActive = true;

                // Adicionar o detector à lista do PlanetsMaster
                if (_detectedPlanet.Transform.gameObject.TryGetComponent(out PlanetsMaster planetsMaster))
                {
                    planetsMaster.AddDetector(evt.Detector);
                    DebugUtility.Log<SensorTrigger>($"Detector '{evt.Detector.GameObject.name}' adicionado ao PlanetsMaster do planeta '{evt.Planet.Name}'.", "blue");
                }
                else
                {
                    DebugUtility.LogWarning<SensorTrigger>($"PlanetsMaster não encontrado no planeta '{evt.Planet.Name}'.", evt.Planet.Transform.gameObject);
                }

                DebugUtility.Log<SensorTrigger>($"Planeta '{evt.Planet.Name}' detectado por '{evt.Detector?.GameObject.name}' com sensor '{_sensorType}'. Ativando trigger.", "green");
            }
        }

        private void HandleSensorLost(SensorLostEvent evt)
        {
            if (evt.SensorName != _sensorType || _detectedPlanet == null || evt.Planet.Name != _detectedPlanet.Name)
            {
                DebugUtility.Log<SensorTrigger>($"Evento de perda ignorado: SensorName={evt.SensorName}, Expected={_sensorType}, DetectedPlanet={_detectedPlanet?.Name}, EventPlanet={evt.Planet.Name}", "cyan");
                return;
            }

            // Remover o detector da lista do PlanetsMaster
            if (_detectedPlanet.Transform.gameObject.TryGetComponent(out PlanetsMaster planetsMaster))
            {
                planetsMaster.RemoveDetector(evt.Detector);
                DebugUtility.Log<SensorTrigger>($"Detector '{evt.Detector.GameObject.name}' removido do PlanetsMaster do planeta '{evt.Planet.Name}'.", "yellow");
            }

            isActive = false;
            _detectedPlanet = null;
            _detector = null;
            _targetPosition = null;
            DebugUtility.Log<SensorTrigger>($"Planeta '{evt.Planet.Name}' perdido por '{_sensorType}'. Desativando trigger.", "yellow");
        }

        protected override void OnDeactivate()
        {
            // Não chamar RemoveDetector aqui, apenas em HandleSensorLost
            isActive = false;
            _detectedPlanet = null;
            _detector = null;
            _targetPosition = null;
            DebugUtility.Log<SensorTrigger>($"Trigger desativado para '{spawnPoint.name}'.", "yellow");
        }

        public override void ReArm()
        {
            base.ReArm();
            isActive = _detectedPlanet != null && _detectedPlanet.IsActive;
            DebugUtility.Log<SensorTrigger>($"Trigger rearmado para sensor '{_sensorType}'. Ativo: {isActive}.");
        }

        public IDetector GetDetector() => _detector;

        private void OnDisable()
        {
            EventBus<SensorDetectedEvent>.Unregister(_detectedBinding);
            EventBus<SensorLostEvent>.Unregister(_lostBinding);

            // Remover o detector ao desativar o componente
            if (_detectedPlanet != null && _detector != null &&
                _detectedPlanet.Transform.gameObject.TryGetComponent(out PlanetsMaster planetsMaster))
            {
                planetsMaster.RemoveDetector(_detector);
                DebugUtility.Log<SensorTrigger>($"Detector '{_detector.GameObject.name}' removido do PlanetsMaster ao desativar componente.", "yellow");
            }
        }
    }
}