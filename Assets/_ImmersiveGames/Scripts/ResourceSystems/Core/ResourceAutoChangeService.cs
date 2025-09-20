using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceAutoChangeService
    {
        private readonly IAutoChangeStrategy _strategy;
        private readonly ValueCore _valueCore;
        private readonly ThresholdMonitor _thresholdMonitor;
        private readonly ModifierManager _modifierManager;
        private readonly ResourceConfigSo _config;
        private readonly string _uniqueId;
        private readonly GameObject _source;
        private readonly string _actorId;

        private float _timer;

        public ResourceAutoChangeService(
            IAutoChangeStrategy strategy,
            ValueCore valueCore,
            ThresholdMonitor thresholdMonitor,
            ModifierManager modifierManager,
            ResourceConfigSo config,
            string uniqueId,
            GameObject source,
            string actorId)
        {
            _strategy = strategy;
            _valueCore = valueCore;
            _thresholdMonitor = thresholdMonitor;
            _modifierManager = modifierManager;
            _config = config;
            _uniqueId = uniqueId;
            _source = source;
            _actorId = actorId;

            _timer = _config.AutoChangeDelay;
        }

        public void Tick(float deltaTime)
        {
            if (!_strategy.ShouldApply(_valueCore)) return;

            _timer += deltaTime;
            if (_timer >= _config.AutoChangeDelay)
            {
                float baseRate = _strategy.GetBaseRate(_config);
                float modifiedRate = _modifierManager.UpdateAndGetDelta(_config.AutoChangeDelay, baseRate);

                if (_strategy.IsIncreasing)
                    _valueCore.Increase(modifiedRate);
                else
                    _valueCore.Decrease(modifiedRate);

                _thresholdMonitor.CheckThresholds();

                EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(
                    _uniqueId,
                    _source,
                    _config.ResourceType,
                    _valueCore.GetPercentage(),
                    _strategy.IsIncreasing,
                    _actorId));

                DebugUtility.LogVerbose<ResourceAutoChangeService>(
                    $"{(_strategy.IsIncreasing ? "AutoFill" : "AutoDrain")}: " +
                    $"ModifiedRate={modifiedRate:F2}, " +
                    $"CurrentValue={_valueCore.GetCurrentValue():F2}, " +
                    $"Percentage={_valueCore.GetPercentage():F3}, " +
                    $"UniqueId={_uniqueId}, Source={_source.name}");

                _timer = 0f;
            }
        }

        public void Reset() => _timer = _config.AutoChangeDelay;
    }
    
    public interface IAutoChangeStrategy
    {
        bool ShouldApply(ValueCore valueCore);
        float GetBaseRate(ResourceConfigSo config);
        bool IsIncreasing { get; }
    }
}
