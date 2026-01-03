/*
 * ChangeLog
 * - Ponte de pause: converte GamePauseCommandEvent/GameResumeRequestedEvent em gate SimulationGateTokens.Pause sem congelar física.
 * - Improvement: resolve gate sob demanda (lazy) para cenários em que DI global ainda não está pronto no ctor.
 * - Fix: ownership determinístico. Bridge NUNCA libera token que não foi adquirido por ela.
 * - Hardening: Release NÃO depende de gate resolvido (evita leak em teardown) e protege Provider nulo.
 * - Fix: Resume/ExitToMenu sem ownership não gera ruído ("release ignorado") — evento pode ocorrer fora do ciclo de pause.
 */

using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Infrastructure.Gate
{
    /// <summary>
    /// Bridge de infraestrutura: traduz eventos de pause/resume em tokens do SimulationGateService.
    /// Mantém idempotência e não altera timeScale/física.
    ///
    /// Contrato de ownership:
    /// - Ao pausar: adquire um handle e mantém referência.
    /// - Ao despausar: libera apenas o handle que ela adquiriu.
    /// - Nunca chama Release/ReleaseAll como fallback para evitar liberar token de terceiros.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GamePauseGateBridge : IDisposable
    {
        private const string PauseToken = SimulationGateTokens.Pause;

        private const string ReasonResumeRequested = "GameResumeRequestedEvent";
        private const string ReasonExitToMenuRequested = "GameExitToMenuRequestedEvent";

        private ISimulationGateService _gateService;

        private readonly EventBinding<GamePauseCommandEvent> _pauseBinding;
        private readonly EventBinding<GameResumeRequestedEvent> _resumeBinding;
        private readonly EventBinding<GameExitToMenuRequestedEvent> _exitToMenuBinding;

        private IDisposable _activeHandle;
        private bool _bindingsRegistered;
        private bool _loggedMissingGate;
        private bool _disposed;

        public GamePauseGateBridge(ISimulationGateService gateService)
        {
            _gateService = gateService;

            _pauseBinding = new EventBinding<GamePauseCommandEvent>(OnGamePause);
            _resumeBinding = new EventBinding<GameResumeRequestedEvent>(_ => ReleasePauseGate(ReasonResumeRequested));
            _exitToMenuBinding = new EventBinding<GameExitToMenuRequestedEvent>(_ => OnExitToMenuRequested());

            TryRegisterBindings();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_bindingsRegistered)
            {
                EventBus<GamePauseCommandEvent>.Unregister(_pauseBinding);
                EventBus<GameResumeRequestedEvent>.Unregister(_resumeBinding);
                EventBus<GameExitToMenuRequestedEvent>.Unregister(_exitToMenuBinding);
                _bindingsRegistered = false;
            }

            ReleasePauseGate("Dispose");
        }

        private void TryRegisterBindings()
        {
            try
            {
                EventBus<GamePauseCommandEvent>.Register(_pauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_resumeBinding);
                EventBus<GameExitToMenuRequestedEvent>.Register(_exitToMenuBinding);
                _bindingsRegistered = true;

                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    "[PauseBridge] Registrado nos eventos GamePauseCommandEvent/GameResumeRequestedEvent/GameExitToMenuRequestedEvent → SimulationGate.");
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<GamePauseGateBridge>(
                    $"[PauseBridge] EventBus indisponível; pause/resume não serão refletidos no gate ({ex.GetType().Name}).");
                _bindingsRegistered = false;
            }
        }

        private void OnGamePause(GamePauseCommandEvent evt)
        {
            bool shouldPause = evt is { IsPaused: true };
            if (shouldPause)
            {
                AcquirePauseGate();
            }
            else
            {
                ReleasePauseGate("GamePauseCommandEvent(paused=false)");
            }
        }

        private void OnExitToMenuRequested()
        {
            DebugUtility.LogVerbose<GamePauseGateBridge>(
                "[PauseBridge] ExitToMenu recebido -> liberando gate Pause (se adquirido por esta bridge).");
            ReleasePauseGate(ReasonExitToMenuRequested);
        }

        private void AcquirePauseGate()
        {
            if (!EnsureGateResolved())
            {
                LogGateUnavailable();
                return;
            }

            // Idempotência local: se já adquirimos neste "ciclo de pause", não adquirir de novo.
            if (_activeHandle != null)
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Acquire ignorado: handle já ativo. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
                return;
            }

            _activeHandle = _gateService.Acquire(PauseToken);

            DebugUtility.LogVerbose<GamePauseGateBridge>(
                $"[PauseBridge] Gate adquirido com token='{PauseToken}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
        }

        private void ReleasePauseGate(string reason)
        {
            // Ownership determinístico: só libera se temos handle.
            if (_activeHandle == null)
            {
                // Resume/ExitToMenu podem ser emitidos fora do ciclo de pause (ex.: UI/fluxos).
                // Sem handle => nada a fazer, e não é evidência de bug. Evitamos ruído.
                if (reason == ReasonResumeRequested || reason == ReasonExitToMenuRequested)
                {
                    return;
                }

                // Para outras origens, mantém log de diagnóstico (melhor esforço).
                if (_gateService != null)
                {
                    DebugUtility.LogVerbose<GamePauseGateBridge>(
                        $"[PauseBridge] Release ignorado ({reason}) — sem handle ativo (ownership inexistente). IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
                }
                else
                {
                    DebugUtility.LogVerbose<GamePauseGateBridge>(
                        $"[PauseBridge] Release ignorado ({reason}) — sem handle ativo (ownership inexistente).");
                }

                return;
            }

            try
            {
                _activeHandle.Dispose();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<GamePauseGateBridge>(
                    $"[PauseBridge] Erro ao liberar handle ativo ({reason}): {ex}");
            }
            finally
            {
                _activeHandle = null;
            }

            // Hardening: NÃO tenta resolver gate aqui só para logar snapshot (evita tocar DI em teardown).
            if (_gateService != null)
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Gate liberado ({reason}) token='{PauseToken}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
            }
            else
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Gate liberado ({reason}) token='{PauseToken}'. (gate indisponível para snapshot)");
            }
        }

        private bool EnsureGateResolved()
        {
            if (_gateService != null)
                return true;

            var provider = DependencyManager.Provider;
            if (provider == null)
                return false;

            if (provider.TryGetGlobal<ISimulationGateService>(out var resolved) && resolved != null)
            {
                _gateService = resolved;
                _loggedMissingGate = false;

                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    "[PauseBridge] ISimulationGateService resolvido via DependencyManager (lazy).");

                return true;
            }

            return false;
        }

        private void LogGateUnavailable()
        {
            if (_loggedMissingGate)
                return;

            DebugUtility.LogWarning<GamePauseGateBridge>(
                "[PauseBridge] ISimulationGateService indisponível; não é possível refletir pause/resume no gate.");
            _loggedMissingGate = true;
        }
    }
}
