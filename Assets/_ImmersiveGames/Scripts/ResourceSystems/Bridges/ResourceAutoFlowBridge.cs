using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Warning)]
    public class ResourceAutoFlowBridge : ResourceBridgeBase
    {
        [SerializeField] private bool startPaused = true;

        private ResourceAutoFlowService _autoFlow;

        protected override bool TryInitializeService()
        {
            if (!base.TryInitializeService())
                return false;

            // Verificar se há recursos com autoflow configurado
            bool hasAutoFlowResources = CheckForAutoFlowResources();
            if (!hasAutoFlowResources)
            {
                LogVerbose("Nenhum recurso com autoflow configurado. Desativando.");
                enabled = false;
                return false;
            }

            _autoFlow = new ResourceAutoFlowService(resourceSystem, startPaused);
            LogVerbose($"✅ AutoFlowService criado com {CountAutoFlowResources()} recursos com autoflow");

            OnServiceInitialized();
            return true;
        }

        protected override void OnServiceInitialized()
        {
            // Nada específico necessário aqui para autoflow
        }

        protected override void OnServiceDispose()
        {
            _autoFlow?.Dispose();
            _autoFlow = null;
        }

        protected override void Update()
        {
            base.Update(); // Importante: chamar a base para manter a inicialização
            _autoFlow?.Process(Time.deltaTime);
        }

        private bool CheckForAutoFlowResources()
        {
            if (resourceSystem == null) return false;

            foreach (var (resourceType, _) in resourceSystem.GetAll())
            {
                var inst = resourceSystem.GetInstanceConfig(resourceType);
                if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountAutoFlowResources()
        {
            int count = 0;
            if (resourceSystem == null) return count;

            foreach (var (resourceType, _) in resourceSystem.GetAll())
            {
                var inst = resourceSystem.GetInstanceConfig(resourceType);
                if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                {
                    count++;
                }
            }

            return count;
        }

        [ContextMenu("AutoFlow: Pause")]
        private void ContextPause() 
        {
            if (!initialized) TryInitializeService();
            _autoFlow?.Pause();
            LogVerbose("AutoFlow pausado");
        }

        [ContextMenu("AutoFlow: Resume")]
        private void ContextResume() 
        {
            if (!initialized) TryInitializeService();
            _autoFlow?.Resume();
            LogVerbose("AutoFlow retomado");
        }

        [ContextMenu("AutoFlow: Toggle")]
        private void ContextToggle() 
        {
            if (!initialized) TryInitializeService();
            _autoFlow?.Toggle();
            LogVerbose($"AutoFlow alternado. Pausado: {_autoFlow?.IsPaused}");
        }

        [ContextMenu("AutoFlow: Reset Timers")]
        private void ContextReset() 
        {
            if (!initialized) TryInitializeService();
            _autoFlow?.ResetTimers();
            LogVerbose("Timers resetados");
        }

        [ContextMenu("Debug AutoFlow Status")]
        private void DebugAutoFlowStatus()
        {
            base.DebugStatus();
            
            if (resourceSystem != null)
            {
                int autoFlowCount = CountAutoFlowResources();
                LogVerbose($"Recursos com AutoFlow: {autoFlowCount}");
                
                if (autoFlowCount > 0)
                {
                    LogVerbose("Recursos com AutoFlow configurado:");
                    foreach (var (resourceType, _) in resourceSystem.GetAll())
                    {
                        var inst = resourceSystem.GetInstanceConfig(resourceType);
                        if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                        {
                            var cfg = inst.autoFlowConfig;
                            LogVerbose($"   - {resourceType}: " +
                                     $"Fill: {cfg.autoFill}, " +
                                     $"Drain: {cfg.autoDrain}, " +
                                     $"Interval: {cfg.tickInterval}s, " +
                                     $"Amount: {cfg.amountPerTick}" +
                                     $"{(cfg.usePercentage ? "%" : "")}");
                        }
                    }
                }
            }
        }
    }
}