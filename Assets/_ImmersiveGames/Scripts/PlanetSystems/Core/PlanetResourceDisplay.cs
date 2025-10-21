using UnityEngine;
using UnityEngine.UI;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    [DisallowMultipleComponent]
    public class PlanetResourceDisplay : MonoBehaviour
    {
        [Header("Configurações Visuais")]
        [Tooltip("Imagem que exibirá o ícone do recurso atribuído ao planeta.")]
        [SerializeField] private Image resourceImage;

        [Tooltip("Sprite utilizada enquanto o recurso do planeta ainda não foi descoberto.")]
        [SerializeField] private Sprite undiscoveredResourceIcon;

        private PlanetsMaster _planetMaster;
        private PlanetResourcesSo _currentResource;

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
            _planetMaster.ResourceDiscoveryChanged += HandleResourceDiscoveryChanged;

            _currentResource = _planetMaster.AssignedResource;
            HandleResourceDiscoveryChanged(_planetMaster.IsResourceDiscovered);
        }

        private void OnDisable()
        {
            if (_planetMaster != null)
            {
                _planetMaster.ResourceAssigned -= HandlePlanetResourceAssigned;
                _planetMaster.ResourceDiscoveryChanged -= HandleResourceDiscoveryChanged;
            }
        }

        private void Reset()
        {
            TryAutoAssignImage();
        }

        private void HandlePlanetResourceAssigned(PlanetResourcesSo resource)
        {
            _currentResource = resource;
            if (resourceImage == null && !TryAutoAssignImage())
            {
                DebugUtility.LogWarning<PlanetResourceDisplay>(
                    "Nenhuma Image configurada para exibir o recurso do planeta.", this);
                return;
            }

            UpdateResourceSprite(_planetMaster != null && _planetMaster.IsResourceDiscovered);
        }

        private void HandleResourceDiscoveryChanged(bool isDiscovered)
        {
            if (resourceImage == null && !TryAutoAssignImage())
            {
                DebugUtility.LogWarning<PlanetResourceDisplay>(
                    "Nenhuma Image configurada para exibir o recurso do planeta.", this);
                return;
            }

            UpdateResourceSprite(isDiscovered);
        }

        private void UpdateResourceSprite(bool isDiscovered)
        {
            if (resourceImage == null)
            {
                return;
            }

            Sprite targetSprite;

            if (!isDiscovered)
            {
                targetSprite = undiscoveredResourceIcon;
                if (targetSprite == null)
                {
                    DebugUtility.LogWarning<PlanetResourceDisplay>(
                        "Sprite de recurso não descoberto não configurada.", this);
                }
            }
            else
            {
                targetSprite = _currentResource != null ? _currentResource.ResourceIcon : null;
            }

            resourceImage.sprite = targetSprite;
            resourceImage.enabled = targetSprite != null;
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
