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

            // CORREÇÃO: Verificação mais robusta
            if (resourceSystem == null)
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>("ResourceSystem é null na inicialização do AutoFlow");
                return false;
            }

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

            return true;
        }

        protected override void OnServiceInitialized()
        {
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"🚀 AutoFlowService inicializado para {Actor.ActorId}");
            
            // Iniciar processamento se não estiver pausado
            if (!startPaused)
            {
                _autoFlow?.Resume();
            }
        }

        protected override void OnServiceDispose()
        {
            _autoFlow?.Dispose();
            _autoFlow = null;
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("AutoFlowService disposed");
        }

        protected override void Update()
        {
            base.Update(); // Importante: chamar a base para manter a inicialização
            
            if (initialized && _autoFlow != null)
            {
                _autoFlow.Process(Time.deltaTime);
            }
        }

        private bool CheckForAutoFlowResources()
        {
            if (resourceSystem == null) return false;

            var allResources = resourceSystem.GetAll();
            if (allResources.Count == 0)
            {
                DebugUtility.LogVerbose<ResourceAutoFlowBridge>("Nenhum recurso encontrado no ResourceSystem");
                return false;
            }

            foreach (var (resourceType, _) in allResources)
            {
                var inst = resourceSystem.GetInstanceConfig(resourceType);
                if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                {
                    DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"✅ Recurso com autoflow encontrado: {resourceType}");
                    return true;
                }
            }

            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("Nenhum recurso com autoflow configurado encontrado");
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

        [ContextMenu("⏸️ Pause AutoFlow")]
        public void ContextPause() 
        {
            if (!initialized) 
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>("Tentando inicializar via ContextMenu...");
                StartCoroutine(InitializeWithRetry());
                return;
            }
            
            _autoFlow?.Pause();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("AutoFlow pausado");
        }
        
        [ContextMenu("▶️ Resume AutoFlow")]
        public void ContextResume() 
        {
            if (!initialized) 
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>("Tentando inicializar via ContextMenu...");
                StartCoroutine(InitializeWithRetry());
                return;
            }
            
            _autoFlow?.Resume();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("AutoFlow retomado");
        }
        
        [ContextMenu("🔄 Toggle AutoFlow")]
        public void ContextToggle() 
        {
            if (!initialized) 
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>("Tentando inicializar via ContextMenu...");
                StartCoroutine(InitializeWithRetry());
                return;
            }
            
            _autoFlow?.Toggle();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"AutoFlow alternado. Pausado: {_autoFlow?.IsPaused}");
        }

        [ContextMenu("🔄 Reset AutoFlow Timers")]
        public void ContextReset() 
        {
            if (!initialized) 
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>("Tentando inicializar via ContextMenu...");
                StartCoroutine(InitializeWithRetry());
                return;
            }
            
            _autoFlow?.ResetTimers();
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>("Timers resetados");
        }

        [ContextMenu("📊 Debug AutoFlow Status")]
        public void DebugAutoFlowStatus()
        {
            base.DebugStatus();
            
            if (resourceSystem != null && initialized)
            {
                int autoFlowCount = CountAutoFlowResources();
                DebugUtility.LogWarning<ResourceAutoFlowBridge>($"📊 Recursos com AutoFlow: {autoFlowCount}");
                
                if (autoFlowCount > 0)
                {
                    DebugUtility.LogWarning<ResourceAutoFlowBridge>("🔧 Recursos com AutoFlow configurado:");
                    foreach (var (resourceType, _) in resourceSystem.GetAll())
                    {
                        var inst = resourceSystem.GetInstanceConfig(resourceType);
                        if (inst is { hasAutoFlow: true } && inst.autoFlowConfig != null)
                        {
                            var cfg = inst.autoFlowConfig;
                            DebugUtility.LogWarning<ResourceAutoFlowBridge>($"   - {resourceType}: " +
                                     $"Fill: {cfg.autoFill}, " +
                                     $"Drain: {cfg.autoDrain}, " +
                                     $"Interval: {cfg.tickInterval}s, " +
                                     $"Amount: {cfg.amountPerTick}" +
                                     $"{(cfg.usePercentage ? "%" : "")}");
                        }
                    }
                }

                if (_autoFlow != null)
                {
                    DebugUtility.LogWarning<ResourceAutoFlowBridge>($"⏱️ AutoFlow State: Paused={_autoFlow.IsPaused}");
                }
            }
            else
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>("ResourceSystem não disponível ou não inicializado");
            }
        }
    }
}