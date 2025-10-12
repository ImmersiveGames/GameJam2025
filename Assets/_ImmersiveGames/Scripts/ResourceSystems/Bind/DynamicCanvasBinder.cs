using System.Collections;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public class DynamicCanvasBinder : InjectableCanvasResourceBinder
    {
        [Header("Dynamic Canvas Settings")]
        [SerializeField] private int initializationDelayFrames = 2;
        [SerializeField] private bool registerInPipeline = true;

        // CORREÇÃO: Usar override em vez de new
        public override CanvasType Type => CanvasType.Dynamic;

        public override void OnDependenciesInjected()
        {
            State = CanvasInitializationState.Injecting;
            InjectionState = DependencyInjectionState.Injecting;
            
            StartCoroutine(DynamicInitializationRoutine());
        }

        private IEnumerator DynamicInitializationRoutine()
        {
            for (int i = 0; i < initializationDelayFrames; i++)
                yield return null;

            try
            {
                base.OnDependenciesInjected();
                
                if (CanvasPipelineManager.HasInstance)
                {
                    CanvasPipelineManager.Instance.RegisterCanvas(this);
                    DebugUtility.LogVerbose<DynamicCanvasBinder>($"✅ Dynamic Canvas '{CanvasId}' registered in pipeline");
                }
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogError<DynamicCanvasBinder>($"❌ Dynamic canvas '{CanvasId}' initialization failed: {ex}");
                State = CanvasInitializationState.Failed;
                InjectionState = DependencyInjectionState.Failed;
            }
        }

        // CORREÇÃO: Sobrescrever corretamente em vez de ocultar
        public override void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data)
        {
            if (CanAcceptBinds())
            {
                // Se está pronto, criar slot imediatamente
                CreateSlotForActor(actorId, resourceType, data);
            }
            else
            {
                // Se não está pronto, usar rotina de bind atrasado
                StartCoroutine(DelayedBindRoutine(actorId, resourceType, data));
            }
        }

        private IEnumerator DelayedBindRoutine(string actorId, ResourceType resourceType, IResourceValue data)
        {
            DebugUtility.LogVerbose<DynamicCanvasBinder>($"⏳ Delayed bind scheduled for {actorId}.{resourceType} on canvas '{CanvasId}'");

            float startTime = Time.time;
            float timeout = 5f; // Timeout reduzido para dynamic canvas

            while (!CanAcceptBinds() && (Time.time - startTime) < timeout)
            {
                yield return null;
            }

            if (CanAcceptBinds())
            {
                CreateSlotForActor(actorId, resourceType, data);
                DebugUtility.LogVerbose<DynamicCanvasBinder>($"✅ Delayed bind completed for {actorId}.{resourceType}");
            }
            else
            {
                DebugUtility.LogWarning<DynamicCanvasBinder>($"⏰ Timeout in delayed bind for {actorId}.{resourceType} on canvas '{CanvasId}'");
                
                // Fallback para o pipeline
                if (CanvasPipelineManager.HasInstance)
                {
                    CanvasPipelineManager.Instance.ScheduleBind(actorId, resourceType, data, CanvasId);
                }
            }
        }

        // CORREÇÃO: Remover métodos desnecessários - usar implementação base
        // Os métodos CanAcceptBinds() e ForceReady() da base já são suficientes

        [ContextMenu("🔄 Force Dynamic Ready")]
        public void ForceDynamicReady()
        {
            ForceReady();
            DebugUtility.LogWarning<DynamicCanvasBinder>($"Dynamic canvas '{CanvasId}' forced to ready state");
        }

        [ContextMenu("🔍 DEBUG DYNAMIC CANVAS")]
        public void DebugDynamicCanvas()
        {
            Debug.Log($"🎭 DYNAMIC CANVAS: '{CanvasId}'");
            Debug.Log($"- Type: {Type}, State: {State}");
            Debug.Log($"- Injection: {InjectionState}, CanAcceptBinds: {CanAcceptBinds()}");
            Debug.Log($"- Delay Frames: {initializationDelayFrames}, RegisterInPipeline: {registerInPipeline}");
            DebugCanvas(); // Chamar debug da base
        }
    }
}