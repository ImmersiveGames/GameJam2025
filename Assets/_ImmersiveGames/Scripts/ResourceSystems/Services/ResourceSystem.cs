using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Services
{
    /// <summary>
    /// Serviço puro para gerenciar recursos de uma entidade.
    /// </summary>
    public class ResourceSystem : IDisposable
    {
        public string EntityId { get; }
        private readonly Dictionary<ResourceType, IResourceValue> _resources = new();
        private readonly Dictionary<ResourceType, ResourceInstanceConfig> _instanceConfigs = new();
        
        private readonly IResourceLinkService _linkService;

        public event Action<ResourceUpdateEvent> ResourceUpdated;
        public event Action<ResourceChangeContext> ResourceChanging;
        public event Action<ResourceChangeContext> ResourceChanged;

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

        public void Set(ResourceType type, float value, ResourceChangeSource source = ResourceChangeSource.Manual)
        {
            if (!_resources.TryGetValue(type, out var resource)) return;
            float previous = resource.GetCurrentValue();
            float clamped = Mathf.Clamp(value, 0, resource.GetMaxValue());
            float delta = clamped - previous;
            if (Mathf.Approximately(delta, 0f)) return;

            ApplyDelta(type, resource, delta, source, false);
        }

        public void Modify(ResourceType type, float delta, ResourceChangeSource source = ResourceChangeSource.Manual)
        {
            if (!_resources.TryGetValue(type, out var resource)) return;

            // Verificar se há link para este recurso
            var linkConfig = _linkService.GetLink(EntityId, type);

            if (linkConfig != null && delta < 0) // Apenas para redução (dano)
            {
                ModifyWithLink(type, delta, linkConfig, source);
            }
            else
            {
                ApplyDelta(type, resource, delta, source, false);
            }
        }

        private void ModifyWithLink(ResourceType type, float delta, ResourceLinkConfig linkConfig, ResourceChangeSource source)
        {
            var sourceResource = _resources[type];
            var targetResource = _resources.GetValueOrDefault(linkConfig.targetResource);

            if (targetResource == null)
            {
                // Fallback para modificação normal se o recurso alvo não existir
                ApplyDelta(type, sourceResource, delta, source, false);
                return;
            }

            float desiredReduction = -delta; // Converter para positivo
            float sourceAvailable = sourceResource.GetCurrentValue();
            
            // Calcular quanto pode ser reduzido do recurso fonte
            float sourceReduction = Mathf.Min(desiredReduction, sourceAvailable);
            float remainingReduction = desiredReduction - sourceReduction;

            // Aplicar redução no recurso fonte
            bool sourceChanged = false;

            if (sourceReduction > 0)
            {
                sourceChanged = ApplyDelta(type, sourceResource, -sourceReduction, source, false);
            }

            bool targetChanged = false;

            if (remainingReduction > 0)
            {
                var targetSource = source == ResourceChangeSource.AutoFlow ? ResourceChangeSource.AutoFlow : ResourceChangeSource.Link;
                targetChanged = ApplyDelta(linkConfig.targetResource, targetResource, -remainingReduction, targetSource, true);
            }

            if (sourceChanged || targetChanged)
            {
                DebugUtility.LogVerbose<ResourceSystem>($"Link transfer: {type} -> {linkConfig.targetResource}, " +
                                                       $"Source: -{sourceReduction}, Target: -{remainingReduction}");
            }
        }

        private bool ApplyDelta(ResourceType type, IResourceValue resource, float delta, ResourceChangeSource source, bool isLinkedChange)
        {
            if (resource == null)
            {
                return false;
            }

            float previous = resource.GetCurrentValue();
            float target = Mathf.Clamp(previous + delta, 0f, resource.GetMaxValue());
            float appliedDelta = target - previous;

            if (Mathf.Approximately(appliedDelta, 0f))
            {
                return false;
            }

            var context = new ResourceChangeContext(this, type, previous, target, appliedDelta, resource.GetMaxValue(), source, isLinkedChange);
            ResourceChanging?.Invoke(context);

            resource.SetCurrentValue(target);

            if (appliedDelta < 0f)
            {
                LastDamageTime = Time.time;
            }

            var evt = new ResourceUpdateEvent(EntityId, type, resource);
            ResourceUpdated?.Invoke(evt);
            EventBus<ResourceUpdateEvent>.Raise(evt);

            ResourceChanged?.Invoke(context);
            return true;
        }

        public IResourceValue Get(ResourceType type) => _resources.GetValueOrDefault(type);
        public IReadOnlyDictionary<ResourceType, IResourceValue> GetAll() => _resources;
        // E modifique o GetInstanceConfig para debug:
        public ResourceInstanceConfig GetInstanceConfig(ResourceType resourceType)
        {
            _instanceConfigs.TryGetValue(resourceType, out var config);
            Debug.Log($"[ResourceSystem] GetInstanceConfig - {EntityId}.{resourceType}: Found={config != null}, Style={config?.slotStyle != null}");
            return config;
        }

        public void RestoreLastDamageTime(float value)
        {
            LastDamageTime = value;
        }
        /// <summary>
        /// Retorna todos os ResourceTypes registrados neste sistema
        /// </summary>
        public IEnumerable<ResourceType> GetAllRegisteredTypes()
        {
            return _resources.Keys;
        }

        /// <summary>
        /// Tenta obter o valor de um recurso específico
        /// </summary>
        public bool TryGetValue(ResourceType resourceType, out IResourceValue value)
        {
            return _resources.TryGetValue(resourceType, out value);
        }
        public void Dispose()
        {
            _resources.Clear();
            _instanceConfigs.Clear();
            ResourceUpdated = null;
        }

        [ContextMenu("🔍 Debug Instance Configs")]
        public void DebugInstanceConfigs()
        {
            Debug.Log($"[ResourceSystem] 🔍 Instance Configs for {EntityId}:");
            foreach (var kvp in _instanceConfigs)
            {
                var config = kvp.Value;
                Debug.Log($"  - {kvp.Key}: Config={config != null}, Style={config.slotStyle != null} ({config.slotStyle?.name})");
            }
        }
       
    }
}