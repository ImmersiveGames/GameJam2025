using System;
using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    
    public class ResourceThresholdService : IDisposable
    {
        private readonly ResourceSystem _system;
        private readonly Dictionary<ResourceType, float[]> _thresholds = new();
        private readonly Dictionary<ResourceType, float> _last = new();
        private const float Eps = 0.0001f;

        public ResourceThresholdService(ResourceSystem system)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
            Initialize();
            _system.ResourceUpdated += OnUpdate;
        }

        private void Initialize()
        {
            foreach (var (type, val) in _system.GetAll())
            {
                var inst = _system.GetInstanceConfig(type);
                _thresholds[type] = inst?.thresholdConfig?.GetNormalizedSortedThresholds()
                    ?? new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
                _last[type] = val?.GetPercentage() ?? 0f;
            }
        }

        private void OnUpdate(ResourceUpdateEvent evt)
        {
            if (!_thresholds.ContainsKey(evt.ResourceType)) return;

            float newPct = evt.NewValue?.GetPercentage() ?? 0f;
            float oldPct = _last.GetValueOrDefault(evt.ResourceType, 0f);
            if (Mathf.Abs(newPct - oldPct) < Eps) return;

            foreach (float thr in _thresholds[evt.ResourceType])
            {
                bool asc = newPct > oldPct;
                if ((asc && oldPct < thr && newPct >= thr) ||
                    (!asc && oldPct > thr && newPct <= thr))
                {
                    var e = new ResourceThresholdEvent(evt.ActorId, evt.ResourceType, thr, asc, newPct);
                    FilteredEventBus<ResourceThresholdEvent>.RaiseFiltered(e, evt.ActorId);
                }
            }

            _last[evt.ResourceType] = newPct;
        }

        public void ForceCheck()
        {
            foreach (var kv in _thresholds)
            {
                var val = _system.Get(kv.Key);
                float pct = val?.GetPercentage() ?? 0f;
                _last[kv.Key] = pct;
            }
        }

        public void Dispose()
        {
            _system.ResourceUpdated -= OnUpdate;
            _thresholds.Clear();
            _last.Clear();
        }
    }
}
