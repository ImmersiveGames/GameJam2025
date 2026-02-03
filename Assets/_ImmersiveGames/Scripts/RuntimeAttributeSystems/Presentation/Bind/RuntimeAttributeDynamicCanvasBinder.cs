using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Utils;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind
{
    public class RuntimeAttributeDynamicCanvasBinder : RuntimeAttributeActorCanvas
    {
        [SerializeField] private bool registerInPipeline = true;
        public override AttributeCanvasType Type => AttributeCanvasType.Dynamic;

        public override void OnDependenciesInjected()
        {
            base.OnDependenciesInjected();

            if (registerInPipeline && RuntimeAttributeCanvasManager.HasInstance)
            {
                RuntimeAttributeCanvasManager.Instance.RegisterCanvas(this);
                RuntimeAttributeEventHub.NotifyCanvasRegistered(CanvasId);

                DebugUtility.LogVerbose<RuntimeAttributeDynamicCanvasBinder>(
                    $"✅ Dynamic Canvas '{CanvasId}' registered & notified",
                    DebugUtility.Colors.Success);
            }
        }
    }
}

