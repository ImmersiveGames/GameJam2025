using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    /// <summary>
    /// Serviço puro para gerenciar recursos de uma entidade.
    /// </summary>
    public class ResourceSystem : IDisposable
    {
        public string EntityId { get; }
        private readonly Dictionary<ResourceType, IResourceValue> _resources = new();
        private readonly Dictionary<ResourceType, ResourceInstanceConfig> _instanceConfigs = new();

        public event Action<ResourceUpdateEvent> ResourceUpdated;

        public float LastDamageTime { get; private set; } = -999f;

        public ResourceSystem(string entityId, IEnumerable<ResourceInstanceConfig> configs)
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
        public void Set(ResourceType type, float value)
        {
            if (!_resources.TryGetValue(type, out var resource)) return;
            float newValue = Mathf.Clamp(value, 0, resource.GetMaxValue());
            if (Mathf.Approximately(resource.GetCurrentValue(), newValue)) return;
            resource.SetCurrentValue(newValue);
            ResourceUpdated?.Invoke(new ResourceUpdateEvent(EntityId, type, resource));
        }
        public void Modify(ResourceType type, float delta)
        {
            if (!_resources.TryGetValue(type, out var resource)) return;
            float newValue = Mathf.Clamp(resource.GetCurrentValue() + delta, 0, resource.GetMaxValue());
            if (Mathf.Approximately(resource.GetCurrentValue(), newValue)) return;

            if (delta > 0) resource.Increase(delta);
            else if (delta < 0)
            {
                resource.Decrease(-delta);
                LastDamageTime = Time.time;
            }

            ResourceUpdated?.Invoke(new ResourceUpdateEvent(EntityId, type, resource));
        }

        public IResourceValue Get(ResourceType type) => _resources.GetValueOrDefault(type);
        public IReadOnlyDictionary<ResourceType, IResourceValue> GetAll() => _resources;
        public ResourceInstanceConfig GetInstanceConfig(ResourceType type) => _instanceConfigs.GetValueOrDefault(type);

        public void Dispose()
        {
            _resources.Clear();
            _instanceConfigs.Clear();
            ResourceUpdated = null;
        }
    }
}