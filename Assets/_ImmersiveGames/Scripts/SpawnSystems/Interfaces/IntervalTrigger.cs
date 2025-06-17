using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    [DebugLevel(DebugLevel.Warning)]
    public class IntervalTrigger : ISpawnTrigger
    {
        private readonly float _interval;
        private readonly bool _startImmediately;
        private float _lastTime;
        private bool _isActive;
        private SpawnPoint _spawnPoint;

        public IntervalTrigger(float interval, bool startImmediately)
        {
            _interval = interval;
            _startImmediately = startImmediately;
            Reset();
        }

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint ?? throw new System.ArgumentNullException(nameof(spawnPoint));
        }

        public bool CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!_isActive || Time.time < _lastTime + _interval) return false;
            _lastTime = Time.time;
            DebugUtility.Log<IntervalTrigger>($"Disparando SpawnRequestEvent com intervalo {_interval} em {origin}");
            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, origin, data));
            return true;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public void Reset()
        {
            _lastTime = _startImmediately ? Time.time - _interval : Time.time;
            _isActive = _startImmediately;
        }
    }
}