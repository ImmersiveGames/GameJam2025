using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public class SimpleSpawnTrigger : ISpawnTrigger
    {
        private SpawnPoint _spawnPoint;
        private bool _isActive = true;
        private float _lastTriggerTime;
        private readonly float _interval = 1f; // Intervalo configurável

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint;
        }

        public bool CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!_isActive || Time.time < _lastTriggerTime + _interval) return false;

            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, _spawnPoint.transform.position, data, _spawnPoint.gameObject));
            _lastTriggerTime = Time.time;
            return true;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public void Reset()
        {
            _isActive = true;
            _lastTriggerTime = 0f;
        }
    }
}