using ImmersiveGames.RuntimeAttributes.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace ImmersiveGames.RuntimeAttributes.Bind
{
    public class RuntimeAttributeSceneCanvasBinder : RuntimeAttributeActorCanvas
    {
        [SerializeField] private bool registerInPipeline = true;
        public override AttributeCanvasType Type => AttributeCanvasType.Scene;

        public override void OnDependenciesInjected()
        {
            base.OnDependenciesInjected();

            if (registerInPipeline && RuntimeAttributeCanvasPipelineManager.HasInstance)
            {
                RuntimeAttributeCanvasPipelineManager.Instance.RegisterCanvas(this);
                DebugUtility.LogVerbose<RuntimeAttributeSceneCanvasBinder>(
                    $"✅ Scene Canvas '{CanvasId}' registered in pipeline",
                    DebugUtility.Colors.Success);
            }
        }
    }
}