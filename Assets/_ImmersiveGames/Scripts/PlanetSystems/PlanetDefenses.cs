/*using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlanetDefenses : SpawnPoint
    {
        [SerializeField] private SensorTypes targetSensor; // Sensor a monitorar (e.g., PlayerDetectorSensor)
        private PlanetsMaster _planetsMaster;
        private bool _isDefenseActive;
        private DetectorsMaster _currentDetector;
        private EventBinding<SensorDetectedEvent> _sensorDetectedBinding;
        private EventBinding<SensorLostEvent> _sensorLostBinding;

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _planetsMaster);
            _sensorDetectedBinding = new EventBinding<SensorDetectedEvent>(OnSensorDetected);
            _sensorLostBinding = new EventBinding<SensorLostEvent>(OnSensorLost);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus<SensorDetectedEvent>.Register(_sensorDetectedBinding);
            EventBus<SensorLostEvent>.Register(_sensorLostBinding);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus<SensorDetectedEvent>.Unregister(_sensorDetectedBinding);
            EventBus<SensorLostEvent>.Unregister(_sensorLostBinding);
            _isDefenseActive = false;
            _currentDetector = null;
        }

        protected override void InitializeTrigger()
        {
            base.InitializeTrigger();
        }

        protected override void HandleSpawnRequest(SpawnRequestEvent evt)
        {
            if (!_isDefenseActive || _currentDetector == null || evt.Data != spawnData || evt.ObjectName != _poolKey || evt.SourceGameObject != gameObject) return;

            Vector3 direction = (_currentDetector.transform.position - transform.position).normalized;
            DebugUtility.Log<PlanetDefenses>($"[{gameObject.name}:{targetSensor}] Direção calculada: {direction}, Planeta: {transform.position}, Detector: {_currentDetector.transform.position}", "cyan", this);

            var pool = poolManager.GetPool(_poolKey);
            if (pool == null)
            {
                DebugUtility.LogError<PlanetDefenses>($"Pool '{_poolKey}' não encontrado.", this);
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, transform.position, spawnData));
                return;
            }

            int spawnCount = Mathf.Min(spawnData.SpawnCount, pool.GetAvailableCount());
            if (spawnCount == 0)
            {
                DebugUtility.Log<PlanetDefenses>($"Pool '{_poolKey}' esgotado para '{name}'.", "yellow", this);
                EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(_poolKey));
                if (useManagerLocking) _isExhausted = true;
                EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, transform.position, spawnData));
                return;
            }

            var objects = new IPoolable[spawnCount];
            for (int i = 0; i < spawnCount; i++)
            {
                objects[i] = poolManager.GetObject(_poolKey, transform.position);
                if (objects[i] == null)
                {
                    DebugUtility.LogError<PlanetDefenses>($"Falha ao obter objeto do pool '{_poolKey}'.", this);
                    EventBus<PoolExhaustedEvent>.Raise(new PoolExhaustedEvent(_poolKey));
                    if (useManagerLocking) _isExhausted = true;
                    EventBus<SpawnFailedEvent>.Raise(new SpawnFailedEvent(_poolKey, transform.position, spawnData));
                    return;
                }
            }

            _strategy.Spawn(objects, spawnData, transform.position, direction);
            spawnManager.RegisterSpawn(this);
            EventBus<SpawnTriggeredEvent>.Raise(new SpawnTriggeredEvent(_poolKey, transform.position, spawnData));
            DebugUtility.Log<PlanetDefenses>($"[{gameObject.name}:{targetSensor}] Solicitou disparo em direção a {_currentDetector.name}, Posição: {transform.position}, Alocados: {spawnCount}.", "green", this);
        }

        private void OnSensorDetected(SensorDetectedEvent evt)
        {
            if (_isDefenseActive || evt.SensorName != targetSensor || evt.Planet != _planetsMaster) return;

            _isDefenseActive = true;
            _currentDetector = evt.Detector as DetectorsMaster;
            if (_currentDetector != null)
                DebugUtility.LogVerbose<PlanetDefenses>($"[{gameObject.name}:{targetSensor}] Defesas ativadas.", "green", this);
        }

        private void OnSensorLost(SensorLostEvent evt)
        {
            if (!_isDefenseActive || evt.SensorName != targetSensor || evt.Planet != _planetsMaster || evt.Detector != _currentDetector) return;

            _isDefenseActive = false;
            _currentDetector = null;
            DebugUtility.LogVerbose<PlanetDefenses>($"[{gameObject.name}:{targetSensor}] Defesas desativadas.", "red", this);
        }

        public override void TriggerReset()
        {
            base.TriggerReset();
            _isDefenseActive = false;
            _currentDetector = null;
        }
    }
}*/