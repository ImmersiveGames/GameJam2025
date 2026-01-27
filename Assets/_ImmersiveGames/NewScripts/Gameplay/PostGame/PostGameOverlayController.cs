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
        private const string RestartReason = "PostGame/Restart";
        private const string ExitToMenuReason = "PostGame/ExitToMenu";
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
        [Inject] private IPostPlayOwnershipService _postPlayOwnership;

        private bool _dependenciesInjected;
        private EventBinding<GameRunEndedEvent> _runEndedBinding;
        private EventBinding<GameRunStartedEvent> _runStartedBinding;
        private bool _registered;
        private IDisposable _postGameGateHandle;
        private bool _loggedMissingGate;
        private bool _isVisible;
        private bool _actionRequested;

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
            if (ShouldOverlayManageOwnership())
            {
                ReleasePostGameGate("OnDisable");
            }
        }

        private void OnDestroy()
        {
            UnregisterBindings();
            if (ShouldOverlayManageOwnership())
            {
                ReleasePostGameGate("OnDestroy");
            }
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() do botão de restart.
        /// </summary>
        public void OnClickRestart()
        {
            if (_actionRequested)
            {
                DebugUtility.LogVerbose<PostGameOverlayController>(
                    "[PostGame] Restart ignorado (ação já solicitada).",
                    DebugUtility.Colors.Info);
                return;
            }

            _actionRequested = true;
            // Ao solicitar restart, o overlay deixa de fazer sentido imediatamente.
            HideImmediate();

            // Caminho único: publicar intenção e deixar a bridge global navegar/resetar.
            // NÃO publicamos GameResumeRequestedEvent aqui: o PostGame usa gate próprio (state.postgame),
            // e publicar resume gerava "release ignorado" no GamePauseGateBridge quando não havia ownership.
            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent(RestartReason));

            DebugUtility.Log<PostGameOverlayController>(
                $"[PostGame] Restart solicitado via overlay. reason='{RestartReason}'.");
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() do botão de voltar ao menu.
        /// </summary>
        public void OnClickExitToMenu()
        {
            if (_actionRequested)
            {
                DebugUtility.LogVerbose<PostGameOverlayController>(
                    "[PostGame] ExitToMenu ignorado (ação já solicitada).",
                    DebugUtility.Colors.Info);
                return;
            }

            _actionRequested = true;
            // Ao sair para o menu, o overlay não é mais relevante — ocultamos imediatamente.
            HideImmediate();

            // Caminho único: publicar intenção e deixar a bridge global navegar.
            EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent(ExitToMenuReason));
            DebugUtility.Log<PostGameOverlayController>(
                $"[PostGame] ExitToMenu solicitado via overlay. reason='{ExitToMenuReason}'.");
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

            _actionRequested = false;
            HideImmediate();
            if (ShouldOverlayManageOwnership())
            {
                ApplyGameplayInputMode("PostGame/RunStarted");
            }
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            if (_isVisible)
            {
                return;
            }

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
            if (_isVisible)
            {
                return;
            }

            _isVisible = true;
            _actionRequested = false;
            SetVisible(true);
            if (ShouldOverlayManageOwnership())
            {
                ApplyPostGameInputMode("PostGame/Show");
                AcquirePostGameGate();
            }
        }

        private void HideImmediate()
        {
            SetVisible(false);

            if (!_isVisible)
            {
                return;
            }

            _isVisible = false;
            if (ShouldOverlayManageOwnership())
            {
                ReleasePostGameGate("HideImmediate");
            }
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

        private bool ShouldOverlayManageOwnership()
        {
            return _postPlayOwnership == null || !_postPlayOwnership.IsOwnerEnabled;
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

        // --------------------------------------------------------------------
        // QA / Context Menu (Editor + DevBuild)
        // --------------------------------------------------------------------
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("QA/PostGame/Force Restart x2 (same frame)")]
        private void QaForceRestartDoubleSameFrame()
        {
            if (!Application.isPlaying)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    "[QA][PostGame] Force Restart x2 ignorado: Application.isPlaying=false.");
                return;
            }

            DebugUtility.Log<PostGameOverlayController>(
                "[QA][PostGame] Forçando Restart x2 (same frame).");

            // Intencional: simula duplo clique/duplo input sem depender de timing humano.
            OnClickRestart();
            OnClickRestart();
        }

        [ContextMenu("QA/PostGame/Force ExitToMenu x2 (same frame)")]
        private void QaForceExitToMenuDoubleSameFrame()
        {
            if (!Application.isPlaying)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    "[QA][PostGame] Force ExitToMenu x2 ignorado: Application.isPlaying=false.");
                return;
            }

            DebugUtility.Log<PostGameOverlayController>(
                "[QA][PostGame] Forçando ExitToMenu x2 (same frame).");

            OnClickExitToMenu();
            OnClickExitToMenu();
        }

        [ContextMenu("QA/PostGame/Force Restart x2 (delay 0.05s)")]
        private void QaForceRestartDoubleWithDelay()
        {
            if (!Application.isPlaying)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    "[QA][PostGame] Force Restart x2 (delay) ignorado: Application.isPlaying=false.");
                return;
            }

            DebugUtility.Log<PostGameOverlayController>(
                "[QA][PostGame] Forçando Restart x2 (delay 0.05s).");

            StartCoroutine(QaCoroutineDouble(() => OnClickRestart(), 0.05f));
        }

        [ContextMenu("QA/PostGame/Force ExitToMenu x2 (delay 0.05s)")]
        private void QaForceExitToMenuDoubleWithDelay()
        {
            if (!Application.isPlaying)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    "[QA][PostGame] Force ExitToMenu x2 (delay) ignorado: Application.isPlaying=false.");
                return;
            }

            DebugUtility.Log<PostGameOverlayController>(
                "[QA][PostGame] Forçando ExitToMenu x2 (delay 0.05s).");

            StartCoroutine(QaCoroutineDouble(() => OnClickExitToMenu(), 0.05f));
        }

        private System.Collections.IEnumerator QaCoroutineDouble(Action action, float delaySeconds)
        {
            // 1a chamada imediata
            action?.Invoke();

            // 2a chamada após um pequeno delay (simula double click humano)
            yield return new WaitForSeconds(delaySeconds);

            action?.Invoke();
        }
#endif
    }
}
