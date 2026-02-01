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

using _ImmersiveGames.NewScripts.Core.DebugLog;
using _ImmersiveGames.NewScripts.Core.DI;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.InputSystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Pause
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
    public sealed class PauseOverlayController : MonoBehaviour
    {
        private const string ExitToMenuReason = "PauseOverlay/ExitToMenu";
        private const string QaPauseReason = "QA/PauseOverlay/Show";
        private const string QaResumeReason = "QA/PauseOverlay/Hide";
        [Header("Overlay")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private string showReason = "PauseOverlay/Show";
        [SerializeField] private string hideReason = "PauseOverlay/Hide";

        [Inject] private IInputModeService _inputModeService;

        private bool _dependenciesInjected;

        // Run lifecycle gating (mesma regra do Hotkey)
        private bool _runActive;
        private bool _runEnded;

        // Event bindings
        private EventBinding<GameRunStartedEvent> _onRunStarted;
        private EventBinding<GameRunEndedEvent> _onRunEnded;

        private EventBinding<GamePauseCommandEvent> _onPauseCommand;
        private EventBinding<GameResumeRequestedEvent> _onResumeRequested;
        private EventBinding<GameExitToMenuRequestedEvent> _onExitToMenu;

        private void Awake()
        {
            // Run lifecycle
            _onRunStarted = new EventBinding<GameRunStartedEvent>(_ => OnRunStarted());
            _onRunEnded = new EventBinding<GameRunEndedEvent>(_ => OnRunEnded());

            EventBus<GameRunStartedEvent>.Register(_onRunStarted);
            EventBus<GameRunEndedEvent>.Register(_onRunEnded);

            // Pause/Resume/Exit -> UI reaction
            _onPauseCommand = new EventBinding<GamePauseCommandEvent>(OnPauseCommand);
            _onResumeRequested = new EventBinding<GameResumeRequestedEvent>(_ => OnResumeRequested());
            _onExitToMenu = new EventBinding<GameExitToMenuRequestedEvent>(_ => OnExitToMenu());

            EventBus<GamePauseCommandEvent>.Register(_onPauseCommand);
            EventBus<GameResumeRequestedEvent>.Register(_onResumeRequested);
            EventBus<GameExitToMenuRequestedEvent>.Register(_onExitToMenu);
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
            EventBus<GameExitToMenuRequestedEvent>.Unregister(_onExitToMenu);
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

            // Fecha o overlay (idempotente). Não publica resume aqui; quem coordena é o fluxo de saída.
            TrySetOverlayActive(false);

            EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent(ExitToMenuReason));
            DebugUtility.LogVerbose(typeof(PauseOverlayController),
                $"[PauseOverlay] ReturnToMenuFrontend -> GameExitToMenuRequestedEvent publicado. reason='{ExitToMenuReason}'.",
                DebugUtility.Colors.Info);

            if (_inputModeService != null)
            {
                _inputModeService.SetFrontendMenu("PauseOverlay/ReturnToMenuFrontend");
            }
            else
            {
                DebugUtility.LogWarning(typeof(PauseOverlayController),
                    "[PauseOverlay] IInputModeService indisponivel. Input nao sera alternado.");
            }
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

        // =========================
        // QA / Context Menu (Editor + DevBuild)
        // =========================
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("QA/Pause/Enter (TC: PauseOverlay)")]
        private void QaPause()
        {
            DebugUtility.Log(typeof(PauseOverlayController),
                $"[QA][PauseOverlay] Pause solicitado. reason='{QaPauseReason}'.",
                DebugUtility.Colors.Info);
            Show();
        }

        [ContextMenu("QA/Pause/Resume (TC: PauseOverlay Resume)")]
        private void QaResume()
        {
            DebugUtility.Log(typeof(PauseOverlayController),
                $"[QA][PauseOverlay] Resume solicitado. reason='{QaResumeReason}'.",
                DebugUtility.Colors.Info);
            Hide();
        }
#endif

        // =========================
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

        private void OnExitToMenu()
        {
            ResetFlagsForMenu();
            HideLocal("ExitToMenu");
        }

        private void OnResumeRequested()
        {
            // Resume sempre oculta overlay.
            HideLocal("ResumeRequested");
        }

        private void OnPauseCommand(GamePauseCommandEvent e)
        {
            // Pausa só deve abrir overlay durante run ativa e antes do final da run.
            // (No fim da run, GameRunStatusService publica pause para congelar simulação;
            // isso NÃO deve abrir PauseOverlay.)
            if (!_runActive || _runEnded)
            {
                // Mesmo se vier pause, garante overlay oculto.
                HideLocal("PauseCommandIgnored_NotActiveOrEnded");
                return;
            }

            if (e.IsPaused)
            {
                ShowLocal("PauseCommand");
            }
            else
            {
                // Suporte opcional caso alguma origem publique GamePauseCommandEvent(false).
                HideLocal("PauseCommandFalse");
            }
        }

        private void ResetFlagsForMenu()
        {
            _runActive = false;
            _runEnded = false;
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

        private void HideLocal(string reason)
        {
            HideLocal(reason, applyGameplayInputMode: true);
        }

        private void HideLocal(string reason, bool applyGameplayInputMode)
        {
            EnsureDependenciesInjected();

            // Idempotente: se já estiver oculto, não faz barulho.
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
            if (_inputModeService != null)
            {
                _inputModeService.SetPauseOverlay(showReason);
            }
            else
            {
                DebugUtility.LogWarning(typeof(PauseOverlayController),
                    "[PauseOverlay] IInputModeService indisponivel. Input nao sera alternado.");
            }
        }

        private void ApplyGameplayInputMode()
        {
            if (_inputModeService != null)
            {
                _inputModeService.SetGameplay(hideReason);
            }
            else
            {
                DebugUtility.LogWarning(typeof(PauseOverlayController),
                    "[PauseOverlay] IInputModeService indisponivel. Input nao sera alternado.");
            }
        }

        // =========================
        // DI + UI toggling
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
