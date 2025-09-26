using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceAutoFlow : MonoBehaviour
    {
        [SerializeField] private List<ResourceAutoFlowConfig> flows = new();
        [SerializeField] private bool startPaused = true;

        private EntityResourceSystem _resourceSystem;
        private IActor _actor;
        private readonly Dictionary<ResourceType, float> _timers = new();
        private bool _paused;

        private void Awake()
        {
            _resourceSystem = GetComponent<EntityResourceSystem>();
            _actor = GetComponent<IActor>();

            if (_resourceSystem == null)
            {
                DebugUtility.LogError<ResourceAutoFlow>($"{name} precisa de um EntityResourceSystem.");
                enabled = false;
                return;
            }

            foreach (var config in flows.Where(config => config != null))
            {
                _timers[config.resourceType] = 0f;
            }

            _paused = startPaused;
        }

        private void Update()
        {
            if (_paused) return;

            AdvanceTimers(Time.deltaTime);
            ProcessFlows();
        }

        // -------------------------------
        // Controle externo
        // -------------------------------

        public void Pause()
        {
            _paused = true;
            DebugUtility.LogVerbose<ResourceAutoFlow>($"⏸️ AutoFlow pausado em {_resourceSystem.ActorId}");
        }

        public void Resume()
        {
            _paused = false;
            DebugUtility.LogVerbose<ResourceAutoFlow>($"▶️ AutoFlow retomado em {_resourceSystem.ActorId}");
        }

        public void Toggle()
        {
            _paused = !_paused;
            DebugUtility.LogVerbose<ResourceAutoFlow>(
                $"{(_paused ? "⏸️ Pausado" : "▶️ Retomado")} AutoFlow em {_resourceSystem.ActorId}");
        }

        public bool IsPaused => _paused;

        /// <summary>
        /// Reseta todos os timers (como se tivesse acabado de iniciar).
        /// </summary>
        public void ResetTimers()
        {
            var keys = new List<ResourceType>(_timers.Keys);
            foreach (var key in keys)
            {
                _timers[key] = 0f;
            }
            DebugUtility.LogVerbose<ResourceAutoFlow>($"🔄 Timers resetados em {_resourceSystem.ActorId}");
        }

        // -------------------------------
        // Processamento
        // -------------------------------

        private void AdvanceTimers(float deltaTime)
        {
            foreach (var key in _timers.Keys.ToList())
            {
                _timers[key] += deltaTime;
            }
        }

        private void ProcessFlows()
        {
            foreach (var config in flows)
            {
                if (config == null) continue;
                if (!_resourceSystem.HasResource(config.resourceType)) continue;

                var resource = _resourceSystem.GetResource(config.resourceType);
                if (resource == null) continue;

                // Delay de regen após dano
                if (config.autoFill && config.regenDelayAfterDamage > 0f)
                {
                    if (Time.time - _resourceSystem.LastDamageTime < config.regenDelayAfterDamage)
                        continue; // ainda no delay
                }

                // Verifica tick
                if (_timers[config.resourceType] < config.tickInterval)
                    continue;

                _timers[config.resourceType] = 0f;

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

                if (Mathf.Abs(delta) > 0.001f)
                {
                    _resourceSystem.ModifyResource(config.resourceType, delta);

                    DebugUtility.LogVerbose<ResourceAutoFlow>(
                        $"⏱️ AutoFlow {_resourceSystem.ActorId} {config.resourceType}: {(delta > 0 ? "+" : "")}{delta}");
                }
            }
        }

        // -------------------------------
        // ContextMenu (testes no inspector)
        // -------------------------------

        [ContextMenu("Pause AutoFlow")]
        private void ContextPause() => Pause();

        [ContextMenu("Resume AutoFlow")]
        private void ContextResume() => Resume();

        [ContextMenu("Toggle AutoFlow")]
        private void ContextToggle() => Toggle();

        [ContextMenu("Reset Timers")]
        private void ContextResetTimers() => ResetTimers();
    }
}
