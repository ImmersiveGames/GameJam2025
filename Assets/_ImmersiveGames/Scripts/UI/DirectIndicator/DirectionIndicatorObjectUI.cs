using System;
using _ImmersiveGames.Scripts.GameManagerSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace _ImmersiveGames.Scripts.UI.DirectIndicator
{
    /// <summary>
    /// Esses arquivos foram subistituidos pelo Compass, mas para não perder essa fórmula de localização toraremos isso obsoleto;
    /// </summary>
    [Obsolete]
    public class DirectionIndicatorObjectUI : MonoBehaviour
    {
        [SerializeField] private float screenMargin = 50f; // Margem para não colar nas bordas
        [SerializeField] private RectTransform indicator;
        [SerializeField] private Image indicatorIcon;
        [SerializeField] private TextMeshProUGUI indicatorHideText;

        private Transform _player;
        private Transform _indicatorTarget;
        private Camera _mainCamera;

        private void Awake()
        {
            _player = PlayerManager.Instance.Players[0]; // Assume o primeiro jogador
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (_player == null || _indicatorTarget == null || _mainCamera == null) return;

            var screenPos = _mainCamera.WorldToScreenPoint(_indicatorTarget.position);

            bool onScreen = screenPos is { z: > 0, x: > 0 } && screenPos.x < Screen.width &&
                screenPos.y > 0 && screenPos.y < Screen.height;

            if (onScreen)
            {
                indicator.gameObject.SetActive(false);
                return;
            }

            indicator.gameObject.SetActive(true);

            // Centro da tela
            var screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

            // Direção na tela
            var screenDir = (screenPos - screenCenter).normalized;
            if (screenPos.z < 0)
            {
                screenDir *= -1f; // Inverte a direção se o NPC estiver atrás da câmera
            }

            screenDir.Normalize();

            // Posição do ìcone nas bordas
            var iconPosition = screenCenter + screenDir * (Mathf.Min(Screen.width, Screen.height) / 2f - screenMargin);

            // Aplica posição
            indicator.position = iconPosition;

            // Rotaciona o �cone para apontar na direção
            float angle = Mathf.Atan2(screenDir.y, screenDir.x) * Mathf.Rad2Deg;
            indicator.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        public void Setup(Transform indicatorTarget, Sprite icon, bool isHidden)
        {
            _indicatorTarget = indicatorTarget;
            indicatorIcon.sprite = icon;
            Hide(isHidden);
        }

        private void Hide(bool isHidden)
        {
            indicatorIcon.enabled = !isHidden;
            indicatorHideText.enabled = isHidden;
        }
    }
}
