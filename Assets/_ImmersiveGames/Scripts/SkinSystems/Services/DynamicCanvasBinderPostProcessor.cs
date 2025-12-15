using _ImmersiveGames.Scripts.ActorSystems;
using ImmersiveGames.RuntimeAttributes.Bind;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Pós-processador padrão que garante a ativação de binders dinâmicos recém-instanciados.
    /// </summary>
    public class DynamicCanvasBinderPostProcessor : ISkinInstancePostProcessor
    {
        public void Process(GameObject instance, ISkinConfig config, IActor owner)
        {
            if (instance == null) return;

            RuntimeAttributeDynamicCanvasBinder[] dynamicBinders = instance.GetComponentsInChildren<RuntimeAttributeDynamicCanvasBinder>(true);
            foreach (var binder in dynamicBinders)
            {
                binder.gameObject.SetActive(true);
            }
        }
    }
}
