using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EntityResourceSystem : MonoBehaviour, IEntityResourceSystem
    {
        [SerializeField] private string entityId;
        
        [Header("Resource Instances")]
        [SerializeField] private List<ResourceInstanceConfig> resourceInstances = new();

        [Header("Debug")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool showDebugLogs = true;
        
        public float LastDamageTime { get; private set; } = -999f;

        private readonly Dictionary<ResourceType, IResourceValue> _resources = new();
        private readonly Dictionary<ResourceType, ResourceInstanceConfig> _instanceConfigs = new();

        public string EntityId => entityId;
        public bool IsInitialized { get; private set; }

        private void Start()
        {
            if (string.IsNullOrEmpty(entityId))
                entityId = gameObject.name;

            if (autoInitialize && !IsInitialized)
            {
                InitializeResources();
            }
        }

        [ContextMenu("Initialize Resources")]
        public void InitializeResources()
        {
            if (IsInitialized)
            {
                DebugUtility.LogWarning<EntityResourceSystem>($"Resources already initialized for {entityId}");
                return;
            }

            foreach (var instanceConfig in resourceInstances.Where(ic => 
                ic != null && ic.resourceDefinition != null && ic.resourceDefinition.enabled))
            {
                AddResource(instanceConfig);
            }

            IsInitialized = true;
            DebugUtility.LogVerbose<EntityResourceSystem>($"✅ Resources initialized for {entityId}: {_resources.Count} resources");
        }

        public void AddResource(ResourceInstanceConfig instanceConfig)
        {
            var def = instanceConfig.resourceDefinition;
            if (_resources.ContainsKey(def.type))
            {
                DebugUtility.LogWarning<EntityResourceSystem>($"Resource {def.type} already exists for {entityId}");
                return;
            }

            _resources[def.type] = new BasicResourceValue(def.initialValue, def.maxValue);
            _instanceConfigs[def.type] = instanceConfig;

            if (showDebugLogs)
            {
                DebugUtility.LogVerbose<EntityResourceSystem>(
                    $"📊 Resource instance added: {entityId} - {def.type} = {def.initialValue}/{def.maxValue}, " +
                    $"Canvas: {instanceConfig.targetCanvasId}, AutoFlow: {instanceConfig.hasAutoFlow}, " +
                    $"Animation: {instanceConfig.enableAnimation}");
            }
        }

        public string GetTargetCanvasId(ResourceType resourceType)
        {
            if (_instanceConfigs.TryGetValue(resourceType, out var instanceConfig))
            {
                return instanceConfig.targetCanvasId;
            }
            return "MainUI";
        }

        public ResourceInstanceConfig GetResourceInstanceConfig(ResourceType type)
        {
            return _instanceConfigs.GetValueOrDefault(type);
        }
        public List<ResourceAutoFlowConfig> GetAutoFlowConfigs()
        {
            return _instanceConfigs.Values
                .Where(ic => ic.hasAutoFlow && ic.autoFlowConfig != null)
                .Select(ic => ic.autoFlowConfig)
                .ToList();
        }

        public void ModifyResource(ResourceType type, float delta)
        {
            if (!_resources.TryGetValue(type, out var resource)) 
                return;

            float current = resource.GetCurrentValue();
            float max = resource.GetMaxValue();
            float newValue = Mathf.Clamp(current + delta, 0, max);
    
            if (Mathf.Approximately(current, newValue))
                return;

            resource.SetCurrentValue(newValue);
    
            var updateEvent = new ResourceUpdateEvent(entityId, type, resource);
            EventBus<ResourceUpdateEvent>.Raise(updateEvent);

            if (delta < 0)
                LastDamageTime = Time.time;
                
            if (showDebugLogs)
            {
                DebugUtility.LogVerbose<EntityResourceSystem>($"🔄 {entityId} {type}: {delta:+#;-#} = {resource.GetCurrentValue()}/{resource.GetMaxValue()}");
            }
        }

        public void SetResourceValue(ResourceType type, float value)
        {
            if (!_resources.TryGetValue(type, out var resource)) return;
            resource.SetCurrentValue(Mathf.Clamp(value, 0, resource.GetMaxValue()));
                
            var updateEvent = new ResourceUpdateEvent(entityId, type, resource);
            EventBus<ResourceUpdateEvent>.Raise(updateEvent);
        }

        public IResourceValue GetResource(ResourceType type) => _resources.GetValueOrDefault(type);
        public bool HasResource(ResourceType type) => _resources.ContainsKey(type);
        
        public void RemoveResource(ResourceType type) 
        { 
            _resources.Remove(type);
            _instanceConfigs.Remove(type);
        }
        
        public Dictionary<ResourceType, IResourceValue> GetAllResources() => new Dictionary<ResourceType, IResourceValue>(_resources);

        // Métodos de conveniência
        [ContextMenu("Take Damage 10")]
        public void TakeDamage() => ModifyResource(ResourceType.Health, -10);

        [ContextMenu("Heal 20")]
        public void Heal() => ModifyResource(ResourceType.Health, 20);

        [ContextMenu("Restore All Resources")]
        public void RestoreAll()
        {
            foreach (var resource in _resources.Values)
            {
                resource.SetCurrentValue(resource.GetMaxValue());
            }

            foreach (var resource in _resources)
            {
                var updateEvent = new ResourceUpdateEvent(entityId, resource.Key, resource.Value);
                EventBus<ResourceUpdateEvent>.Raise(updateEvent);
            }

            DebugUtility.LogVerbose<EntityResourceSystem>($"💫 All resources restored for {entityId}");
        }

        [ContextMenu("Debug Resources")]
        public void DebugResources()
        {
            DebugUtility.LogVerbose<EntityResourceSystem>($"📊 {entityId} Resources:");
            foreach (var resource in _resources)
            {
                DebugUtility.LogVerbose<EntityResourceSystem>($"   {resource.Key}: {resource.Value.GetCurrentValue()}/{resource.Value.GetMaxValue()}");
            }
        }

        [ContextMenu("Debug Resource Instances")]
        public void DebugResourceInstances()
        {
            DebugUtility.LogVerbose<EntityResourceSystem>($"📋 Resource Instances for {entityId}:");
            foreach (var instanceConfig in _instanceConfigs.Values)
            {
                var def = instanceConfig.resourceDefinition;
                DebugUtility.LogVerbose<EntityResourceSystem>(
                    $"   - {def.type}: {def.initialValue}/{def.maxValue}, " +
                    $"Canvas: {instanceConfig.targetCanvasId}, AutoFlow: {instanceConfig.hasAutoFlow}");
            }
        }

        public void ClearResources()
        {
            var keys = new List<ResourceType>(_resources.Keys);
            foreach (var key in keys)
            {
                RemoveResource(key);
            }
        }
    }
}