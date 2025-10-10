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
                DebugUtility.LogVerbose<ResourceAutoFlowBridge>("Nenhum recurso com autoflow configurado. Desativando.");
                enabled = false;
                return false;
            }

            _autoFlow = new ResourceAutoFlowService(resourceSystem, startPaused);
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"✅ AutoFlowService criado com {CountAutoFlowResources()} recursos com autoflow");

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
        public  void ContextPause() 
        {
            if (!initialized) TryInitializeService();
            _autoFlow?.Pause();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("AutoFlow pausado");
        }
        
        public  void ContextResume() 
        {
            if (!initialized) TryInitializeService();
            _autoFlow?.Resume();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("AutoFlow retomado");
        }
        
        public  void ContextToggle() 
        {
            if (!initialized) TryInitializeService();
            _autoFlow?.Toggle();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"AutoFlow alternado. Pausado: {_autoFlow?.IsPaused}");
        }

        public void ContextReset() 
        {
            if (!initialized) TryInitializeService();
            _autoFlow?.ResetTimers();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("Timers resetados");
        }

        public void DebugAutoFlowStatus()
        {
            base.DebugStatus();
            
            if (resourceSystem != null)
            {
                int autoFlowCount = CountAutoFlowResources();
                DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"Recursos com AutoFlow: {autoFlowCount}");
                
                if (autoFlowCount > 0)
                {
                    DebugUtility.LogVerbose<ResourceAutoFlowBridge>("Recursos com AutoFlow configurado:");
                    foreach (var (resourceType, _) in resourceSystem.GetAll())
                    {
                        var inst = resourceSystem.GetInstanceConfig(resourceType);
                        if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                        {
                            var cfg = inst.autoFlowConfig;
                            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"   - {resourceType}: " +
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