using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Services;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Bind
{
    public class DynamicAttributeCanvasBinder : ActorResourceAttributeCanvas
    {
        [SerializeField] private bool registerInPipeline = true;
        public override AttributeCanvasType Type => AttributeCanvasType.Dynamic;

        public override void OnDependenciesInjected()
        {
            base.OnDependenciesInjected();

            if (registerInPipeline && AttributeCanvasPipelineManager.HasInstance)
            {
                AttributeCanvasPipelineManager.Instance.RegisterCanvas(this);
                RuntimeAttributeEventHub.NotifyCanvasRegistered(CanvasId);

                DebugUtility.LogVerbose<DynamicAttributeCanvasBinder>(
                    $"✅ Dynamic Canvas '{CanvasId}' registered & notified",
                    DebugUtility.Colors.Success);
            }
        }
    }
}