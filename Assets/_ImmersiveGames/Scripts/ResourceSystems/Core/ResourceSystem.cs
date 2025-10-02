using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
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
        
        private IResourceLinkService _linkService;

        public event Action<ResourceUpdateEvent> ResourceUpdated;

        public float LastDamageTime { get; private set; } = -999f;

        public ResourceSystem(string entityId, IEnumerable<ResourceInstanceConfig> configs)
        {
            EntityId = string.IsNullOrEmpty(entityId) ? Guid.NewGuid().ToString() : entityId;
            
            if (!DependencyManager.Instance.TryGetGlobal(out _linkService))
            {
                _linkService = new ResourceLinkService();
            }

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

            // Verificar se há link para este recurso
            var linkConfig = _linkService.GetLink(EntityId, type);
            
            if (linkConfig != null && delta < 0) // Apenas para redução (dano)
            {
                ModifyWithLink(type, delta, linkConfig);
            }
            else
            {
                // Modificação normal
                float newValue = Mathf.Clamp(resource.GetCurrentValue() + delta, 0, resource.GetMaxValue());
                if (Mathf.Approximately(resource.GetCurrentValue(), newValue)) return;

                ApplyModification(type, delta, resource, newValue);
            }
        }
        private void ModifyWithLink(ResourceType type, float delta, ResourceLinkConfig linkConfig)
        {
            var sourceResource = _resources[type];
            var targetResource = _resources.GetValueOrDefault(linkConfig.targetResource);

            if (targetResource == null) 
            {
                // Fallback para modificação normal se o recurso alvo não existir
                float newValue = Mathf.Clamp(sourceResource.GetCurrentValue() + delta, 0, sourceResource.GetMaxValue());
                if (!Mathf.Approximately(sourceResource.GetCurrentValue(), newValue))
                {
                    ApplyModification(type, delta, sourceResource, newValue);
                }
                return;
            }

            float desiredReduction = -delta; // Converter para positivo
            float sourceAvailable = sourceResource.GetCurrentValue();
            
            // Calcular quanto pode ser reduzido do recurso fonte
            float sourceReduction = Mathf.Min(desiredReduction, sourceAvailable);
            float remainingReduction = desiredReduction - sourceReduction;

            // Aplicar redução no recurso fonte
            if (sourceReduction > 0)
            {
                float newSourceValue = Mathf.Clamp(sourceResource.GetCurrentValue() - sourceReduction, 0, sourceResource.GetMaxValue());
                if (!Mathf.Approximately(sourceResource.GetCurrentValue(), newSourceValue))
                {
                    sourceResource.SetCurrentValue(newSourceValue);
                    ResourceUpdated?.Invoke(new ResourceUpdateEvent(EntityId, type, sourceResource));
                    
                    if (delta < 0)
                    {
                        LastDamageTime = Time.time;
                    }
                }
            }

            // Aplicar redução restante no recurso alvo
            if (remainingReduction > 0)
            {
                float newTargetValue = Mathf.Clamp(targetResource.GetCurrentValue() - remainingReduction, 0, targetResource.GetMaxValue());
                if (!Mathf.Approximately(targetResource.GetCurrentValue(), newTargetValue))
                {
                    targetResource.SetCurrentValue(newTargetValue);
                    ResourceUpdated?.Invoke(new ResourceUpdateEvent(EntityId, linkConfig.targetResource, targetResource));
                    
                    LastDamageTime = Time.time;
                }

                DebugUtility.LogVerbose<ResourceSystem>($"Link transfer: {type} -> {linkConfig.targetResource}, " +
                                                       $"Source: -{sourceReduction}, Target: -{remainingReduction}");
            }
        }

        private void ApplyModification(ResourceType type, float delta, IResourceValue resource, float newValue)
        {
            switch (delta)
            {
                case > 0:
                    resource.Increase(delta);
                    break;
                case < 0:
                    resource.Decrease(-delta);
                    LastDamageTime = Time.time;
                    break;
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