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

            if (_visualConfig != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = _visualConfig.icon;
                    iconImage.color = _visualConfig.baseColor;
                }

                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(_visualConfig.baseSize, _visualConfig.baseSize);
                }
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
    }
}
