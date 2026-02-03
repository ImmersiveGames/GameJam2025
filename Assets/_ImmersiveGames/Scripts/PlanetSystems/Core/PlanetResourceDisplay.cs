using UnityEngine;
using UnityEngine.UI;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    /// <summary>
    /// Responsável por exibir o ícone do recurso de um planeta
    /// na UI, reagindo à atribuição e à descoberta do recurso.
    ///
    /// Usa o PlanetsMaster como fonte de eventos, que por sua vez
    /// delega o estado de recurso para o módulo PlanetResourceState.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Planet Systems/Planet Resource Display")]
    public sealed class PlanetResourceDisplay : MonoBehaviour
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
            EnsureResourceImage();
        }

        private void OnEnable()
        {
            if (!TryCachePlanetMaster())
            {
                DebugUtility.LogWarning<PlanetResourceDisplay>(
                    "Nenhum PlanetsMaster encontrado na hierarquia para exibir o recurso.",
                    this);
                return;
            }

            _planetMaster.ResourceAssigned += HandlePlanetResourceAssigned;
            _planetMaster.ResourceDiscoveryChanged += HandleResourceDiscoveryChanged;

            SyncWithPlanetState();
        }

        private void OnDisable()
        {
            if (_planetMaster == null)
            {
                return;
            }

            _planetMaster.ResourceAssigned -= HandlePlanetResourceAssigned;
            _planetMaster.ResourceDiscoveryChanged -= HandleResourceDiscoveryChanged;
        }

        private void Reset()
        {
            EnsureResourceImage();
        }

        /// <summary>
        /// Atualiza o estado interno e a UI com base no estado atual do planeta.
        /// </summary>
        private void SyncWithPlanetState()
        {
            if (_planetMaster == null)
            {
                return;
            }

            _currentResource = _planetMaster.AssignedResource;
            HandleResourceDiscoveryChanged(_planetMaster.IsResourceDiscovered);
        }

        private void HandlePlanetResourceAssigned(PlanetResourcesSo resource)
        {
            _currentResource = resource;

            if (!EnsureResourceImage())
            {
                DebugUtility.LogWarning<PlanetResourceDisplay>(
                    "Nenhuma Image configurada para exibir o recurso do planeta.",
                    this);
                return;
            }

            UpdateResourceSprite(_planetMaster != null && _planetMaster.IsResourceDiscovered);
        }

        private void HandleResourceDiscoveryChanged(bool isDiscovered)
        {
            if (!EnsureResourceImage())
            {
                DebugUtility.LogWarning<PlanetResourceDisplay>(
                    "Nenhuma Image configurada para exibir o recurso do planeta.",
                    this);
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
                        "Sprite de recurso não descoberto não configurada.",
                        this);
                }
            }
            else
            {
                targetSprite = _currentResource != null ? _currentResource.ResourceIcon : null;
            }

            resourceImage.sprite = targetSprite;
            resourceImage.enabled = targetSprite != null;
        }

        /// <summary>
        /// Garante que a Image de destino está atribuída, tentando
        /// localizar automaticamente em filhos quando necessário.
        /// </summary>
        private bool EnsureResourceImage()
        {
            if (resourceImage != null)
            {
                return true;
            }

            resourceImage = GetComponentInChildren<Image>(true);
            return resourceImage != null;
        }

        /// <summary>
        /// Tenta encontrar e cachear o PlanetsMaster na hierarquia.
        /// </summary>
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

