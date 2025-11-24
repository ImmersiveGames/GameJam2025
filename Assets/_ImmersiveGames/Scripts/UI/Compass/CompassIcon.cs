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
        private Color _currentColor = Color.white;

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
                _currentColor = _visualConfig.baseColor;
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

            Sprite targetSprite;

            if (_planetMaster == null)
            {
                targetSprite = _visualConfig.iconSprite;
                ApplyBaseVisuals();
            }
            else if (!_isDiscovered)
            {
                if (_visualConfig.hideUntilDiscovered)
                {
                    targetSprite = _visualConfig.undiscoveredPlanetIcon ?? _visualConfig.iconSprite;
                }
                else
                {
                    targetSprite = _visualConfig.iconSprite ?? _visualConfig.undiscoveredPlanetIcon;
                }

                ApplyBaseVisuals();
            }
            else
            {
                targetSprite = _currentResource != null ? _currentResource.ResourceIcon : null;
                if (targetSprite == null)
                {
                    targetSprite = _visualConfig.iconSprite ?? _visualConfig.undiscoveredPlanetIcon;
                }

                ApplyPlanetResourceStyle();
            }

            iconImage.sprite = targetSprite;
            bool hasSprite = targetSprite != null;
            iconImage.enabled = hasSprite;

            if (distanceLabel != null)
            {
                distanceLabel.enabled = hasSprite;
            }
        }

        private void ApplyPlanetResourceStyle()
        {
            if (_visualConfig == null)
            {
                return;
            }

            var baseColor = _visualConfig.baseColor;
            float baseSize = _visualConfig.baseSize;

            var finalColor = baseColor;

            var resourceType = _currentResource != null ? _currentResource.ResourceType : default;

            if (_visualConfig.planetResourceStyleDatabase != null)
            {
                finalColor = _visualConfig.planetResourceStyleDatabase.GetColorForResource(resourceType, baseColor);
            }

            if (rectTransform != null)
            {
                rectTransform.sizeDelta = Vector2.one * baseSize;
            }

            if (iconImage != null)
            {
                iconImage.color = finalColor;
                _currentColor = finalColor;
            }
        }

        /// <summary>
        /// Aplica ou remove destaque visual para ícones de planeta marcado.
        /// </summary>
        /// <param name="isMarked">Indica se o ícone representa o planeta marcado atualmente.</param>
        /// <param name="scaleMultiplier">Multiplicador opcional aplicado à escala do ícone quando marcado.</param>
        /// <param name="colorTint">Cor opcional multiplicada ao ícone ao marcar.</param>
        public void SetMarked(bool isMarked, float scaleMultiplier = 1.3f, Color? colorTint = null)
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = isMarked ? _baseScale * scaleMultiplier : _baseScale;
            }

            if (iconImage != null && colorTint.HasValue)
            {
                iconImage.color = isMarked ? _currentColor * colorTint.Value : _currentColor;
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
