using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services
{
    
    public class RuntimeAttributeThresholdService : IDisposable
    {
        private readonly RuntimeAttributeContext _system;
        private readonly Dictionary<RuntimeAttributeType, float[]> _thresholds = new();
        private readonly Dictionary<RuntimeAttributeType, float> _last = new();
        private const float Eps = 0.0001f;

        public RuntimeAttributeThresholdService(RuntimeAttributeContext system)
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

        private void OnUpdate(RuntimeAttributeUpdateEvent evt)
        {
            if (!_thresholds.ContainsKey(evt.RuntimeAttributeType)) return;

            float newPct = evt.NewValue?.GetPercentage() ?? 0f;
            float oldPct = _last.GetValueOrDefault(evt.RuntimeAttributeType, 0f);
            if (Mathf.Abs(newPct - oldPct) < Eps) return;

            foreach (float thr in _thresholds[evt.RuntimeAttributeType])
            {
                bool asc = newPct > oldPct;
                if ((asc && oldPct < thr && newPct >= thr) ||
                    (!asc && oldPct > thr && newPct <= thr))
                {
                    var e = new RuntimeAttributeThresholdEvent(evt.ActorId, evt.RuntimeAttributeType, thr, asc, newPct);
                    FilteredEventBus<RuntimeAttributeThresholdEvent>.RaiseFiltered(e, evt.ActorId);
                }
            }

            _last[evt.RuntimeAttributeType] = newPct;
        }

        public void ForceCheck()
        {
            foreach (KeyValuePair<RuntimeAttributeType, float[]> kv in _thresholds)
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
