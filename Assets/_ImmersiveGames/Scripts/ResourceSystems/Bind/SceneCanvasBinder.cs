using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public class SceneCanvasBinder : ActorResourceCanvas
    {
        [SerializeField] private bool registerInPipeline = true;
        public override CanvasType Type => CanvasType.Scene;

        public override void OnDependenciesInjected()
        {
            base.OnDependenciesInjected();

            if (registerInPipeline && CanvasPipelineManager.HasInstance)
            {
                CanvasPipelineManager.Instance.RegisterCanvas(this);
                DebugUtility.LogVerbose<SceneCanvasBinder>(
                    $"✅ Scene Canvas '{CanvasId}' registered in pipeline",
                    DebugUtility.Colors.Success);
            }
        }
    }
}