using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.InputSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.NewScripts.Gameplay.PostGame
{
    /// <summary>
    /// Controller do overlay de pós-game (Game Over / Victory).
    /// Nota: a pausa da simulação em Victory/Defeat é solicitada pelo GameRunStatusService.
    /// Este overlay apenas exibe UI e publica intents (Restart / ExitToMenu).
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostGameOverlayController : MonoBehaviour
    {
        private const string PostGameGateToken = "state.postgame";

        [Header("Overlay")]
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text reasonText;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitToMenuButton;

        [Inject] private IInputModeService _inputModeService;
        [Inject] private ISimulationGateService _gateService;

        private bool _dependenciesInjected;
        private EventBinding<GameRunEndedEvent> _runEndedBinding;
        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private bool _registered;
        private IDisposable _postGameGateHandle;
        private bool _loggedMissingGate;

        private void Awake()
        {
            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = GetComponent<CanvasGroup>();
            }

            _runEndedBinding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);

            RegisterBindings();
            HideImmediate();

            ValidateReferences();
        }

        private void Start()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameRunStatusService>(out var statusService)
                && statusService != null
                && statusService.HasResult)
            {
                DebugUtility.LogVerbose<PostGameOverlayController>(
                    "[PostGame] Resultado pré-existente detectado na inicialização. Atualizando overlay.",
                    DebugUtility.Colors.Info);
                ApplyStatus(statusService);
                Show();
            }
        }

        private void OnEnable() => RegisterBindings();

        private void OnDisable()
        {
            UnregisterBindings();
            ReleasePostGameGate("OnDisable");
        }

        private void OnDestroy()
        {
            UnregisterBindings();
            ReleasePostGameGate("OnDestroy");
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() do botão de restart.
        /// </summary>
        public void OnClickRestart()
        {
            // Ao solicitar restart, o overlay deixa de fazer sentido imediatamente.
            HideImmediate();

            // Victory/Defeat deixa a simulação pausada.
            // Para reiniciar corretamente, liberamos a pausa antes de solicitar reset.
            EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());

            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
            DebugUtility.Log<PostGameOverlayController>(
                "[PostGame] Restart solicitado via overlay.");
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() do botão de voltar ao menu.
        /// </summary>
        public void OnClickExitToMenu()
        {
            // Ao sair para o menu, o overlay não é mais relevante — ocultamos imediatamente.
            HideImmediate();

            // Caminho único: publicar intenção e deixar a bridge global navegar.
            EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent());
            DebugUtility.Log<PostGameOverlayController>(
                "[PostGame] ExitToMenu solicitado via overlay.");
        }

        private void RegisterBindings()
        {
            if (_registered) return;

            EventBus<GameRunEndedEvent>.Register(_runEndedBinding);
            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            _registered = true;

            DebugUtility.LogVerbose<PostGameOverlayController>(
                "[PostGame] Bindings de GameRunEnded/GameRunStarted registrados.",
                DebugUtility.Colors.Info);
        }

        private void UnregisterBindings()
        {
            if (!_registered) return;

            EventBus<GameRunEndedEvent>.Unregister(_runEndedBinding);
            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            _registered = false;

            DebugUtility.LogVerbose<PostGameOverlayController>(
                "[PostGame] Bindings de GameRunEnded/GameRunStarted removidos.",
                DebugUtility.Colors.Info);
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            DebugUtility.LogVerbose<PostGameOverlayController>(
                "[PostGame] GameRunStartedEvent recebido. Ocultando overlay.",
                DebugUtility.Colors.Info);

            HideImmediate();
            ApplyGameplayInputMode("PostGame/RunStarted");
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            DebugUtility.LogVerbose<PostGameOverlayController>(
                "[PostGame] GameRunEndedEvent recebido. Exibindo overlay.",
                DebugUtility.Colors.Info);

            if (!DependencyManager.Provider.TryGetGlobal<IGameRunStatusService>(out var statusService)
                || statusService == null)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    "[PostGame] IGameRunStatusService indisponível. Usando fallback de texto.");
                ApplyFallbackText();
                Show();
                return;
            }

            ApplyStatus(statusService);
            Show();
        }

        private void ApplyStatus(IGameRunStatusService statusService)
        {
            UpdateTextsFromStatus(statusService.Outcome, statusService.Reason);
        }

        private void UpdateTextsFromStatus(GameRunOutcome outcome, string reason)
        {
            switch (outcome)
            {
                case GameRunOutcome.Victory:
                    SetTitle("Victory!");
                    break;
                case GameRunOutcome.Defeat:
                    SetTitle("Defeat!");
                    break;
                default:
                    SetTitle("Match Ended");
                    break;
            }

            SetReason(reason);
        }

        private void ApplyFallbackText()
        {
            SetTitle("Match Ended");
            SetReason("Resultado indisponível.");
        }

        private void SetTitle(string text)
        {
            if (titleText == null) return;
            titleText.text = text;
        }

        private void SetReason(string reason)
        {
            if (reasonText == null) return;

            if (string.IsNullOrWhiteSpace(reason))
            {
                reasonText.text = string.Empty;
                reasonText.gameObject.SetActive(false);
                return;
            }

            reasonText.text = reason;
            reasonText.gameObject.SetActive(true);
        }

        private void Show()
        {
            SetVisible(true);
            ApplyPostGameInputMode("PostGame/Show");
            AcquirePostGameGate();
        }

        private void HideImmediate()
        {
            SetVisible(false);
            ReleasePostGameGate("HideImmediate");
        }

        private void SetVisible(bool visible)
        {
            if (rootCanvasGroup == null)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    "[PostGame] CanvasGroup não configurado. Não foi possível alterar visibilidade.");
                return;
            }

            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }

        private void ApplyPostGameInputMode(string reason)
        {
            EnsureDependenciesInjected();

            if (_inputModeService != null)
            {
                _inputModeService.SetFrontendMenu(reason);
                return;
            }

            DebugUtility.LogWarning<PostGameOverlayController>(
                "[PostGame] IInputModeService indisponível. InputMode não será alternado.");
        }

        private void ApplyGameplayInputMode(string reason)
        {
            EnsureDependenciesInjected();

            if (_inputModeService != null)
            {
                _inputModeService.SetGameplay(reason);
                return;
            }

            DebugUtility.LogWarning<PostGameOverlayController>(
                "[PostGame] IInputModeService indisponível. InputMode não será alternado.");
        }

        private void EnsureDependenciesInjected()
        {
            if (_dependenciesInjected) return;

            var provider = DependencyManager.Provider;
            if (provider == null) return;

            try
            {
                provider.InjectDependencies(this);
                _dependenciesInjected = true;
            }
            catch
            {
                _dependenciesInjected = false;
            }
        }

        private void AcquirePostGameGate()
        {
            EnsureDependenciesInjected();

            if (_postGameGateHandle != null)
            {
                return;
            }

            if (_gateService == null)
            {
                if (!_loggedMissingGate)
                {
                    DebugUtility.LogWarning<PostGameOverlayController>(
                        "[PostGame] ISimulationGateService indisponível. Gate não será adquirido.");
                    _loggedMissingGate = true;
                }

                return;
            }

            _postGameGateHandle = _gateService.Acquire(PostGameGateToken);
            DebugUtility.Log<PostGameOverlayController>(
                $"[PostGame] Gate adquirido token='{PostGameGateToken}'.");
        }

        private void ReleasePostGameGate(string reason)
        {
            if (_postGameGateHandle == null)
            {
                return;
            }

            try
            {
                _postGameGateHandle.Dispose();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    $"[PostGame] Falha ao liberar gate ({reason}): {ex}");
            }
            finally
            {
                _postGameGateHandle = null;
            }

            DebugUtility.Log<PostGameOverlayController>(
                $"[PostGame] Gate liberado token='{PostGameGateToken}'.");
        }

        private void ValidateReferences()
        {
            if (rootCanvasGroup == null)
                DebugUtility.LogWarning<PostGameOverlayController>("[PostGame] rootCanvasGroup não configurado no Inspector.");
            if (titleText == null)
                DebugUtility.LogWarning<PostGameOverlayController>("[PostGame] titleText não configurado no Inspector.");
            if (reasonText == null)
                DebugUtility.LogWarning<PostGameOverlayController>("[PostGame] reasonText não configurado no Inspector.");
            if (restartButton == null)
                DebugUtility.LogWarning<PostGameOverlayController>("[PostGame] restartButton não configurado no Inspector.");
            if (exitToMenuButton == null)
                DebugUtility.LogWarning<PostGameOverlayController>("[PostGame] exitToMenuButton não configurado no Inspector.");
        }
    }
}
