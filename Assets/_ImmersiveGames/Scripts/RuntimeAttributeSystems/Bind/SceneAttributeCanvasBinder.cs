using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Bind
{
    public class SceneAttributeCanvasBinder : ActorResourceAttributeCanvas
    {
        [SerializeField] private bool registerInPipeline = true;
        public override AttributeCanvasType Type => AttributeCanvasType.Scene;

        public override void OnDependenciesInjected()
        {
            base.OnDependenciesInjected();

            if (registerInPipeline && AttributeCanvasPipelineManager.HasInstance)
            {
                AttributeCanvasPipelineManager.Instance.RegisterCanvas(this);
                DebugUtility.LogVerbose<SceneAttributeCanvasBinder>(
                    $"✅ Scene Canvas '{CanvasId}' registered in pipeline",
                    DebugUtility.Colors.Success);
            }
        }
    }
}