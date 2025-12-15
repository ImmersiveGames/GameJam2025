using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind
{
    public class RuntimeAttributeSceneCanvasBinder : RuntimeAttributeActorCanvas
    {
        [SerializeField] private bool registerInPipeline = true;
        public override AttributeCanvasType Type => AttributeCanvasType.Scene;

        public override void OnDependenciesInjected()
        {
            base.OnDependenciesInjected();

            if (registerInPipeline && RuntimeAttributeCanvasManager.HasInstance)
            {
                RuntimeAttributeCanvasManager.Instance.RegisterCanvas(this);
                DebugUtility.LogVerbose<RuntimeAttributeSceneCanvasBinder>(
                    $"✅ Scene Canvas '{CanvasId}' registered in pipeline",
                    DebugUtility.Colors.Success);
            }
        }
    }
}