using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    /// <summary>
    /// Responsável por atualizar o ícone do recurso exibido no canvas associado ao planeta.
    /// </summary>
    public sealed class PlanetResourceCanvasView : MonoBehaviour
    {
        [SerializeField] private PlanetResourceController controller;
        [SerializeField] private Image iconImage;
        [SerializeField] private bool hideImageWhenEmpty = true;

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponentInParent<PlanetResourceController>();
            }

            if (iconImage == null)
            {
                iconImage = GetComponentInChildren<Image>();
            }
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh(controller != null ? controller.CurrentResource : null);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnResourceAssigned(PlanetResourceController _, PlanetResourcesSo resource)
        {
            Refresh(resource);
        }

        private void OnResourceCleared(PlanetResourceController _, PlanetResourcesSo __)
        {
            Refresh(null);
        }

        public void SetController(PlanetResourceController newController)
        {
            if (controller == newController)
            {
                return;
            }

            Unsubscribe();
            controller = newController;
            Subscribe();
            Refresh(controller != null ? controller.CurrentResource : null);
        }

        private void Refresh(PlanetResourcesSo resource)
        {
            if (iconImage == null)
            {
                return;
            }

            iconImage.sprite = resource != null ? resource.ResourceIcon : null;
            if (hideImageWhenEmpty)
            {
                iconImage.enabled = resource != null;
            }
        }

        private void Subscribe()
        {
            if (controller == null)
            {
                return;
            }

            controller.ResourceAssigned += OnResourceAssigned;
            controller.ResourceCleared += OnResourceCleared;
        }

        private void Unsubscribe()
        {
            if (controller == null)
            {
                return;
            }

            controller.ResourceAssigned -= OnResourceAssigned;
            controller.ResourceCleared -= OnResourceCleared;
        }
    }
}
