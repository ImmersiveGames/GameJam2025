using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.NewScripts.Gameplay.PostGame
{
    /// <summary>
    /// Controlador do overlay de pós-game (Game Over / Victory) na GameplayScene.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostGameOverlayController : MonoBehaviour
    {
        [Header("Overlay")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text reasonText;

        [Header("Texts")]
        [SerializeField] private string victoryTitle = "Victory!";
        [SerializeField] private string defeatTitle = "Defeat!";
        [SerializeField] private string unknownTitle = "End of Run";
        [SerializeField] private string fallbackTitle = "Result unavailable";

        private EventBinding<GameRunEndedEvent> _runEndedBinding;
        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private bool _registered;

        private void Awake()
        {
            _runEndedBinding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
            RegisterBindings();

            HideImmediate();
        }

        private void OnEnable()
        {
            RegisterBindings();
        }

        private void OnDisable()
        {
            UnregisterBindings();
        }

        private void OnDestroy()
        {
            UnregisterBindings();
        }

        public void OnClickRestart()
        {
            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
            DebugUtility.Log<PostGameOverlayController>(
                "[PostGame] Restart requested -> GameResetRequestedEvent publicado.",
                DebugUtility.Colors.Info);
        }

        public void OnClickExitToMenu()
        {
            EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent());
            DebugUtility.Log<PostGameOverlayController>(
                "[PostGame] ExitToMenu requested -> GameExitToMenuRequestedEvent publicado.",
                DebugUtility.Colors.Info);
        }

        private void RegisterBindings()
        {
            if (_registered)
            {
                return;
            }

            EventBus<GameRunEndedEvent>.Register(_runEndedBinding);
            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            _registered = true;

            DebugUtility.LogVerbose<PostGameOverlayController>(
                "[PostGame] Bindings registrados (GameRunEndedEvent/GameRunStartedEvent).",
                DebugUtility.Colors.Info);
        }

        private void UnregisterBindings()
        {
            if (!_registered)
            {
                return;
            }

            EventBus<GameRunEndedEvent>.Unregister(_runEndedBinding);
            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            _registered = false;
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            DebugUtility.LogVerbose<PostGameOverlayController>(
                $"[PostGame] GameRunStartedEvent recebido (state={evt?.StateId}). Ocultando overlay.",
                DebugUtility.Colors.Info);
            HideImmediate();
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            DebugUtility.LogVerbose<PostGameOverlayController>(
                $"[PostGame] GameRunEndedEvent recebido (outcome={evt?.Outcome}, reason='{evt?.Reason ?? "<null>"}').",
                DebugUtility.Colors.Info);

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunStatusService>(out var statusService) || statusService == null)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    "[PostGame] IGameRunStatusService indisponível. Exibindo fallback de resultado.");
                UpdateTexts(GameRunOutcome.Unknown, null, fallbackTitle);
                Show();
                return;
            }

            UpdateTexts(statusService.Outcome, statusService.Reason, null);
            Show();
        }

        private void UpdateTexts(GameRunOutcome outcome, string reason, string overrideTitle)
        {
            string title = overrideTitle ?? outcome switch
            {
                GameRunOutcome.Victory => victoryTitle,
                GameRunOutcome.Defeat => defeatTitle,
                _ => unknownTitle
            };

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (reasonText != null)
            {
                bool hasReason = !string.IsNullOrWhiteSpace(reason);
                reasonText.text = hasReason ? reason : string.Empty;
                reasonText.gameObject.SetActive(hasReason);
            }
        }

        private void Show()
        {
            if (!TrySetOverlayActive(true))
            {
                return;
            }
        }

        private void HideImmediate()
        {
            TrySetOverlayActive(false);
        }

        private bool TrySetOverlayActive(bool active)
        {
            if (overlayRoot == null)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    "[PostGame] overlayRoot nao configurado. Operacao ignorada.");
                return false;
            }

            overlayRoot.SetActive(active);
            DebugUtility.LogVerbose<PostGameOverlayController>(
                $"[PostGame] Overlay {(active ? "ativado" : "desativado")}.",
                DebugUtility.Colors.Info);
            return true;
        }
    }
}
