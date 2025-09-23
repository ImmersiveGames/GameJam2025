using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ThresholdMonitorService : IResourceThreshold
    {
        private readonly ResourceConfigSo _config;
        private readonly ResourceState _state;
        private readonly IThresholdStrategy _strategy;
        private readonly string _actorId;
        private readonly GameObject _source;

        public ThresholdMonitorService(ResourceConfigSo config, ResourceState state, string actorId, GameObject source, IThresholdStrategy strategy = null)
        {
            _config = config;
            _state = state;
            _strategy = strategy;
            _actorId = actorId;
            _source = source;
            DebugUtility.LogVerbose<ThresholdMonitorService>($"Initialize: Thresholds={string.Join(", ", _config.Thresholds.Select(t => t.ToString("F2")))}");
        }

        public void CheckThresholds()
        {
            float percentage = _state.GetPercentage();
            var thresholds = _strategy != null ? _config.Thresholds.Select(t => _strategy.AdjustThreshold(t)).ToList() : _config.Thresholds.ToList();
            thresholds = thresholds.OrderBy(t => t).ToList(); // Ordenar ascendente para encontrar o menor >= percentage

            // Encontrar o menor threshold >= percentage que não foi disparado
            float? targetThreshold = thresholds.FirstOrDefault(t => percentage <= t && !_state.TriggeredThresholds.Contains(t));

            if (targetThreshold.HasValue)
            {
                _state.TriggeredThresholds.Add(targetThreshold.Value);
                EventBus<ResourceThresholdCrossedEvent>.Raise(new ResourceThresholdCrossedEvent(_config.UniqueId, _source, targetThreshold.Value, false, _actorId));
                DebugUtility.LogVerbose<ThresholdMonitorService>($"Threshold cruzado (descendente): Threshold={targetThreshold.Value:F3}, Percentage={percentage:F3}");
            }

            // Remover thresholds cruzados ascendentemente
            var toRemove = _state.TriggeredThresholds.Where(t => percentage > t).ToList();
            foreach (float threshold in toRemove)
            {
                _state.TriggeredThresholds.Remove(threshold);
                EventBus<ResourceThresholdCrossedEvent>.Raise(new ResourceThresholdCrossedEvent(_config.UniqueId, _source, threshold, true, _actorId));
                DebugUtility.LogVerbose<ThresholdMonitorService>($"Threshold cruzado (ascendente): Threshold={threshold:F3}, Percentage={percentage:F3}");
            }
        }
    }
}