using ImmersiveGames.RuntimeAttributes.Services;
using ImmersiveGames.RuntimeAttributes.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace ImmersiveGames.RuntimeAttributes.Bind
{
    public class RuntimeAttributeDynamicCanvasBinder : RuntimeAttributeActorCanvas
    {
        [SerializeField] private bool registerInPipeline = true;
        public override AttributeCanvasType Type => AttributeCanvasType.Dynamic;

        public override void OnDependenciesInjected()
        {
            base.OnDependenciesInjected();

            if (registerInPipeline && RuntimeAttributeCanvasPipelineManager.HasInstance)
            {
                RuntimeAttributeCanvasPipelineManager.Instance.RegisterCanvas(this);
                RuntimeAttributeEventHub.NotifyCanvasRegistered(CanvasId);

                DebugUtility.LogVerbose<RuntimeAttributeDynamicCanvasBinder>(
                    $"✅ Dynamic Canvas '{CanvasId}' registered & notified",
                    DebugUtility.Colors.Success);
            }
        }
    }
}