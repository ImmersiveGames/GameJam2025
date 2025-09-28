using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;

namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Serviço puro para gerenciar recursos de uma entidade.
    /// </summary>
    public class ResourceSystemService : IDisposable
    {
        public string EntityId { get; }
        private readonly Dictionary<ResourceType, IResourceValue> _resources = new();
        private readonly Dictionary<ResourceType, ResourceInstanceConfig> _instanceConfigs = new();

        public event Action<ResourceUpdateEvent> ResourceUpdated;

        public float LastDamageTime { get; private set; } = -999f;

        public ResourceSystemService(string entityId, IEnumerable<ResourceInstanceConfig> configs)
        {
            EntityId = string.IsNullOrEmpty(entityId) ? Guid.NewGuid().ToString() : entityId;

            if (configs == null) return;

            foreach (var cfg in configs.Where(c => c != null && c.resourceDefinition != null && c.resourceDefinition.enabled))
            {
                var def = cfg.resourceDefinition;
                _resources[def.type] = new BasicResourceValue(def.initialValue, def.maxValue);
                _instanceConfigs[def.type] = cfg;
            }
        }

        public bool IsInitialized => _resources.Count > 0;
        public bool HasResource(ResourceType type) => _resources.ContainsKey(type);

        public IResourceValue Get(ResourceType type) => _resources.GetValueOrDefault(type);
        public IReadOnlyDictionary<ResourceType, IResourceValue> GetAll() => _resources;
        public ResourceInstanceConfig GetInstanceConfig(ResourceType type) => _instanceConfigs.GetValueOrDefault(type);

        public List<ResourceAutoFlowConfig> GetAutoFlowConfigs()
        {
            return _instanceConfigs.Values
                .Where(ic => ic.hasAutoFlow && ic.autoFlowConfig != null)
                .Select(ic => ic.autoFlowConfig)
                .ToList();
        }

        public void Modify(ResourceType type, float delta)
        {
            if (!_resources.TryGetValue(type, out var resource)) return;

            float current = resource.GetCurrentValue();
            float max = resource.GetMaxValue();
            float newValue = Mathf.Clamp(current + delta, 0, max);

            if (Mathf.Approximately(current, newValue)) return;

            resource.SetCurrentValue(newValue);

            if (delta < 0) LastDamageTime = Time.time;

            ResourceUpdated?.Invoke(new ResourceUpdateEvent(EntityId, type, resource));
        }

        public void Set(ResourceType type, float value)
        {
            if (!_resources.TryGetValue(type, out var resource)) return;
            resource.SetCurrentValue(Mathf.Clamp(value, 0, resource.GetMaxValue()));
            ResourceUpdated?.Invoke(new ResourceUpdateEvent(EntityId, type, resource));
        }

        public void RestoreAll()
        {
            foreach (var kv in _resources)
            {
                kv.Value.SetCurrentValue(kv.Value.GetMaxValue());
                ResourceUpdated?.Invoke(new ResourceUpdateEvent(EntityId, kv.Key, kv.Value));
            }
        }

        public void Dispose()
        {
            _resources.Clear();
            _instanceConfigs.Clear();
            ResourceUpdated = null;
        }
    }
}
