using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public interface IResourceValue
    {
        float GetCurrentValue();
        float GetMaxValue();
        float GetPercentage();
        void Increase(float amount);
        void Decrease(float amount);
        void SetCurrentValue(float value);
        bool IsDepleted();
        bool IsFull();
    }

    public class BasicResourceValue : IResourceValue
    {
        private float _current;
        private float _max;

        public BasicResourceValue(float current, float max = 100f)
        {
            _max = Mathf.Max(0f, max);
            _current = Mathf.Clamp(current, 0f, _max);
        }

        public float GetCurrentValue() => _current;
        public float GetMaxValue() => _max;
        public float GetPercentage() => _max > 0f ? _current / _max : 0f;
        public bool IsDepleted() => _current <= 0f;
        public bool IsFull() => _current >= _max;
        
        public void Increase(float amount) => _current = Mathf.Clamp(_current + amount, 0f, _max);
        public void Decrease(float amount) => _current = Mathf.Clamp(_current - amount, 0f, _max);
        public void SetCurrentValue(float value) => _current = Mathf.Clamp(value, 0f, _max);
    }
}