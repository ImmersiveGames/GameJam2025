using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.World.Compass;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.UI.Compass
{
    /// <summary>
    /// Controla o ícone individual exibido na HUD da bússola para um alvo rastreável.
    /// </summary>
    public class CompassIcon : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("RectTransform do ícone para ajustes de posição e tamanho.")]
        public RectTransform rectTransform;

        [Tooltip("Imagem utilizada para exibir o ícone do alvo.")]
        public Image iconImage;

        [Tooltip("Rótulo opcional para exibir a distância até o alvo.")]
        public TextMeshProUGUI distanceLabel;

        private ICompassTrackable _target;
        private CompassTargetVisualConfig _visualConfig;

        // Suporte a ícones dinâmicos de planetas
        private PlanetsMaster _planetMaster;
        private PlanetResourcesSo _currentResource;
        private bool _isDiscovered;
        private Vector3 _baseScale;

        /// <summary>
        /// Alvo rastreável associado a este ícone.
        /// </summary>
        public ICompassTrackable Target => _target;

        /// <summary>
        /// Inicializa o ícone com o alvo e a configuração visual correspondente.
        /// </summary>
        /// <param name="target">Alvo rastreável.</param>
        /// <param name="visualConfig">Configuração visual aplicada ao ícone.</param>
        public void Initialize(ICompassTrackable target, CompassTargetVisualConfig visualConfig)
        {
            _target = target;
            _visualConfig = visualConfig;
            _baseScale = rectTransform != null ? rectTransform.localScale : Vector3.one;

            if (_visualConfig != null)
            {
                ApplyBaseVisuals();
                ApplyDynamicMode();
            }
        }

        /// <summary>
        /// Atualiza o texto de distância exibido para o alvo associado.
        /// </summary>
        /// <param name="distance">Distância calculada até o alvo.</param>
        public void UpdateDistance(float distance)
        {
            if (distanceLabel == null)
            {
                return;
            }

            int roundedDistance = Mathf.RoundToInt(distance);
            distanceLabel.text = $"{roundedDistance}m";
        }

        private void ApplyBaseVisuals()
        {
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = Vector2.one * _visualConfig.baseSize;
            }

            if (iconImage != null)
            {
                iconImage.color = _visualConfig.baseColor;
            }
        }

        private void ApplyDynamicMode()
        {
            if (_visualConfig.dynamicMode == CompassIconDynamicMode.Static)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = _visualConfig.iconSprite;
                    iconImage.enabled = _visualConfig.iconSprite != null;
                }

                return;
            }

            if (_visualConfig.dynamicMode == CompassIconDynamicMode.PlanetResourceIcon)
            {
                SetupPlanetBindings();
            }
        }

        private void SetupPlanetBindings()
        {
            if (_target == null || _target.Transform == null)
            {
                Debug.LogWarning("[CompassIcon] Target inválido para modo PlanetResourceIcon.");
                return;
            }

            _planetMaster = _target.Transform.GetComponentInParent<PlanetsMaster>();
            if (_planetMaster == null)
            {
                Debug.LogWarning("[CompassIcon] Nenhum PlanetsMaster encontrado no alvo do planeta. Usando fallback estático se disponível.");
                if (iconImage != null && _visualConfig.iconSprite != null)
                {
                    iconImage.sprite = _visualConfig.iconSprite;
                    iconImage.enabled = true;
                }

                return;
            }

            _planetMaster.ResourceAssigned += HandlePlanetResourceAssigned;
            _planetMaster.ResourceDiscoveryChanged += HandlePlanetResourceDiscoveryChanged;

            _currentResource = _planetMaster.AssignedResource;
            _isDiscovered = _planetMaster.IsResourceDiscovered;

            UpdatePlanetIcon();
        }

        private void HandlePlanetResourceAssigned(PlanetResourcesSo resource)
        {
            _currentResource = resource;
            UpdatePlanetIcon();
        }

        private void HandlePlanetResourceDiscoveryChanged(bool isDiscovered)
        {
            _isDiscovered = isDiscovered;
            UpdatePlanetIcon();
        }

        private void UpdatePlanetIcon()
        {
            if (iconImage == null)
            {
                return;
            }

            if (_planetMaster == null)
            {
                iconImage.sprite = _visualConfig.iconSprite;
                iconImage.enabled = _visualConfig.iconSprite != null;
                return;
            }

            if (!_isDiscovered)
            {
                if (_visualConfig.hideUntilDiscovered)
                {
                    iconImage.enabled = false;
                    if (distanceLabel != null)
                    {
                        distanceLabel.enabled = false;
                    }

                    return;
                }

                Sprite targetSprite = _visualConfig.undiscoveredPlanetIcon;
                iconImage.sprite = targetSprite;
                iconImage.enabled = targetSprite != null;

                if (distanceLabel != null)
                {
                    distanceLabel.enabled = targetSprite != null;
                }

                return;
            }

            Sprite discoveredSprite = _currentResource != null ? _currentResource.ResourceIcon : null;
            iconImage.sprite = discoveredSprite;
            iconImage.enabled = discoveredSprite != null;

            if (distanceLabel != null)
            {
                distanceLabel.enabled = discoveredSprite != null;
            }
        }

        private void OnDestroy()
        {
            if (_planetMaster != null)
            {
                _planetMaster.ResourceAssigned -= HandlePlanetResourceAssigned;
                _planetMaster.ResourceDiscoveryChanged -= HandlePlanetResourceDiscoveryChanged;
            }
        }
    }
}
