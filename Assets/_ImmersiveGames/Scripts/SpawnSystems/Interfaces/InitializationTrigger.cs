using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    [DebugLevel(DebugLevel.Warning)]
    public class InitializationTrigger : ISpawnTrigger
    {
        private bool _firstCallDone;
        private bool _isActive;
        private SpawnPoint _spawnPoint;

        public InitializationTrigger()
        {
            Reset();
        }

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint ?? throw new System.ArgumentNullException(nameof(spawnPoint));
        }

        public bool CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!_isActive || _firstCallDone) return false;
            _firstCallDone = true;
            DebugUtility.Log<InitializationTrigger>($"Disparando SpawnRequestEvent na inicialização em {origin}");
            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, origin, data));
            return true;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            if (active) Reset();
        }

        public void Reset()
        {
            _firstCallDone = false;
            _isActive = true;
        }
    }
}