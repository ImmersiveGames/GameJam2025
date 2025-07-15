using _ImmersiveGames.Scripts.SpawnSystems.Data;
using _ImmersiveGames.Scripts.SpawnSystems.EventBus;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    public class GenericGlobalEventTrigger : ISpawnTrigger
    {
        private readonly string _eventName;
        private bool _isActive;
        private SpawnPoint _spawnPoint;
        private readonly EventBinding<GlobalGenericSpawnEvent> _eventBinding;

        public GenericGlobalEventTrigger(EnhancedTriggerData data)
        {
            _eventName = data.GetProperty("eventName", "GlobalGenericSpawnEvent");
            if (string.IsNullOrEmpty(_eventName))
            {
                DebugUtility.LogError<GenericGlobalEventTrigger>("eventName não pode ser vazio. Usando 'GlobalGenericSpawnEvent'.");
                _eventName = "GlobalGenericSpawnEvent";
            }
            _isActive = true;
            _eventBinding = new EventBinding<GlobalGenericSpawnEvent>(HandleGlobalEvent);
        }

        public void Initialize(SpawnPoint spawnPointRef)
        {
            _spawnPoint = spawnPointRef ?? throw new System.ArgumentNullException(nameof(spawnPointRef));
            EventBus<GlobalGenericSpawnEvent>.Register(_eventBinding);
            DebugUtility.Log<GenericGlobalEventTrigger>($"Inicializado com eventName='{_eventName}' para '{_spawnPoint.name}'.", "blue", _spawnPoint);
        }

        public bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = _spawnPoint.transform.position;
            sourceObject = _spawnPoint.gameObject;
            return false;
        }

        private void HandleGlobalEvent(GlobalGenericSpawnEvent evt)
        {
            if (!_isActive || evt.EventName != _eventName) return;
            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(_spawnPoint.GetPoolKey(), _spawnPoint.gameObject, _spawnPoint.transform.position));
            DebugUtility.Log<GenericGlobalEventTrigger>($"Trigger disparado por evento '{_eventName}' em '{_spawnPoint.name}' na posição {_spawnPoint.transform.position}.", "green", _spawnPoint);
        }

        public void Reset()
        {
            SetActive(true);
            DebugUtility.Log<GenericGlobalEventTrigger>($"Resetado para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }

        public void SetActive(bool active)
        {
            if (_isActive == active) return;
            _isActive = active;
            if (active)
                EventBus<GlobalGenericSpawnEvent>.Register(_eventBinding);
            else
                EventBus<GlobalGenericSpawnEvent>.Unregister(_eventBinding);
            DebugUtility.Log<GenericGlobalEventTrigger>($"Trigger {(active ? "ativado" : "desativado")} para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }

        public void OnDisable()
        {
            EventBus<GlobalGenericSpawnEvent>.Unregister(_eventBinding);
            DebugUtility.Log<GenericGlobalEventTrigger>($"OnDisable chamado para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }

        public bool IsActive => _isActive;
    }
}