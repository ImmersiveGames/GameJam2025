using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.ResourceSystems.Utils;
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
            // Chama base (faz reconciliação de ID, pool e registro no orchestrator)
            base.OnDependenciesInjected();

            if (registerInPipeline && CanvasPipelineManager.HasInstance)
            {
                CanvasPipelineManager.Instance.RegisterCanvas(this);
                DebugUtility.LogVerbose<SceneCanvasBinder>(
                    $"✅ Scene Canvas '{CanvasId}' registered in pipeline");
            }

            // Reemite pendentes via Hub (garante compatibilidade com event-driven pipeline)
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
                if (CanvasPipelineManager.HasInstance)
                {
                    CanvasPipelineManager.Instance.ScheduleBind(actorId, resourceType, data, CanvasId);
                    DebugUtility.LogVerbose<SceneCanvasBinder>(
                        $"📤 Bind forwarded to pipeline: {actorId}.{resourceType}");
                }
                else
                {
                    DebugUtility.LogWarning<SceneCanvasBinder>(
                        $"❌ Cannot bind {actorId}.{resourceType} - canvas not ready and no pipeline available");
                }
            }
        }

        [ContextMenu("🔄 Force Scene Ready")]
        public void ForceSceneReady()
        {
            ForceReady();
            DebugUtility.LogWarning<SceneCanvasBinder>(
                $"Scene canvas '{CanvasId}' forced to ready state");
        }
    }
}
