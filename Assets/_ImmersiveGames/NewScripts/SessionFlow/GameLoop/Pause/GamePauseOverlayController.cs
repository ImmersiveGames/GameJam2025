/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Criar PauseOverlayRoot desativado, adicionar GamePauseOverlayController e arrastar a referencia.
 * - Conectar botao Resume para GamePauseOverlayController.Resume().
 * - Conectar botao ReturnToMenu para GamePauseOverlayController.ReturnToMenuFrontend().
 *
 * NOTAS IMPORTANTES (NewScripts)
 * - O overlay PRECISA reagir ao contrato canônico de pause:
 *     - PauseStateChangedEvent(true)  => mostrar overlay (apenas durante run ativa e antes do fim).
 *     - PauseStateChangedEvent(false) => ocultar overlay.
 * - Ao reagir ao estado, o overlay NÃO altera ownership do pause.
 */

using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Commands;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Pause
{
    /// <summary>
    /// Controlador do contexto visual local de PauseMenu no UIGlobal.
    ///
    /// Duas responsabilidades:
    /// 1) UI local (overlayRoot + InputMode)
    /// 2) Acionar o contrato público IPauseCommands quando acionado por UI (botões)
    ///
    /// Regras:
    /// - Mostrar overlay apenas durante run ativa (após GameRunStartedEvent) e antes do fim (antes de GameRunEndedEvent).
    /// - Pós-game usa overlay próprio; pausas de "freeze" no fim da run não devem abrir este overlay.
    ///
    /// Importante:
    /// - Quando reagindo ao estado, este controller NÃO altera o owner canônico.
    ///   Só muda a UI e o InputMode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class GamePauseOverlayController : MonoBehaviour
    {
        private const string ExitToMenuReason = "PauseOverlay/ExitToMenu";
        [Header("Overlay")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private string showReason = "PauseOverlay/Show";
        [SerializeField] private string hideReason = "PauseOverlay/Hide";
        [SerializeField] private bool enableEscapeHotkey = true;

        private bool _dependenciesInjected;
        [Inject] private IPauseCommands _pauseCommands;
        [Inject] private IPauseStateService _pauseStateService;
        [Inject] private IGameNavigationService _navigationService;
        [Inject] private ISessionIntegrationContextService _sessionIntegrationContextService;

        // Run lifecycle gating (mesma regra do Hotkey)
        private bool _runActive;
        private bool _runEnded;

        // Event bindings
        private EventBinding<GameRunStartedEvent> _onRunStarted;
        private EventBinding<GameRunEndedEvent> _onRunEnded;
        private EventBinding<PauseStateChangedEvent> _onPauseStateChanged;
        private int _lastEscapeToggleFrame = -1;

        private void Awake()
        {
            // Run lifecycle
            _onRunStarted = new EventBinding<GameRunStartedEvent>(_ => OnRunStarted());
            _onRunEnded = new EventBinding<GameRunEndedEvent>(_ => OnRunEnded());
            _onPauseStateChanged = new EventBinding<PauseStateChangedEvent>(OnPauseStateChanged);

            EventBus<GameRunStartedEvent>.Register(_onRunStarted);
            EventBus<GameRunEndedEvent>.Register(_onRunEnded);
            EventBus<PauseStateChangedEvent>.Register(_onPauseStateChanged);
        }

        private void Start()
        {
            EnsureDependenciesInjected();

            if (overlayRoot == null)
            {
                DebugUtility.LogWarning(typeof(GamePauseOverlayController),
                    "[PauseOverlay] overlayRoot nao configurado no Inspector.");
            }
            else
            {
                // Safety: no boot, deve começar oculto (root desativado no prefab).
                // Não publica evento aqui; apenas garante estado de UI consistente.
                if (overlayRoot.activeSelf)
                {
                    TrySetOverlayActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            EventBus<GameRunStartedEvent>.Unregister(_onRunStarted);
            EventBus<GameRunEndedEvent>.Unregister(_onRunEnded);
            EventBus<PauseStateChangedEvent>.Unregister(_onPauseStateChanged);
        }

        private void OnDisable()
        {
            if (overlayRoot == null)
            {
                return;
            }

            if (overlayRoot.activeSelf)
            {
                overlayRoot.SetActive(false);
                DebugUtility.LogVerbose(typeof(GamePauseOverlayController),
                    "[PauseOverlay] OnDisable -> overlay desativado sem alterar o estado canônico de pause.",
                    DebugUtility.Colors.Info);
            }
        }

        // =========================
        // Public API (UI Buttons)
        // =========================

        /// <summary>
        /// Chamado por UI (ex.: botão Pause, se existir).
        /// Emite intent visual de pause e delega ao contrato canônico de pause.
        /// </summary>
        public void Show()
        {
            EnsureDependenciesInjected();
            if (_pauseCommands == null)
            {
                DebugUtility.LogWarning(typeof(GamePauseOverlayController),
                    "[PauseOverlay][Intent] IPauseCommands indisponivel; intent de pause nao delegada.");
                return;
            }

            DebugUtility.Log(typeof(GamePauseOverlayController),
                $"[OBS][PauseOverlay][Intent] Pause solicitado pelo contexto visual local. reason='{showReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(GamePauseOverlayController),
                "[OBS][PauseOverlay][Delegate] RequestPause encaminhado ao executor real IPauseCommands.",
                DebugUtility.Colors.Info);

            _pauseCommands.RequestPause(showReason);
        }

        /// <summary>
        /// Chamado por UI (botão Resume).
        /// Emite intent visual de resume e delega ao contrato canônico de pause.
        /// </summary>
        public void Hide()
        {
            EnsureDependenciesInjected();
            if (_pauseCommands == null)
            {
                DebugUtility.LogWarning(typeof(GamePauseOverlayController),
                    "[PauseOverlay][Intent] IPauseCommands indisponivel; intent de resume nao delegada.");
                return;
            }

            DebugUtility.Log(typeof(GamePauseOverlayController),
                $"[OBS][PauseOverlay][Intent] Resume solicitado pelo contexto visual local. reason='{hideReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(GamePauseOverlayController),
                "[OBS][PauseOverlay][Delegate] RequestResume encaminhado ao executor real IPauseCommands.",
                DebugUtility.Colors.Info);

            _pauseCommands.RequestResume(hideReason);
        }

        public void Resume() => Hide();

        /// <summary>
        /// Chamado por UI (botão ReturnToMenu).
        /// Publica intent visual e delega direto ao owner canonico de navigation.
        /// </summary>
        public void ReturnToMenuFrontend()
        {
            EnsureDependenciesInjected();

            DebugUtility.LogVerbose(typeof(GamePauseOverlayController),
                $"[PauseOverlay][Intent] ReturnToMenuFrontend delegado ao executor real IGameNavigationService. reason='{ExitToMenuReason}'.",
                DebugUtility.Colors.Info);

            if (_navigationService == null)
            {
                HardFailFastH1.Trigger(typeof(GamePauseOverlayController),
                    $"[FATAL][H1][Navigation] IGameNavigationService indisponivel para ReturnToMenuFrontend. reason='{ExitToMenuReason}'.");
                return;
            }

            _ = ObserveAsync(_navigationService.GoToMenuAsync(ExitToMenuReason), ExitToMenuReason);
            PublishFrontendMenuInputMode("PauseOverlay/ReturnToMenuFrontend");
        }

        private static async System.Threading.Tasks.Task ObserveAsync(System.Threading.Tasks.Task task, string reason)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GamePauseOverlayController),
                    $"[PauseOverlay] ReturnToMenuFrontend failed reason='{reason}'. ex={ex}");
            }
        }

        public void Toggle()
        {
            EnsureDependenciesInjected();

            if (_pauseStateService == null)
            {
                DebugUtility.LogWarning(typeof(GamePauseOverlayController),
                    "[PauseOverlay] IPauseStateService indisponivel; Toggle de intent ignorado.");
                return;
            }

            if (_pauseStateService.IsPaused)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void Update()
        {
            if (!enableEscapeHotkey)
            {
                return;
            }

            if (!_runActive || _runEnded)
            {
                return;
            }

            if (!WasEscapePressedThisFrame())
            {
                return;
            }

            int frame = Time.frameCount;
            if (_lastEscapeToggleFrame == frame)
            {
                return;
            }

            bool willResume = _pauseStateService != null && _pauseStateService.IsPaused;

            _lastEscapeToggleFrame = frame;
            Toggle();

            DebugUtility.LogVerbose(typeof(GamePauseOverlayController),
                $"[OBS][PauseOverlay] EscapeHotkeyToggle action='{(willResume ? "resume" : "pause")}' runActive='{_runActive}' runEnded='{_runEnded}'",
                DebugUtility.Colors.Info);
        }
        // Event reactions (NO publish)
        // =========================

        private void OnRunStarted()
        {
            _runActive = true;
            _runEnded = false;

            // Garantia: ao iniciar run, overlay deve estar oculto.
            HideLocal("GameRunStarted");
        }

        private void OnRunEnded()
        {
            _runEnded = true;

            // Pós-game usa overlay próprio; este overlay deve estar oculto.
            HideLocal("GameRunEnded");
        }

        private void OnPauseStateChanged(PauseStateChangedEvent evt)
        {
            DebugUtility.LogVerbose(typeof(GamePauseOverlayController),
                $"[OBS][PauseOverlay] PauseStateChangedEvent consumed isPaused='{evt?.IsPaused}' runActive='{_runActive}' runEnded='{_runEnded}'.",
                DebugUtility.Colors.Info);

            if (!_runActive || _runEnded)
            {
                HideLocal("PauseStateChanged_NotActiveOrEnded");
                return;
            }

            if (evt.IsPaused)
            {
                ShowLocal("PauseStateChanged");
            }
            else
            {
                HideLocal("PauseStateChanged");
            }
        }

        // Local UI-only helpers (no EventBus)
        private void ShowLocal(string reason)
        {
            EnsureDependenciesInjected();

            if (!TrySetOverlayActive(true))
            {
                return;
            }

            DebugUtility.LogVerbose(typeof(GamePauseOverlayController),
                $"[PauseOverlay] ShowLocal (reason='{reason}').",
                DebugUtility.Colors.Info);

            ApplyPauseInputMode();
        }

        private void HideLocal(string reason)
        {
            EnsureDependenciesInjected();

            if (overlayRoot != null && !overlayRoot.activeSelf)
            {
                return;
            }

            TrySetOverlayActive(false);

            DebugUtility.LogVerbose(typeof(GamePauseOverlayController),
                $"[PauseOverlay] HideLocal (reason='{reason}').",
                DebugUtility.Colors.Info);
        }

        private void ApplyPauseInputMode()
        {
            DebugUtility.Log(typeof(GamePauseOverlayController),
                $"[OBS][PauseOverlay][Visual] UI local exibida. reason='{showReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(GamePauseOverlayController),
                $"[OBS][InputMode] Request mode='PauseOverlay' map='UI' phase='Pause' reason='{showReason}' source='SessionIntegration'.",
                DebugUtility.Colors.Info);

            PublishPauseOverlayInputMode(showReason);
        }

        private void PublishFrontendMenuInputMode(string reason)
        {
            EnsureDependenciesInjected();

            if (_sessionIntegrationContextService == null)
            {
                HardFailFastH1.Trigger(typeof(GamePauseOverlayController),
                    $"[FATAL][H1][SessionIntegration] ISessionIntegrationContextService indisponivel para FrontendMenu input mode. reason='{reason}'.");
                return;
            }

            _sessionIntegrationContextService.RequestFrontendMenuInputMode(reason, "PauseOverlay");
        }

        private void PublishPauseOverlayInputMode(string reason)
        {
            EnsureDependenciesInjected();

            if (_sessionIntegrationContextService == null)
            {
                HardFailFastH1.Trigger(typeof(GamePauseOverlayController),
                    $"[FATAL][H1][SessionIntegration] ISessionIntegrationContextService indisponivel para PauseOverlay input mode. reason='{reason}'.");
                return;
            }

            _sessionIntegrationContextService.RequestPauseOverlayInputMode(reason, "PauseOverlay");
        }

        private static bool WasEscapePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        // DI + UI toggling
        // =========================

        // =========================

        private void EnsureDependenciesInjected()
        {
            if (_dependenciesInjected)
            {
                return;
            }

            var provider = DependencyManager.Provider;
            if (provider == null)
            {
                return;
            }

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

        private bool TrySetOverlayActive(bool active)
        {
            if (overlayRoot == null)
            {
                DebugUtility.LogWarning(typeof(GamePauseOverlayController),
                    "[PauseOverlay] overlayRoot nao configurado. Operacao ignorada.");
                return false;
            }

            if (overlayRoot.activeSelf == active)
            {
                // Para reações (event-driven), isso é normal e desejável (idempotência).
                return false;
            }

            overlayRoot.SetActive(active);
            DebugUtility.LogVerbose(typeof(GamePauseOverlayController),
                $"[PauseOverlay] Overlay {(active ? "ativado" : "desativado")}.",
                DebugUtility.Colors.Info);
            return true;
        }
    }
}














