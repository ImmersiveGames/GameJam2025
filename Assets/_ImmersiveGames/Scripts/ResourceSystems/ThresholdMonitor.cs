using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ThresholdMonitor : IResourceThreshold
    {
        private readonly ResourceConfigSo _config;
        private readonly ResourceState _state;
        private IModThresholdStrategy _strategy;
        public event Action<float> OnThresholdReached;

        public ThresholdMonitor(ResourceConfigSo config, ResourceState state, IModThresholdStrategy strategy = null)
        {
            _config = config;
            _state = state;
            _strategy = strategy ?? new DefaultModThresholdStrategy();
            DebugUtility.LogVerbose<ThresholdMonitor>($"Initialize: Thresholds={string.Join(", ", _config.Thresholds)}");
        }

        public void CheckThresholds()
        {
            float percentage = _state.GetPercentage();
            var effectiveThresholds = _strategy.AdjustThresholds(new List<float>(_config.Thresholds));
            foreach (float threshold in effectiveThresholds)
            {
                if (percentage <= threshold && !_state.TriggeredThresholds.Contains(threshold))
                {
                    _state.TriggeredThresholds.Add(threshold);
                    OnThresholdReached?.Invoke(threshold);
                    DebugUtility.LogVerbose<ThresholdMonitor>($"Threshold cruzado (descendente): Threshold={threshold:F3}, Percentage={percentage:F3}");
                }
                else if (percentage > threshold && _state.TriggeredThresholds.Contains(threshold))
                {
                    _state.TriggeredThresholds.Remove(threshold);
                    OnThresholdReached?.Invoke(threshold);
                    DebugUtility.LogVerbose<ThresholdMonitor>($"Threshold cruzado (ascendente): Threshold={threshold:F3}, Percentage={percentage:F3}");
                }
            }
        }

        public void SetStrategy(IModThresholdStrategy strategy)
        {
            _strategy = strategy ?? new DefaultModThresholdStrategy();
        }
    }

    public interface IModThresholdStrategy
    {
        List<float> AdjustThresholds(List<float> thresholds);
    }

    public class DefaultModThresholdStrategy : IModThresholdStrategy
    {
        public List<float> AdjustThresholds(List<float> thresholds) => new List<float>(thresholds);
    }

    public class ModThresholdStrategy : IModThresholdStrategy
    {
        private readonly float _factor;
        public ModThresholdStrategy(float factor) => _factor = factor;
        public List<float> AdjustThresholds(List<float> thresholds) => thresholds.Select(t => t * _factor).ToList();
    }
}