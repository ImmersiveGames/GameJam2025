using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public class SimpleSpawnTrigger : ISpawnTrigger
    {
        private SpawnPoint _spawnPoint;
        private bool _isActive = true;

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint;
        }

        public bool CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!_isActive) return false;
            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, origin, data));
            return true;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public void Reset()
        {
            _isActive = true;
        }
    }
}