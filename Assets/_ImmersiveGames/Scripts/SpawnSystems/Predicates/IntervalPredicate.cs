using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Predicates
{
    public class IntervalPredicate : IPredicate
    {
        private readonly float _interval;
        private float _lastTriggerTime;
        private bool _isActive;

        public float Interval => _interval;
        public bool StartImmediately { get; }

        public IntervalPredicate(float interval, bool startImmediately)
        {
            _interval = interval;
            StartImmediately = startImmediately;
            _isActive = startImmediately;
            _lastTriggerTime = startImmediately ? Time.time - interval : Time.time;
        }

        public bool Evaluate()
        {
            if (!_isActive) return false;
            if (Time.time >= _lastTriggerTime + _interval)
            {
                _lastTriggerTime = Time.time;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _lastTriggerTime = Time.time - _interval;
            _isActive = StartImmediately;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }
    }
}