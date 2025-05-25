using _ImmersiveGames.Scripts.Utils.Predicates;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public class IntervalPredicate : IPredicate
    {
        private readonly float _interval;
        private float _lastTriggerTime;
        private readonly bool _startImmediately;

        public IntervalPredicate(float interval, bool startImmediately)
        {
            _interval = interval;
            _startImmediately = startImmediately;
            _lastTriggerTime = startImmediately ? Time.time - interval : Time.time;
        }

        public bool Evaluate()
        {
            if (Time.time >= _lastTriggerTime + _interval)
            {
                _lastTriggerTime = Time.time;
                return true;
            }
            return false;
        }
    }
}