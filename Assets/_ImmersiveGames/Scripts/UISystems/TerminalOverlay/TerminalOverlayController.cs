using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameResetRequestedEvent = _ImmersiveGames.NewScripts.Gameplay.GameLoop.GameResetRequestedEvent;
using GameResumeRequestedEvent = _ImmersiveGames.NewScripts.Gameplay.GameLoop.GameResumeRequestedEvent;

namespace _ImmersiveGames.Scripts.UISystems.TerminalOverlay
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class TerminalOverlayController : MonoBehaviour, ITerminalOverlayService
    {
        [Header("UI Root")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Animator animator;

        [Header("Text (Opcional)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text reasonText;

        [Header("Buttons (Opcional)")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button closeButton;

        [Header("Animator Triggers (Opcional)")]
        [SerializeField] private string triggerShow = "Show";
        [SerializeField] private string triggerHide = "Hide";

        public bool IsVisible { get; private set; }

        private void Awake()
        {
            if (canvasGroup == null)
            {
                DebugUtility.LogWarning<TerminalOverlayController>(
                    "[TerminalOverlay] CanvasGroup não configurado. Overlay não será exibido.");
                return;
            }

            // 🔒 Garante que o overlay SEMPRE receba raycast quando visível
            canvasGroup.ignoreParentGroups = true;
            SetVisible(false);

            if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(OnRestartClicked);
            if (menuButton != null) menuButton.onClick.RemoveListener(OnMenuClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
        }

        public void ShowVictory(string reason = null) => ShowInternal("VICTORY", reason);
        public void ShowGameOver(string reason = null) => ShowInternal("GAME OVER", reason);

        public void Hide()
        {
            if (!IsVisible) return;

            if (animator != null && !string.IsNullOrWhiteSpace(triggerHide))
                animator.SetTrigger(triggerHide);

            SetVisible(false);
            DebugUtility.LogVerbose<TerminalOverlayController>("[TerminalOverlay] Hide()");
        }

        private void ShowInternal(string title, string reason)
        {
            if (canvasGroup == null) return;

            if (titleText != null) titleText.text = title;
            if (reasonText != null) reasonText.text = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason;

            if (animator != null && !string.IsNullOrWhiteSpace(triggerShow))
                animator.SetTrigger(triggerShow);

            SetVisible(true);
            DebugUtility.LogVerbose<TerminalOverlayController>(
                $"[TerminalOverlay] Show '{title}' (reason='{reason ?? "null"}')");
        }

        private void SetVisible(bool visible)
        {
            IsVisible = visible;

            if (canvasGroup == null) return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            if (visible)
            {
                canvasGroup.ignoreParentGroups = true;
            }
        }

        private void OnRestartClicked()
        {
            DebugUtility.LogVerbose<TerminalOverlayController>(
                "[TerminalOverlay] Restart clicked -> GameResetRequestedEvent");

            Hide();
            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
        }

        private void OnMenuClicked()
        {
            DebugUtility.LogVerbose<TerminalOverlayController>(
                "[TerminalOverlay] Menu clicked -> GameReturnToMenuRequestedEvent");

            Hide();
            EventBus<GameReturnToMenuRequestedEvent>.Raise(new GameReturnToMenuRequestedEvent());
        }
    }
}
