using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ValueCore : IResourceValue
    {
        private ResourceState _state;
        private readonly ResourceConfigSo _config;

        public ValueCore(ResourceConfigSo config)
        {
            _config = config;
            _state = new ResourceState(config.InitialValue, config.MaxValue);
            DebugUtility.LogVerbose<ValueCore>($"Initialize: CurrentValue={_state.CurrentValue}, MaxValue={_state.MaxValue}");
        }

        public void Increase(float amount)
        {
            _state.CurrentValue = Mathf.Min(_state.MaxValue, _state.CurrentValue + amount);
            DebugUtility.LogVerbose<ValueCore>($"Increase: Amount={amount:F5}, NewValue={_state.CurrentValue:F5}");
        }

        public void Decrease(float amount)
        {
            _state.CurrentValue = Mathf.Max(0, _state.CurrentValue - amount);
            DebugUtility.LogVerbose<ValueCore>($"Decrease: Amount={amount:F5}, NewValue={_state.CurrentValue:F5}");
        }

        public float GetCurrentValue() => _state.CurrentValue;
        public float GetMaxValue() => _state.MaxValue;
        public float GetPercentage() => _state.GetPercentage();
        public void SetCurrentValue(float value) => _state.CurrentValue = Mathf.Clamp(value, 0, _state.MaxValue);
        public ResourceState GetState() => _state;
    }
}