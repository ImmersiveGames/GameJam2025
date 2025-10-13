using System.Collections;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
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

        [ContextMenu("🔍 DEBUG DYNAMIC CANVAS")]
        public void DebugDynamicCanvas()
        {
            Debug.Log($"🎭 DYNAMIC CANVAS: '{CanvasId}'");
            Debug.Log($"- Type: {Type}, State: {State}");
            Debug.Log($"- Injection: {InjectionState}, CanAcceptBinds: {CanAcceptBinds()}");
            Debug.Log($"- Delay Frames: {initializationDelayFrames}, RegisterInPipeline: {registerInPipeline}");
            DebugCanvas();
        }
    }


}