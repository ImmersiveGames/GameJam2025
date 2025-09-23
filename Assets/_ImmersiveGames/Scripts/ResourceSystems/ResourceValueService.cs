using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceValueService : IResourceValue
    {
        private readonly ResourceState _state;
        private readonly ResourceConfigSo _config;
        private static int _instanceCount = 0;

        public ResourceValueService(ResourceConfigSo config)
        {
            _instanceCount++;
            _config = config;
            _state = new ResourceState(config.InitialValue, config.MaxValue);
            DebugUtility.LogVerbose<ResourceValueService>($"Initialize: CurrentValue={_state.CurrentValue:F2}, MaxValue={_state.MaxValue:F2}, InstanceCount={_instanceCount}");
        }

        public void Dispose()
        {
            _instanceCount--;
            DebugUtility.LogVerbose<ResourceValueService>($"Dispose: InstanceCount={_instanceCount}");
        }

        public float GetCurrentValue() => _state.CurrentValue;
        public float GetMaxValue() => _state.MaxValue;
        public float GetPercentage() => _state.GetPercentage();

        public void Increase(float amount)
        {
            float newValue = Mathf.Clamp(_state.CurrentValue + amount, 0f, _state.MaxValue);
            _state.CurrentValue = newValue;
            DebugUtility.LogVerbose<ResourceValueService>($"Increase: Amount={amount:F2}, NewValue={newValue:F2}");
        }

        public void Decrease(float amount)
        {
            float newValue = Mathf.Clamp(_state.CurrentValue - amount, 0f, _state.MaxValue);
            _state.CurrentValue = newValue;
            DebugUtility.LogVerbose<ResourceValueService>($"Decrease: Amount={amount:F2}, NewValue={newValue:F2}");
        }

        public void SetCurrentValue(float value)
        {
            _state.CurrentValue = Mathf.Clamp(value, 0f, _state.MaxValue);
            DebugUtility.LogVerbose<ResourceValueService>($"SetCurrentValue: NewValue={_state.CurrentValue:F2}");
        }
    }
}