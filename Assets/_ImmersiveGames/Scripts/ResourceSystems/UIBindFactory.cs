using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class UIBindFactory : MonoBehaviour
    {
        [SerializeField] private ResourceUI uiComponent;

        private void Awake()
        {
            if (uiComponent == null)
            {
                uiComponent = GetComponent<ResourceUI>();
                if (uiComponent == null)
                {
                    DebugUtility.LogError<UIBindFactory>("Nenhum ResourceUI encontrado! Vincule um componente ResourceUI no Inspector.", this);
                    return;
                }
            }

            if (string.IsNullOrEmpty(uiComponent.targetResourceId))
            {
                DebugUtility.LogError<UIBindFactory>($"Awake: targetResourceId não configurado em {uiComponent.gameObject.name}! Defina o ResourceId base (ex.: 'Health').", this);
                return;
            }

            var bindHandler = new BindHandler(uiComponent.targetResourceId, uiComponent.targetActorId, uiComponent.targetResourceType);
            uiComponent.SetBindHandler(bindHandler);
            DebugUtility.LogVerbose<UIBindFactory>($"Awake: Configurado BindHandler para UI {uiComponent.gameObject.name} com targetResourceId={uiComponent.targetResourceId}, targetActorId={uiComponent.targetActorId}, targetResourceType={uiComponent.targetResourceType}");
        }
    }
}