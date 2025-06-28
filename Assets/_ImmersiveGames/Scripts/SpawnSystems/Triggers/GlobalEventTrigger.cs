using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class GlobalEventTrigger : ISpawnTrigger
    {
        private readonly string _eventName;
        private bool _isActive;
        private SpawnPoint _spawnPoint;
        private readonly EventBinding<ISpawnEvent> _eventBinding;

        public GlobalEventTrigger(EnhancedTriggerData data)
        {
            _eventName = data.GetProperty("eventName", "GlobalSpawnEvent");
            if (string.IsNullOrEmpty(_eventName))
            {
                DebugUtility.LogError<GlobalEventTrigger>("eventName não pode ser vazio. Usando 'GlobalSpawnEvent'.");
                _eventName = "GlobalSpawnEvent";
            }
            _isActive = true;
            _eventBinding = new EventBinding<ISpawnEvent>(HandleSpawnEvent);
        }

        public void ReArm()
        {
            //nope
        }
        public bool IsActive => _isActive;

        public void Initialize(SpawnPoint spawnPointRef)
        {
            _spawnPoint = spawnPointRef ?? throw new System.ArgumentNullException(nameof(spawnPointRef));
            EventBus<ISpawnEvent>.Register(_eventBinding);
            DebugUtility.Log<GlobalEventTrigger>($"Inicializado com eventName='{_eventName}' para '{_spawnPoint.name}'.", "blue", _spawnPoint);
        }

        public bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = _spawnPoint.transform.position;
            sourceObject = _spawnPoint.gameObject;
            return false;
        }

        private void HandleSpawnEvent(ISpawnEvent evt)
        {
            if (!_isActive || evt.GetType().Name != _eventName) return;

            Vector3? spawnPosition = evt.Position;
            GameObject sourceObject = evt.SourceGameObject ?? _spawnPoint.gameObject;
            if (sourceObject == null)
            {
                DebugUtility.LogWarning<GlobalEventTrigger>("sourceObject do evento é nulo. Usando SpawnPoint como source.", _spawnPoint);
            }

            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(_spawnPoint.GetPoolKey(), sourceObject, spawnPosition)); // Passa a spawnPosition
            DebugUtility.Log<GlobalEventTrigger>($"Trigger disparado por evento '{_eventName}' em '{_spawnPoint.name}' na posição {spawnPosition} com sourceObject {(sourceObject != null ? sourceObject.name : "null")}.", "green", _spawnPoint);
        }

        public void Reset()
        {
            DebugUtility.Log<GlobalEventTrigger>($"Reset não necessário para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            if (active)
                EventBus<ISpawnEvent>.Register(_eventBinding);
            else
                EventBus<ISpawnEvent>.Unregister(_eventBinding);
            DebugUtility.Log<GlobalEventTrigger>($"Trigger {(active ? "ativado" : "desativado")} para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }
    }
}