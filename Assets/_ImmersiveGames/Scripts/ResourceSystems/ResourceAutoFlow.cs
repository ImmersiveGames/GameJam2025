using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public class ResourceAutoFlow : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool startPaused = true;

        private EntityResourceSystem _resourceSystem;
        private readonly Dictionary<ResourceType, float> _timers = new();
        private bool _paused;

        private void Awake()
        {
            _resourceSystem = GetComponent<EntityResourceSystem>();
            
            if (_resourceSystem == null)
            {
                DebugUtility.LogError<ResourceAutoFlow>($"{name} precisa de um EntityResourceSystem.");
                enabled = false;
                return;
            }

            if (!_resourceSystem.IsInitialized)
            {
                _resourceSystem.InitializeResources();
            }

            InitializeTimers();
            _paused = startPaused;

            DebugUtility.LogVerbose<ResourceAutoFlow>(
                $"🔄 AutoFlow inicializado para {_resourceSystem.EntityId}. Pausado: {_paused}");
        }

        private void InitializeTimers()
        {
            _timers.Clear();
            
            var allResources = _resourceSystem.GetAllResources();
            if (allResources == null) return;

            foreach (var resourceType in allResources.Keys)
            {
                var instanceConfig = _resourceSystem.GetResourceInstanceConfig(resourceType);
                if (instanceConfig != null && instanceConfig.hasAutoFlow && instanceConfig.autoFlowConfig != null)
                {
                    _timers[resourceType] = 0f;
                }
            }
        }

        private void Update()
        {
            if (_paused || _resourceSystem == null) return;

            ProcessFlows(Time.deltaTime);
        }

        public void Pause() => _paused = true;
        public void Resume() => _paused = false;
        public void Toggle() => _paused = !_paused;
        public bool IsPaused => _paused;

        public void ResetTimers()
        {
            foreach (var key in _timers.Keys.ToList())
            {
                _timers[key] = 0f;
            }
        }

        private void ProcessFlows(float deltaTime)
        {
            foreach (var resourceType in _timers.Keys.ToList())
            {
                var instanceConfig = _resourceSystem.GetResourceInstanceConfig(resourceType);
                if (instanceConfig == null || !instanceConfig.hasAutoFlow || instanceConfig.autoFlowConfig == null)
                    continue;

                var config = instanceConfig.autoFlowConfig;
                var resource = _resourceSystem.GetResource(resourceType);
                if (resource == null) continue;

                // Delay de regen após dano
                if (config.autoFill && config.regenDelayAfterDamage > 0f)
                {
                    if (Time.time - _resourceSystem.LastDamageTime < config.regenDelayAfterDamage)
                        continue;
                }

                _timers[resourceType] += deltaTime;

                if (_timers[resourceType] >= config.tickInterval)
                {
                    _timers[resourceType] = 0f;
                    ApplyAutoFlow(resourceType, config, resource);
                }
            }
        }

        private void ApplyAutoFlow(ResourceType resourceType, ResourceAutoFlowConfig config, IResourceValue resource)
        {
            float delta = CalculateDelta(config, resource);
            
            if (Mathf.Abs(delta) > 0.001f)
            {
                _resourceSystem.ModifyResource(resourceType, delta);
                
                DebugUtility.LogVerbose<ResourceAutoFlow>(
                    $"⏱️ {_resourceSystem.EntityId}.{resourceType}: " +
                    $"{resource.GetCurrentValue():F1}/{resource.GetMaxValue():F1} " +
                    $"({(delta > 0 ? "+" : "")}{delta:F2})");
            }
        }

        private float CalculateDelta(ResourceAutoFlowConfig config, IResourceValue resource)
        {
            float delta = 0;

            if (config.autoFill && resource.GetCurrentValue() < resource.GetMaxValue())
            {
                delta = config.usePercentage
                    ? resource.GetMaxValue() * config.amountPerTick / 100f
                    : config.amountPerTick;
            }

            if (config.autoDrain && resource.GetCurrentValue() > 0)
            {
                delta = config.usePercentage
                    ? -(resource.GetMaxValue() * config.amountPerTick / 100f)
                    : -config.amountPerTick;
            }

            return delta;
        }

        [ContextMenu("Pause AutoFlow")]
        private void ContextPause() => Pause();

        [ContextMenu("Resume AutoFlow")]
        private void ContextResume() => Resume();

        [ContextMenu("Toggle AutoFlow")]
        private void ContextToggle() => Toggle();

        [ContextMenu("Reset Timers")]
        private void ContextResetTimers() => ResetTimers();

        [ContextMenu("Debug AutoFlow State")]
        private void ContextDebugState()
        {
            DebugUtility.LogVerbose<ResourceAutoFlow>($"🎯 AutoFlow para {_resourceSystem.EntityId}:");
            DebugUtility.LogVerbose<ResourceAutoFlow>($"   Pausado: {_paused}");
            
            foreach (var resourceType in _timers.Keys)
            {
                var instanceConfig = _resourceSystem.GetResourceInstanceConfig(resourceType);
                if (instanceConfig?.autoFlowConfig != null)
                {
                    var config = instanceConfig.autoFlowConfig;
                    DebugUtility.LogVerbose<ResourceAutoFlow>(
                        $"   - {resourceType}: Tick={config.tickInterval}s, " +
                        $"Timer={_timers[resourceType]:F1}s, " +
                        $"Fill={config.autoFill}, Drain={config.autoDrain}");
                }
            }
        }
    }
}