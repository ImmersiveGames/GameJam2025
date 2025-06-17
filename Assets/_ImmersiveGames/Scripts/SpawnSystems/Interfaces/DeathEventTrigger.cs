using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    [DebugLevel(DebugLevel.Warning)]
    public class DeathEventTrigger : ISpawnTrigger
    {
        private bool _isTriggered;
        private Vector3 _triggerPosition;
        private SpawnPoint _spawnPoint;
        private EventBinding<DeathEvent> _deathEventBinding;
        private bool _isActive = true;

        public DeathEventTrigger()
        {
            _deathEventBinding = new EventBinding<DeathEvent>(OnDeathEvent);
            EventBus<DeathEvent>.Register(_deathEventBinding);
            DebugUtility.LogVerbose<DeathEventTrigger>("Registrado no EventBus.");
        }

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint ?? throw new System.ArgumentNullException(nameof(spawnPoint));
        }

        public bool CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!_isActive || !_isTriggered) return false;
            DebugUtility.Log<DeathEventTrigger>($"Disparando SpawnRequestEvent com posição {_triggerPosition}");
            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, _triggerPosition, data));
            return true;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public void Reset()
        {
            _isTriggered = false;
            _triggerPosition = Vector3.zero;
            DebugUtility.LogVerbose<DeathEventTrigger>("Estado resetado.");
        }

        private void OnDeathEvent(DeathEvent evt)
        {
            if (_isTriggered) return;
            _isTriggered = true;
            _triggerPosition = evt.Position;
            DebugUtility.LogVerbose<DeathEventTrigger>($"Recebeu DeathEvent com posição {_triggerPosition} do objeto {evt.GameObject.name}." );
        }

        public void Dispose()
        {
            if (_deathEventBinding != null)
            {
                EventBus<DeathEvent>.Unregister(_deathEventBinding);
                DebugUtility.LogVerbose<DeathEventTrigger>("Desregistrado do EventBus.");
            }
        }
    }
}