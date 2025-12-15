using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Core.Events;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Core
{
    /// <summary>
    /// Serviço puro para gerenciar recursos de uma entidade.
    /// </summary>
    public class RuntimeAttributeContext : IDisposable
    {
        public string EntityId { get; }
        private readonly Dictionary<RuntimeAttributeType, IRuntimeAttributeValue> _resources = new();
        private readonly Dictionary<RuntimeAttributeType, RuntimeAttributeInstanceConfig> _instanceConfigs = new();

        private readonly IRuntimeAttributeLinkService _linkService;

        public event Action<RuntimeAttributeUpdateEvent> ResourceUpdated;
        public event Action<RuntimeAttributeChangeContext> ResourceChanging;
        public event Action<RuntimeAttributeChangeContext> ResourceChanged;

        public float LastDamageTime { get; private set; } = -999f;

        public RuntimeAttributeContext(string entityId, IEnumerable<RuntimeAttributeInstanceConfig> configs)
        {
            EntityId = string.IsNullOrEmpty(entityId) ? Guid.NewGuid().ToString() : entityId;

            if (!DependencyManager.Provider.TryGetGlobal(out _linkService))
            {
                _linkService = new RuntimeAttributeLinkService();
            }

            if (configs == null) return;

            foreach (var cfg in configs.Where(c => c != null && c.runtimeAttributeDefinition != null && c.runtimeAttributeDefinition.enabled))
            {
                var def = cfg.runtimeAttributeDefinition;
                _resources[def.type] = new BasicRuntimeAttributeValue(def.initialValue, def.maxValue);
                _instanceConfigs[def.type] = cfg;
            }
        }

        public void Set(RuntimeAttributeType type, float value, RuntimeAttributeChangeSource source = RuntimeAttributeChangeSource.Manual)
        {
            if (!_resources.TryGetValue(type, out var resource)) return;

            float previous = resource.GetCurrentValue();
            float clamped = Mathf.Clamp(value, 0, resource.GetMaxValue());
            float delta = clamped - previous;

            if (Mathf.Approximately(delta, 0f))
            {
                // Não muda valor, mas pode ser útil notificar em fluxos de rebind.
                // Mantemos Set "silencioso" por padrão (comportamento existente).
                return;
            }

            ApplyDelta(type, resource, delta, source, false);
        }

        public void Modify(RuntimeAttributeType type, float delta, RuntimeAttributeChangeSource source = RuntimeAttributeChangeSource.Manual)
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

        private void ModifyWithLink(RuntimeAttributeType type, float delta, RuntimeAttributeLinkConfig linkConfig, RuntimeAttributeChangeSource source)
        {
            var sourceResource = _resources[type];
            var targetResource = _resources.GetValueOrDefault(linkConfig.targetRuntimeAttribute);

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
                var targetSource = source == RuntimeAttributeChangeSource.AutoFlow ? RuntimeAttributeChangeSource.AutoFlow : RuntimeAttributeChangeSource.Link;
                targetChanged = ApplyDelta(linkConfig.targetRuntimeAttribute, targetResource, -remainingReduction, targetSource, true);
            }

            if (sourceChanged || targetChanged)
            {
                DebugUtility.LogVerbose<RuntimeAttributeContext>($"Link transfer: {type} -> {linkConfig.targetRuntimeAttribute}, " +
                                                       $"Source: -{sourceReduction}, Target: -{remainingReduction}");
            }
        }

        private bool ApplyDelta(RuntimeAttributeType type, IRuntimeAttributeValue runtimeAttribute, float delta, RuntimeAttributeChangeSource source, bool isLinkedChange)
        {
            if (runtimeAttribute == null)
                return false;

            float previous = runtimeAttribute.GetCurrentValue();
            float target = Mathf.Clamp(previous + delta, 0f, runtimeAttribute.GetMaxValue());
            float appliedDelta = target - previous;

            if (Mathf.Approximately(appliedDelta, 0f))
                return false;

            var context = new RuntimeAttributeChangeContext(this, type, previous, target, appliedDelta, runtimeAttribute.GetMaxValue(), source, isLinkedChange);
            ResourceChanging?.Invoke(context);

            runtimeAttribute.SetCurrentValue(target);

            if (appliedDelta < 0f)
            {
                LastDamageTime = Time.time;
            }

            NotifyResource(type, runtimeAttribute, source, isLinkedChange);
            ResourceChanged?.Invoke(context);

            return true;
        }

        private void NotifyResource(RuntimeAttributeType type, IRuntimeAttributeValue runtimeAttribute, RuntimeAttributeChangeSource source, bool isLinkedChange)
        {
            var evt = new RuntimeAttributeUpdateEvent(EntityId, type, runtimeAttribute);
            ResourceUpdated?.Invoke(evt);
            EventBus<RuntimeAttributeUpdateEvent>.Raise(evt);

            // Observação: ResourceChanged é chamado por quem cria o RuntimeAttributeChangeContext.
            // Aqui só levantamos o “update” (que é o que os binds de UI normalmente consomem).
        }

        /// <summary>
        /// Reset dos recursos para os valores iniciais dos RuntimeAttributeInstanceConfig.
        /// Importante: mantém a instância do RuntimeAttributeContext (não quebra binds).
        /// Opcionalmente força notificação mesmo se o valor já estiver igual.
        /// </summary>
        public void ResetToInitialValues(RuntimeAttributeChangeSource source = RuntimeAttributeChangeSource.Manual, bool forceNotify = true)
        {
            foreach (var kv in _instanceConfigs)
            {
                var type = kv.Key;
                var cfg = kv.Value;
                if (cfg == null || cfg.runtimeAttributeDefinition == null) continue;

                if (!_resources.TryGetValue(type, out var resource) || resource == null)
                    continue;

                float previous = resource.GetCurrentValue();
                float initial = Mathf.Clamp(cfg.runtimeAttributeDefinition.initialValue, 0f, resource.GetMaxValue());

                if (!Mathf.Approximately(previous, initial))
                {
                    // Reusa pipeline padrão (com eventos e contextos)
                    float delta = initial - previous;
                    ApplyDelta(type, resource, delta, source, false);
                }
                else if (forceNotify)
                {
                    // Força update para UI/HUD rebindarem (mesmo sem delta)
                    NotifyResource(type, resource, source, false);
                }
            }
        }

        public IRuntimeAttributeValue Get(RuntimeAttributeType type) => _resources.GetValueOrDefault(type);
        public IReadOnlyDictionary<RuntimeAttributeType, IRuntimeAttributeValue> GetAll() => _resources;

        public RuntimeAttributeInstanceConfig GetInstanceConfig(RuntimeAttributeType runtimeAttributeType)
        {
            _instanceConfigs.TryGetValue(runtimeAttributeType, out var config);
            DebugUtility.LogVerbose<RuntimeAttributeContext>($"GetInstanceConfig - {EntityId}.{runtimeAttributeType}: Found={config != null}, Style={config?.slotStyle != null}");
            return config;
        }

        public void RestoreLastDamageTime(float value)
        {
            LastDamageTime = value;
        }

        public IEnumerable<RuntimeAttributeType> GetAllRegisteredTypes()
        {
            return _resources.Keys;
        }

        public bool TryGetValue(RuntimeAttributeType runtimeAttributeType, out IRuntimeAttributeValue value)
        {
            return _resources.TryGetValue(runtimeAttributeType, out value);
        }

        public void Dispose()
        {
            _resources.Clear();
            _instanceConfigs.Clear();
            ResourceUpdated = null;
            ResourceChanging = null;
            ResourceChanged = null;
        }

        [ContextMenu("🔍 Debug Instance Configs")]
        public void DebugInstanceConfigs()
        {
            DebugUtility.LogVerbose<RuntimeAttributeContext>($"🔍 Instance Configs for {EntityId}:");
            foreach (var (resourceType, resourceInstanceConfig) in _instanceConfigs)
            {
                DebugUtility.LogVerbose<RuntimeAttributeContext>(
                    $"  - {resourceType}: Config={resourceInstanceConfig != null}, Style={resourceInstanceConfig?.slotStyle != null} ({resourceInstanceConfig?.slotStyle?.name})");
            }
        }
    }
}
