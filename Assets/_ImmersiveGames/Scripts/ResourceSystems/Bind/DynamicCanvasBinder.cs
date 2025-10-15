using System.Collections;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.ResourceSystems.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public class DynamicCanvasBinder : InjectableCanvasResourceBinder
    {
        [Header("Dynamic Canvas Settings")]
        [SerializeField] private int initializationDelayFrames = 2;
        [SerializeField] private bool registerInPipeline = true;

        public override CanvasType Type => CanvasType.Dynamic;

        // NOTA: base.OnDependenciesInjected() é chamado pelo ResourceInitializationManager.
        // Aqui apenas completamos com o registro no pipeline (após estar pronto).
        public override void OnDependenciesInjected()
        {
            base.OnDependenciesInjected();

            if (registerInPipeline && CanvasPipelineManager.HasInstance)
            {
                CanvasPipelineManager.Instance.RegisterCanvas(this);
                DebugUtility.LogVerbose<DynamicCanvasBinder>($"✅ Dynamic Canvas '{CanvasId}' registered in pipeline (post-injection)");
            }

            // Reemitir pendentes no hub (compat)
            ResourceEventHub.NotifyCanvasRegistered(CanvasId);
        }

        // Método público simples para ser chamado pelo código que instancia o prefab (se necessário).
        // NÃO chama base.OnDependenciesInjected(); apenas registra e notifica pendentes.
        public void InitializeDynamicCanvas()
        {
            if (State == CanvasInitializationState.Ready) return;

            DebugUtility.LogVerbose<DynamicCanvasBinder>($"🧩 InitializeDynamicCanvas chamado para '{CanvasId}'");

            if (registerInPipeline && CanvasPipelineManager.HasInstance)
            {
                CanvasPipelineManager.Instance.RegisterCanvas(this);
            }

            ResourceEventHub.NotifyCanvasRegistered(CanvasId);
        }

        public override void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data)
        {
            if (CanAcceptBinds())
            {
                CreateSlotForActor(actorId, resourceType, data);
            }
            else
            {
                StartCoroutine(DelayedBindRoutine(actorId, resourceType, data));
            }
        }

        private IEnumerator DelayedBindRoutine(string actorId, ResourceType resourceType, IResourceValue data)
        {
            DebugUtility.LogVerbose<DynamicCanvasBinder>($"⏳ Delayed bind scheduled for {actorId}.{resourceType} on canvas '{CanvasId}'");

            float startTime = Time.time;
            float timeout = 5f;

            while (!CanAcceptBinds() && (Time.time - startTime) < timeout)
                yield return null;

            if (CanAcceptBinds())
            {
                CreateSlotForActor(actorId, resourceType, data);
                DebugUtility.LogVerbose<DynamicCanvasBinder>($"✅ Delayed bind completed for {actorId}.{resourceType}");
            }
            else
            {
                DebugUtility.LogWarning<DynamicCanvasBinder>($"⏰ Timeout in delayed bind for {actorId}.{resourceType} on canvas '{CanvasId}'");

                if (CanvasPipelineManager.HasInstance)
                {
                    CanvasPipelineManager.Instance.ScheduleBind(actorId, resourceType, data, CanvasId);
                }
            }
        }

        [ContextMenu("🔄 Force Dynamic Ready")]
        public void ForceDynamicReady()
        {
            ForceReady();
            DebugUtility.LogWarning<DynamicCanvasBinder>($"Dynamic canvas '{CanvasId}' forced to ready state");
        }
    }
}