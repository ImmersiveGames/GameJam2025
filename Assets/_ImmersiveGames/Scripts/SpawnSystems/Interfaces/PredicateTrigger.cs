using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    [DebugLevel(DebugLevel.Warning)]
    public class PredicateTrigger : ISpawnTrigger
    {
        private readonly PredicateData _predicate;
        private SpawnPoint _spawnPoint;
        private bool _isActive = true;

        public PredicateTrigger(PredicateData predicate)
        {
            _predicate = predicate ?? throw new System.ArgumentNullException(nameof(predicate));
        }

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint ?? throw new System.ArgumentNullException(nameof(spawnPoint));
        }

        public bool CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!_isActive || !_predicate.Evaluate()) return false;
            DebugUtility.Log<PredicateTrigger>($"Disparando SpawnRequestEvent com posição {origin}", "green");
            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, origin, data));
            return true;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public void Reset()
        {
        }
    }
}