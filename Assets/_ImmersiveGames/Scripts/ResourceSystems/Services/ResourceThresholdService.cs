using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Serviço puro que monitora thresholds por resourceType e dispara ResourceThresholdEvent via EventBus.
    /// </summary>
    public class ResourceThresholdService : IDisposable
    {
        private readonly ResourceSystemService _resourceSystem;
        private readonly Dictionary<ResourceType, float[]> _thresholds = new();
        private readonly Dictionary<ResourceType, float> _lastPercentages = new();
        private const float Epsilon = 0.001f;

        public ResourceThresholdService(ResourceSystemService resourceSystem)
        {
            _resourceSystem = resourceSystem ?? throw new ArgumentNullException(nameof(resourceSystem));
            Initialize();
            _resourceSystem.ResourceUpdated += OnResourceUpdated;
        }

        private void Initialize()
        {
            foreach (var (resourceType, resourceValue) in _resourceSystem.GetAll())
            {
                var inst = _resourceSystem.GetInstanceConfig(resourceType);
                if (inst is not { thresholdConfig: not null }) continue;
                _thresholds[resourceType] = inst.thresholdConfig != null
                    ? inst.thresholdConfig.GetNormalizedSortedThresholds()
                    : new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };

                _lastPercentages[resourceType] = resourceValue?.GetPercentage() ?? 1f;
            }
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (!_thresholds.ContainsKey(evt.ResourceType)) return;

            float newPct = evt.NewValue?.GetPercentage() ?? 0f;
            float oldPct = _lastPercentages.GetValueOrDefault(evt.ResourceType, 1f);

            if (Mathf.Abs(newPct - oldPct) < Epsilon)
            {
                _lastPercentages[evt.ResourceType] = newPct;
                return;
            }

            List<float> crossed = DetectCrossedThresholds(oldPct, newPct, _thresholds[evt.ResourceType]);
            foreach (float thr in crossed)
            {
                bool asc = newPct > oldPct;
                var thrEvent = new ResourceThresholdEvent(evt.ActorId, evt.ResourceType, thr, asc, newPct);
                EventBus<ResourceThresholdEvent>.Raise(thrEvent);
            }

            _lastPercentages[evt.ResourceType] = newPct;
        }

        private List<float> DetectCrossedThresholds(float oldVal, float newVal, float[] thresholds)
        {
            var crossed = new List<float>();
            bool ascending = newVal > oldVal;
            if (ascending)
            {
                foreach (float t in thresholds)
                    if (t > oldVal + Epsilon && t <= newVal + Epsilon)
                        crossed.Add(t);
                crossed.Sort();
            }
            else
            {
                crossed.AddRange(thresholds.Where(t => t >= newVal - Epsilon && t < oldVal - Epsilon));
                crossed.Sort((a, b) => b.CompareTo(a));
            }
            return crossed;
        }

        public void ForceCheck()
        {
            foreach (KeyValuePair<ResourceType, float[]> kv in _thresholds)
            {
                var res = _resourceSystem.Get(kv.Key);
                float newPct = res?.GetPercentage() ?? 0f;
                float oldPct = _lastPercentages.GetValueOrDefault(kv.Key, 1f);
                List<float> crossed = DetectCrossedThresholds(oldPct, newPct, kv.Value);
                foreach (float t in crossed)
                    EventBus<ResourceThresholdEvent>.Raise(new ResourceThresholdEvent(_resourceSystem.EntityId, kv.Key, t, newPct > oldPct, newPct));
                _lastPercentages[kv.Key] = newPct;
            }
        }

        public void Dispose()
        {
            _resourceSystem.ResourceUpdated -= OnResourceUpdated;
            _thresholds.Clear();
            _lastPercentages.Clear();
        }
    }
}