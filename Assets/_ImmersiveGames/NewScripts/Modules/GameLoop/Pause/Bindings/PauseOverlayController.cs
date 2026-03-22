/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Criar PauseOverlayRoot desativado, adicionar PauseOverlayController e arrastar a referencia.
 * - Conectar botao Resume para PauseOverlayController.Resume().
 * - Conectar botao ReturnToMenu para PauseOverlayController.ReturnToMenuFrontend().
 *
 * NOTAS IMPORTANTES (NewScripts)
 * - O overlay PRECISA reagir aos eventos do GameLoop:
 *     - GamePauseCommandEvent(true) => mostrar overlay (apenas durante run ativa e antes do fim).
 *     - GameResumeRequestedEvent()  => ocultar overlay.
 * - Ao reagir a eventos, o overlay NÃO republica eventos (evita loop).
 */

using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Pause.Bindings
{
    /// <summary>
    /// Controlador do overlay de pausa no UIGlobal.
    ///
    /// Duas responsabilidades:
    /// 1) UI (overlayRoot + InputMode)
    /// 2) Publicar intenções via eventos quando acionado por UI (botões)
    ///
    /// Regras:
    /// - Mostrar overlay apenas durante run ativa (após GameRunStartedEvent) e antes do fim (antes de GameRunEndedEvent).
    /// - Pós-game usa overlay próprio; pausas de "freeze" no fim da run não devem abrir este overlay.
    ///
    /// Importante:
    /// - Quando reagindo a eventos (pause/resume), este controller NÃO republica eventos.
    ///   Só muda a UI e o InputMode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class PauseOverlayController : MonoBehaviour
    {
        private const string ExitToMenuReason = "PauseOverlay/ExitToMenu";
        [Header("Overlay")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private string showReason = "PauseOverlay/Show";
        [SerializeField] private string hideReason = "PauseOverlay/Hide";
        [SerializeField] private bool enableEscapeHotkey = true;

        
        private bool _dependenciesInjected;

        // Run lifecycle gating (mesma regra do Hotkey)
        private bool _runActive;
        private bool _runEnded;

        // Event bindings
        private EventBinding<GameRunStartedEvent> _onRunStarted;
        private EventBinding<GameRunEndedEvent> _onRunEnded;

        private EventBinding<GamePauseCommandEvent> _onPauseCommand;
        private EventBinding<GameResumeRequestedEvent> _onResumeRequested;
        private int _lastPauseFrame = -1;
        private string _lastPauseKey = string.Empty;
        private int _lastResumeFrame = -1;
        private string _lastResumeKey = string.Empty;
        private int _lastEscapeToggleFrame = -1;

        private void Awake()
        {
            // Run lifecycle
            _onRunStarted = new EventBinding<GameRunStartedEvent>(_ => OnRunStarted());
            _onRunEnded = new EventBinding<GameRunEndedEvent>(_ => OnRunEnded());

            EventBus<GameRunStartedEvent>.Register(_onRunStarted);
            EventBus<GameRunEndedEvent>.Register(_onRunEnded);

            // Pause/Resume -> UI reaction
            _onPauseCommand = new EventBinding<GamePauseCommandEvent>(OnPauseCommand);
            _onResumeRequested = new EventBinding<GameResumeRequestedEvent>(OnResumeRequested);

            EventBus<GamePauseCommandEvent>.Register(_onPauseCommand);
            EventBus<GameResumeRequestedEvent>.Register(_onResumeRequested);
        }

        private void Start()
        {
            EnsureDependenciesInjected();

            if (overlayRoot == null)
            {
                DebugUtility.LogWarning(typeof(PauseOverlayController),
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

            EventBus<GamePauseCommandEvent>.Unregister(_onPauseCommand);
            EventBus<GameResumeRequestedEvent>.Unregister(_onResumeRequested);
        }

        private void OnDisable()
        {
            // Safety net: se o controller sumir enquanto overlay estava ativo, publica Resume para não deixar pausa “presa”.
            // (Só faz sentido para teardown/disable inesperado do controller.)
            if (overlayRoot == null)
            {
                return;
            }
            if (!overlayRoot.activeSelf)
            {
                return;
            }

            overlayRoot.SetActive(false);

            try
            {
                EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
            }
            catch
            {
                // EventBus pode não estar disponível durante teardown; ignore.
            }

            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                "[PauseOverlay] OnDisable (safety) -> overlay desativado e GameResumeRequestedEvent publicado.",
                DebugUtility.Colors.Info);
        }

        // =========================
        // Public API (UI Buttons)
        // =========================

        /// <summary>
        /// Chamado por UI (ex.: botão Pause, se existir).
        /// Publica comando de pausa e ajusta UI/InputMode localmente.
        /// </summary>
        public void Show()
        {
            EnsureDependenciesInjected();

            // UI local primeiro (idempotente)
            if (!TrySetOverlayActive(true))
            {
                return;
            }

            // Publica intenção/comando
            EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(true));
            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                "[PauseOverlay] Show -> GamePauseCommandEvent publicado.",
                DebugUtility.Colors.Info);

            ApplyPauseInputMode();
        }

        /// <summary>
        /// Chamado por UI (botão Resume).
        /// Publica resume request e ajusta UI/InputMode localmente.
        /// </summary>
        public void Hide()
        {
            EnsureDependenciesInjected();

            if (!TrySetOverlayActive(false))
            {
                return;
            }

            EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                "[PauseOverlay] Hide -> GameResumeRequestedEvent publicado.",
                DebugUtility.Colors.Info);

            ApplyGameplayInputMode();
        }

        public void Resume() => Hide();

        /// <summary>
        /// Chamado por UI (botão ReturnToMenu).
        /// Publica intenção e deixa a bridge única navegar.
        /// </summary>
        public void ReturnToMenuFrontend()
        {
            EnsureDependenciesInjected();

            TrySetOverlayActive(false);

            EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent(ExitToMenuReason));
            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                $"[PauseOverlay] ReturnToMenuFrontend -> GameExitToMenuRequestedEvent publicado. reason='{ExitToMenuReason}'.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(InputModeRequestKind.FrontendMenu, "PauseOverlay/ReturnToMenuFrontend", "PauseOverlay"));
        }

        public void Toggle()
        {
            EnsureDependenciesInjected();

            bool isActive = overlayRoot != null && overlayRoot.activeSelf;
            if (isActive)
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

            bool willResume = overlayRoot != null && overlayRoot.activeSelf;

            _lastEscapeToggleFrame = frame;
            Toggle();

            DebugUtility.LogVerbose(typeof(PauseOverlayController),
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
            HideLocal("GameRunEnded", applyGameplayInputMode: false);
        }

        private void OnResumeRequested(GameResumeRequestedEvent evt)
        {
            string key = BuildResumeKey(evt);
            int frame = Time.frameCount;
            if (_lastResumeFrame == frame && string.Equals(_lastResumeKey, key, System.StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose(typeof(PauseOverlayController),
                    $"[OBS][GRS] GameResumeRequestedEvent dedupe_same_frame consumer='PauseOverlayController' key='{key}' frame='{frame}'",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastResumeFrame = frame;
            _lastResumeKey = key;
            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                $"[OBS][GRS] GameResumeRequestedEvent consumed consumer='PauseOverlayController' key='{key}' frame='{frame}'",
                DebugUtility.Colors.Info);

            HideLocal("ResumeRequested");
        }

        private void OnPauseCommand(GamePauseCommandEvent e)
        {
            string key = BuildPauseKey(e);
            int frame = Time.frameCount;
            if (_lastPauseFrame == frame && string.Equals(_lastPauseKey, key, System.StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose(typeof(PauseOverlayController),
                    $"[OBS][GRS] GamePauseCommandEvent dedupe_same_frame consumer='PauseOverlayController' key='{key}' frame='{frame}'",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastPauseFrame = frame;
            _lastPauseKey = key;
            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                $"[OBS][GRS] GamePauseCommandEvent consumed consumer='PauseOverlayController' key='{key}' frame='{frame}'",
                DebugUtility.Colors.Info);

            if (!_runActive || _runEnded)
            {
                HideLocal("PauseCommandIgnored_NotActiveOrEnded");
                return;
            }

            if (e.IsPaused)
            {
                ShowLocal("PauseCommand");
            }
            else
            {
                HideLocal("PauseCommandFalse");
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

            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                $"[PauseOverlay] ShowLocal (reason='{reason}').",
                DebugUtility.Colors.Info);

            ApplyPauseInputMode();
        }

        private void HideLocal(string reason, bool applyGameplayInputMode = true)
        {
            EnsureDependenciesInjected();

            if (overlayRoot != null && !overlayRoot.activeSelf)
            {
                return;
            }

            TrySetOverlayActive(false);

            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                $"[PauseOverlay] HideLocal (reason='{reason}').",
                DebugUtility.Colors.Info);

            if (applyGameplayInputMode)
            {
                ApplyGameplayInputMode();
            }
        }

        private void ApplyPauseInputMode()
        {
            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(InputModeRequestKind.PauseOverlay, showReason, "PauseOverlay"));
        }

        private void ApplyGameplayInputMode()
        {
            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(InputModeRequestKind.Gameplay, hideReason, "PauseOverlay"));
        }

        private static string BuildPauseKey(GamePauseCommandEvent evt)
        {
            bool isPaused = evt is { IsPaused: true };
            return $"pause|isPaused={isPaused}|reason=<null>";
        }

        private static string BuildResumeKey(GameResumeRequestedEvent evt)
        {
            return "resume|reason=<null>";
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
                DebugUtility.LogWarning(typeof(PauseOverlayController),
                    "[PauseOverlay] overlayRoot nao configurado. Operacao ignorada.");
                return false;
            }

            if (overlayRoot.activeSelf == active)
            {
                // Para reações (event-driven), isso é normal e desejável (idempotência).
                return false;
            }

            overlayRoot.SetActive(active);
            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                $"[PauseOverlay] Overlay {(active ? "ativado" : "desativado")}.",
                DebugUtility.Colors.Info);
            return true;
        }
    }
}













