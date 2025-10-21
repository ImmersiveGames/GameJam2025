using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.ResourceSystems.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public class DynamicCanvasBinder : InjectableCanvasResourceBinder
    {
        [SerializeField] private bool registerInPipeline = true;
        public override CanvasType Type => CanvasType.Dynamic;

        public override void OnDependenciesInjected()
        {
            base.OnDependenciesInjected();

            if (registerInPipeline && CanvasPipelineManager.HasInstance)
            {
                CanvasPipelineManager.Instance.RegisterCanvas(this);
                ResourceEventHub.NotifyCanvasRegistered(CanvasId);

                DebugUtility.Log<DynamicCanvasBinder>(
                    $"✅ Dynamic Canvas '{CanvasId}' registered & notified",
                    DebugUtility.Colors.Success);
            }
        }
    }
}