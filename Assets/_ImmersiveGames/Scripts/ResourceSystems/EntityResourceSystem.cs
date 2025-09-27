using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [System.Serializable]
    public struct ResourceCanvasTarget
    {
        public ResourceType resourceType;
        public string targetCanvasId;
    }

    [System.Serializable]
    public struct ResourceConfig
    {
        public string name;
        public ResourceType type;
        public int initialValue;
        public int maxValue;
        public bool enabled;
    }

    [DebugLevel(DebugLevel.Verbose)]
    public class EntityResourceSystem : MonoBehaviour, IEntityResourceSystem
    {
        [SerializeField] private string entityId;

        [Header("Canvas Targeting")]
        [SerializeField] private string defaultCanvasId = "MainUI";
        [SerializeField] private List<ResourceCanvasTarget> canvasOverrides = new();
        
        [Header("Resource Configuration")]
        [SerializeField] private List<ResourceConfig> resourceConfigs = new List<ResourceConfig>
        {
            new ResourceConfig { name = nameof(ResourceType.Health),type = ResourceType.Health, initialValue = 100, maxValue = 100, enabled = true },
            new ResourceConfig { name = nameof(ResourceType.Mana),type = ResourceType.Mana, initialValue = 50, maxValue = 50, enabled = true },
            new ResourceConfig { name = nameof(ResourceType.Stamina),type = ResourceType.Stamina, initialValue = 80, maxValue = 80, enabled = true }
        };

        [Header("Debug")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool showDebugLogs = true;
        
        public float LastDamageTime { get; private set; } = -999f;
        

        private readonly Dictionary<ResourceType, IResourceValue> _resources = new();

        public string ActorId => entityId;
        public Dictionary<ResourceType, IResourceValue> Resources => new Dictionary<ResourceType, IResourceValue>(_resources);
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
        

        // ✅ INICIALIZAÇÃO ÚNICA COM CONFIG DO INSPECTOR
        [ContextMenu("Initialize Resources")]
        public void InitializeResources()
        {
            if (IsInitialized)
            {
                DebugUtility.LogWarning<EntityResourceSystem>($"Resources already initialized for {entityId}");
                return;
            }

            foreach (var config in resourceConfigs.Where(config => config.enabled))
            {
                AddResource(config.type, config.initialValue, config.maxValue);
            }

            IsInitialized = true;

            if (showDebugLogs)
            {
                DebugUtility.LogVerbose<EntityResourceSystem>($"✅ Resources initialized for {entityId}: {_resources.Count} resources");
            }
        }

        // ✅ MÉTODO PRINCIPAL - Cria recurso (SEM ResourceBindEvent)
        public void AddResource(ResourceType type, int initialValue, int maxValue)
        {
            if (_resources.ContainsKey(type))
            {
                DebugUtility.LogWarning<EntityResourceSystem>($"Resource {type} already exists for {entityId}");
                return;
            }

            var resource = new BasicResourceValue(initialValue, maxValue);
            _resources[type] = resource;
            
            // O EntityResourceBinder vai descobrir estes recursos automaticamente

            if (showDebugLogs)
            {
                DebugUtility.LogVerbose<EntityResourceSystem>($"📊 Resource added: {entityId} - {type} = {initialValue}/{maxValue}");
            }
        }
        
        public string GetTargetCanvasId(ResourceType resourceType)
        {
            var overrideConfig = canvasOverrides.FirstOrDefault(x => x.resourceType == resourceType);
            return string.IsNullOrEmpty(overrideConfig.targetCanvasId) ? defaultCanvasId : overrideConfig.targetCanvasId;
        }

        // ✅ MÉTODOS DE MODIFICAÇÃO (usam o NOVO evento)
        public void ModifyResource(ResourceType type, float delta)
        {
            // ✅ Cache para performance
            if (!_resources.TryGetValue(type, out var resource)) 
                return;

            // ✅ Cálculo otimizado
            float current = resource.GetCurrentValue();
            float max = resource.GetMaxValue();
            float newValue = Mathf.Clamp(current + delta, 0, max);
    
            // ✅ Verifica se realmente mudou
            if (Mathf.Approximately(current, newValue))
                return;

            resource.SetCurrentValue(newValue);
    
            // ✅ Só dispara evento se houve mudança
            var updateEvent = new ResourceUpdateEvent(entityId, type, resource);
            EventBus<ResourceUpdateEvent>.Raise(updateEvent);

            // ✅ Cache do LastDamageTime
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
                
            // ✅ ATUALIZAR VIA EVENT BUS (novo sistema)
            var updateEvent = new ResourceUpdateEvent(entityId, type, resource);
            EventBus<ResourceUpdateEvent>.Raise(updateEvent);
        }

        // ✅ MÉTODOS DE CONSULTA
        public IResourceValue GetResource(ResourceType type) => _resources.GetValueOrDefault(type);
        public bool HasResource(ResourceType type) => _resources.ContainsKey(type);
        public void RemoveResource(ResourceType type) => _resources.Remove(type);
        public Dictionary<ResourceType, IResourceValue> GetAllResources() => new Dictionary<ResourceType, IResourceValue>(_resources);

        // ✅ MÉTODOS DE CONVENIÊNCIA
        [ContextMenu("Take Damage 10")]
        public void TakeDamage() => ModifyResource(ResourceType.Health, -10);
        // ✅ MÉTODOS DE CONVENIÊNCIA
        [ContextMenu("Take Damage 50")]
        public void TakeDamage50() => ModifyResource(ResourceType.Health, -40);

        [ContextMenu("Heal 20")]
        public void Heal() => ModifyResource(ResourceType.Health, 20);

        [ContextMenu("Restore All Resources")]
        public void RestoreAll()
        {
            foreach (var resource in _resources.Values)
            {
                resource.SetCurrentValue(resource.GetMaxValue());
            }

            // ✅ ATUALIZAR TODAS AS UIs via NOVO evento
            foreach (var updateEvent in _resources.Select(resource => new ResourceUpdateEvent(entityId, resource.Key, resource.Value)))
            {
                EventBus<ResourceUpdateEvent>.Raise(updateEvent);
            }

            DebugUtility.LogVerbose<EntityResourceSystem>($"💫 All resources restored for {entityId}");
        }

        [ContextMenu("Debug Resources")]
        public void DebugResources()
        {
            DebugUtility.LogVerbose<EntityResourceSystem>($"📊 {entityId} Resources:");
            foreach (KeyValuePair<ResourceType, IResourceValue> resource in _resources)
            {
                DebugUtility.LogVerbose<EntityResourceSystem>($"   {resource.Key}: {resource.Value.GetCurrentValue()}/{resource.Value.GetMaxValue()}");
            }
        }
        [ContextMenu("Debug Canvas Targeting")]
        public void DebugCanvasTargeting()
        {
            DebugUtility.LogVerbose<EntityResourceSystem>($"🎯 Canvas Targeting for {entityId}:");
            DebugUtility.LogVerbose<EntityResourceSystem>($"   Default Canvas: {defaultCanvasId}");
            foreach (var resource in _resources)
            {
                string targetCanvas = GetTargetCanvasId(resource.Key);
                DebugUtility.LogVerbose<EntityResourceSystem>($"   - {resource.Key} → {targetCanvas}");
            }
        }
        public string EntityId => entityId;
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