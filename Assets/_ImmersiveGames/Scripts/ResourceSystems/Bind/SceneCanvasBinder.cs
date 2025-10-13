using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public class SceneCanvasBinder : InjectableCanvasResourceBinder
    {
        [Header("Scene Canvas Settings")]
        [SerializeField] private bool registerInPipeline = true;
        
        public override CanvasType Type => CanvasType.Scene;

        public override void OnDependenciesInjected()
        {
            State = CanvasInitializationState.Injecting;
            InjectionState = DependencyInjectionState.Injecting;

            try
            {
                base.OnDependenciesInjected();
                
                if (CanvasPipelineManager.HasInstance)
                {
                    CanvasPipelineManager.Instance.RegisterCanvas(this);
                    DebugUtility.LogVerbose<SceneCanvasBinder>($"✅ Scene Canvas '{CanvasId}' registered in pipeline");
                }
            }
            catch (System.Exception ex)
            {
                DebugUtility.LogError<SceneCanvasBinder>($"❌ Scene canvas '{CanvasId}' initialization failed: {ex}");
                State = CanvasInitializationState.Failed;
                InjectionState = DependencyInjectionState.Failed;
            }
        }

        // CORREÇÃO: Sobrescrever corretamente
        public override void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data)
        {
            if (CanAcceptBinds())
            {
                // Se está pronto, criar slot imediatamente
                CreateSlotForActor(actorId, resourceType, data);
            }
            else
            {
                // CORREÇÃO: Usar fallback para pipeline em vez de tentar criar slot diretamente
                if (CanvasPipelineManager.HasInstance)
                {
                    CanvasPipelineManager.Instance.ScheduleBind(actorId, resourceType, data, CanvasId);
                    DebugUtility.LogVerbose<SceneCanvasBinder>($"📤 Bind forwarded to pipeline: {actorId}.{resourceType}");
                }
                else
                {
                    DebugUtility.LogWarning<SceneCanvasBinder>($"❌ Cannot bind {actorId}.{resourceType} - canvas not ready and no pipeline available");
                }
            }
        }
        

        [ContextMenu("🔄 Force Scene Ready")]
        public void ForceSceneReady()
        {
            ForceReady();
            DebugUtility.LogWarning<SceneCanvasBinder>($"Scene canvas '{CanvasId}' forced to ready state");
        }

        [ContextMenu("🔍 DEBUG SCENE CANVAS")]
        public void DebugSceneCanvas()
        {
            Debug.Log($"🏞️ SCENE CANVAS: '{CanvasId}'");
            Debug.Log($"- Type: {Type}, State: {State}");
            Debug.Log($"- Injection: {InjectionState}, CanAcceptBinds: {CanAcceptBinds()}");
            Debug.Log($"- RegisterInPipeline: {registerInPipeline}");
            DebugCanvas(); // Chamar debug da base
        }
    }
}