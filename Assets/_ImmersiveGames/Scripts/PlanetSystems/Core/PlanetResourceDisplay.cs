using UnityEngine;
using UnityEngine.UI;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    [DisallowMultipleComponent]
    public class PlanetResourceDisplay : MonoBehaviour
    {
        [Header("Configurações Visuais")]
        [Tooltip("Imagem que exibirá o ícone do recurso atribuído ao planeta.")]
        [SerializeField] private Image resourceImage;

        private PlanetsMaster _planetMaster;

        private void Awake()
        {
            TryCachePlanetMaster();
            TryAutoAssignImage();
        }

        private void OnEnable()
        {
            if (!TryCachePlanetMaster())
            {
                DebugUtility.LogWarning<PlanetResourceDisplay>(
                    "Nenhum PlanetsMaster encontrado na hierarquia para exibir o recurso.", this);
                return;
            }

            _planetMaster.ResourceAssigned += HandlePlanetResourceAssigned;

            if (_planetMaster.HasAssignedResource)
            {
                HandlePlanetResourceAssigned(_planetMaster.AssignedResource);
            }
        }

        private void OnDisable()
        {
            if (_planetMaster != null)
            {
                _planetMaster.ResourceAssigned -= HandlePlanetResourceAssigned;
            }
        }

        private void Reset()
        {
            TryAutoAssignImage();
        }

        private void HandlePlanetResourceAssigned(PlanetResourcesSo resource)
        {
            if (resourceImage == null && !TryAutoAssignImage())
            {
                DebugUtility.LogWarning<PlanetResourceDisplay>(
                    "Nenhuma Image configurada para exibir o recurso do planeta.", this);
                return;
            }

            resourceImage.sprite = resource != null ? resource.ResourceIcon : null;
            resourceImage.enabled = resourceImage.sprite != null;
        }

        private bool TryAutoAssignImage()
        {
            if (resourceImage != null)
            {
                return true;
            }

            resourceImage = GetComponentInChildren<Image>();
            return resourceImage != null;
        }

        private bool TryCachePlanetMaster()
        {
            if (_planetMaster != null)
            {
                return true;
            }

            _planetMaster = GetComponentInParent<PlanetsMaster>();
            return _planetMaster != null;
        }
    }
}
