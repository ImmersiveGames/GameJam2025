using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceThresholdMonitor : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private EntityResourceSystem _resourceSystem;
        private string _actorId;
        private bool _isInitialized = false;

        // Dicionários para rastreamento
        private readonly Dictionary<ResourceType, float> _lastPercentages = new();
        private readonly Dictionary<ResourceType, float[]> _thresholds = new();
        private EventBinding<ResourceUpdateEvent> _updateBinding;

        private const float EPSILON = 0.001f;

        private void Awake()
        {
            if (enableDebugLogs)
                DebugUtility.LogVerbose<ResourceThresholdMonitor>($"🔔 Awake() em {gameObject.name}");

            _resourceSystem = GetComponent<EntityResourceSystem>();
            if (_resourceSystem == null)
            {
                DebugUtility.LogError<ResourceThresholdMonitor>($"❌ EntityResourceSystem não encontrado em {gameObject.name}");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            if (enableDebugLogs)
                DebugUtility.LogVerbose<ResourceThresholdMonitor>($"🔔 Start() em {gameObject.name}");

            if (_resourceSystem == null)
            {
                DebugUtility.LogError<ResourceThresholdMonitor>($"❌ EntityResourceSystem é nulo em Start()");
                enabled = false;
                return;
            }

            // Aguardar um frame para garantir que o ResourceSystem esteja inicializado
            StartCoroutine(InitializeAfterFrame());
        }

        private System.Collections.IEnumerator InitializeAfterFrame()
        {
            yield return null; // Esperar um frame

            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            _actorId = _resourceSystem.EntityId;

            if (enableDebugLogs)
                DebugUtility.LogVerbose<ResourceThresholdMonitor>($"🔔 Inicializando para actor: {_actorId}");

            // Verificar se há recursos com threshold monitoring
            var monitoredResources = new List<ResourceType>();
            foreach (var resourceType in _resourceSystem.GetAllResources().Keys)
            {
                var instanceConfig = _resourceSystem.GetResourceInstanceConfig(resourceType);
                if (instanceConfig != null && instanceConfig.enableThresholdMonitoring)
                {
                    monitoredResources.Add(resourceType);
                }
            }

            if (monitoredResources.Count == 0)
            {
                DebugUtility.LogWarning<ResourceThresholdMonitor>($"⚠️ Nenhum recurso com threshold monitoring para {_actorId}");
                enabled = false;
                return;
            }

            if (enableDebugLogs)
                DebugUtility.LogVerbose<ResourceThresholdMonitor>($"🔔 {monitoredResources.Count} recursos para monitorar: {string.Join(", ", monitoredResources)}");

            // Configurar thresholds para cada recurso monitorado
            foreach (var resourceType in monitoredResources)
            {
                ConfigureResourceMonitoring(resourceType);
            }

            // Registrar para eventos
            _updateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
            EventBus<ResourceUpdateEvent>.Register(_updateBinding);

            _isInitialized = true;

            if (enableDebugLogs)
                DebugUtility.LogVerbose<ResourceThresholdMonitor>($"✅ ThresholdMonitor inicializado para {_actorId} com {_thresholds.Count} recursos");

            // Log do estado inicial
            LogCurrentState("Estado inicial");
        }

        private void ConfigureResourceMonitoring(ResourceType resourceType)
        {
            var instanceConfig = _resourceSystem.GetResourceInstanceConfig(resourceType);
            if (instanceConfig == null) return;

            // Configurar thresholds
            if (instanceConfig.thresholdConfig != null)
            {
                _thresholds[resourceType] = instanceConfig.thresholdConfig.GetNormalizedSortedThresholds();
            }
            else
            {
                // Thresholds padrão
                _thresholds[resourceType] = new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
            }

            // Inicializar último valor conhecido
            var resource = _resourceSystem.GetResource(resourceType);
            _lastPercentages[resourceType] = resource?.GetPercentage() ?? 1f;

            if (enableDebugLogs)
            {
                DebugUtility.LogVerbose<ResourceThresholdMonitor>(
                    $"📊 Configurado {resourceType} com thresholds: {string.Join(", ", _thresholds[resourceType].Select(t => t.ToString("P0")))}");
            }
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (!_isInitialized) return;

            // Filtrar por actor
            if (!string.Equals(evt.ActorId, _actorId, StringComparison.OrdinalIgnoreCase))
                return;

            // Verificar se este recurso está sendo monitorado
            if (!_thresholds.ContainsKey(evt.ResourceType))
                return;

            float newPercentage = evt.NewValue?.GetPercentage() ?? 0f;
            float oldPercentage = _lastPercentages.GetValueOrDefault(evt.ResourceType, 1f);

            // Verificar se há mudança significativa
            if (Mathf.Abs(newPercentage - oldPercentage) < EPSILON)
            {
                _lastPercentages[evt.ResourceType] = newPercentage;
                return;
            }

            // Detectar thresholds cruzados
            var crossedThresholds = DetectCrossedThresholds(oldPercentage, newPercentage, _thresholds[evt.ResourceType]);

            // Disparar eventos para cada threshold cruzado
            foreach (var threshold in crossedThresholds)
            {
                bool isAscending = newPercentage > oldPercentage;
                DispatchThresholdEvent(evt.ActorId, evt.ResourceType, threshold, isAscending, newPercentage);
            }

            _lastPercentages[evt.ResourceType] = newPercentage;
        }

        private List<float> DetectCrossedThresholds(float oldValue, float newValue, float[] thresholds)
        {
            var crossed = new List<float>();
            bool ascending = newValue > oldValue;

            if (ascending)
            {
                // Para valores ascendentes: thresholds entre oldValue (exclusive) e newValue (inclusive)
                foreach (var threshold in thresholds)
                {
                    if (threshold > oldValue + EPSILON && threshold <= newValue + EPSILON)
                    {
                        crossed.Add(threshold);
                    }
                }
                crossed.Sort(); // Ordem crescente
            }
            else
            {
                // Para valores descendentes: thresholds entre newValue (inclusive) e oldValue (exclusive)
                foreach (var threshold in thresholds)
                {
                    if (threshold >= newValue - EPSILON && threshold < oldValue - EPSILON)
                    {
                        crossed.Add(threshold);
                    }
                }
                crossed.Sort((a, b) => b.CompareTo(a)); // Ordem decrescente
            }

            return crossed;
        }

        private void DispatchThresholdEvent(string actorId, ResourceType resourceType, float threshold, bool isAscending, float currentPercentage)
        {
            var thresholdEvent = new ResourceThresholdEvent(actorId, resourceType, threshold, isAscending, currentPercentage);
            EventBus<ResourceThresholdEvent>.Raise(thresholdEvent);

            if (enableDebugLogs)
            {
                DebugUtility.LogVerbose<ResourceThresholdMonitor>(
                    $"⚡ THRESHOLD: {actorId}.{resourceType} → {threshold:P0} ({(isAscending ? "↑" : "↓")}) - Atual: {currentPercentage:P2}");
            }
        }

        private void LogCurrentState(string context)
        {
            if (!enableDebugLogs) return;

            DebugUtility.LogVerbose<ResourceThresholdMonitor>($"📊 {context} para {_actorId}:");
            foreach (var kvp in _thresholds)
            {
                var resource = _resourceSystem.GetResource(kvp.Key);
                float currentPct = resource?.GetPercentage() ?? 0f;
                DebugUtility.LogVerbose<ResourceThresholdMonitor>(
                    $"   - {kvp.Key}: {currentPct:P2} | Thresholds: {string.Join(", ", kvp.Value.Select(t => t.ToString("P0")))}");
            }
        }

        private void OnDestroy()
        {
            if (_updateBinding != null)
            {
                EventBus<ResourceUpdateEvent>.Unregister(_updateBinding);
                if (enableDebugLogs)
                    DebugUtility.LogVerbose<ResourceThresholdMonitor>($"🔔 ThresholdMonitor destruído para {_actorId}");
            }
        }

        [ContextMenu("Debug State")]
        private void DebugState()
        {
            LogCurrentState("Debug manual");
        }

        [ContextMenu("Force Threshold Check")]
        private void ForceThresholdCheck()
        {
            if (!_isInitialized) return;

            if (enableDebugLogs)
                DebugUtility.LogVerbose<ResourceThresholdMonitor>("🔔 Forçando verificação de thresholds");

            foreach (var resourceType in _thresholds.Keys)
            {
                var resource = _resourceSystem.GetResource(resourceType);
                if (resource != null)
                {
                    float currentPct = resource.GetPercentage();
                    var evt = new ResourceUpdateEvent(_actorId, resourceType, resource);
                    OnResourceUpdated(evt);
                }
            }
        }

        [ContextMenu("Test Health Thresholds")]
        private void TestHealthThresholds()
        {
            if (!_isInitialized) return;

            if (enableDebugLogs)
                DebugUtility.LogVerbose<ResourceThresholdMonitor>($"🧪 Testando thresholds do Health");

            if (_resourceSystem.HasResource(ResourceType.Health))
            {
                // Testar uma mudança grande para cruzar múltiplos thresholds
                _resourceSystem.SetResourceValue(ResourceType.Health, 30f);
                
                if (enableDebugLogs)
                    DebugUtility.LogVerbose<ResourceThresholdMonitor>($"🧪 Saúde ajustada para 30%");
            }
        }
    }
}