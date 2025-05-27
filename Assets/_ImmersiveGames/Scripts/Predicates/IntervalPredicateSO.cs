using UnityEngine;
namespace _ImmersiveGames.Scripts.Predicates
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Predicates/Interval")]
    public class IntervalPredicateSo : PredicateSo
    {
        public float interval = 2f;
        public bool startImmediately = true;

        private float _lastTime;

        private void OnEnable() => Reset();

        public override bool Evaluate()
        {
            if (!isActive) return false;
            if (!(Time.time >= _lastTime + interval)) return false;
            _lastTime = Time.time;
            return true;
        }

        public override void Reset()
        {
            _lastTime = startImmediately ? Time.time - interval : Time.time;
            isActive = startImmediately;
        }
    }
}