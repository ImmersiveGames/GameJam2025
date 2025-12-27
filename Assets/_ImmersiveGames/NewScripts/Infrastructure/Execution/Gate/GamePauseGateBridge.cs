/*
 * ChangeLog
 * - Ponte de pause: converte GamePauseCommandEvent/GameResumeRequestedEvent em gate SimulationGateTokens.Pause sem congelar física.
 * - Improvement: resolve gate sob demanda (lazy) para cenários em que DI global ainda não está pronto no ctor.
 */
using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate
{
    /// <summary>
    /// Bridge de infraestrutura: traduz eventos de pause/resume em tokens do SimulationGateService.
    /// Mantém idempotência e não altera timeScale/física.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GamePauseGateBridge : IDisposable
    {
        private const string PauseToken = SimulationGateTokens.Pause;

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
            _resumeBinding = new EventBinding<GameResumeRequestedEvent>(_ => ReleasePauseGate("GameResumeRequestedEvent"));
            _exitToMenuBinding = new EventBinding<GameExitToMenuRequestedEvent>(_ => OnExitToMenuRequested());

            TryRegisterBindings();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
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
            var shouldPause = evt is { IsPaused: true };
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
                "[PauseBridge] ExitToMenu recebido -> liberando gate Pause.");
            ReleasePauseGate("GameExitToMenuRequestedEvent");
        }

        private void AcquirePauseGate()
        {
            if (!EnsureGateResolved())
            {
                LogGateUnavailable();
                return;
            }

            if (_gateService.IsTokenActive(PauseToken))
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Gate já bloqueado com token='{PauseToken}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
                return;
            }

            ReleaseHandle();
            _activeHandle = _gateService.Acquire(PauseToken);

            DebugUtility.LogVerbose<GamePauseGateBridge>(
                $"[PauseBridge] Gate adquirido com token='{PauseToken}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
        }

        private void ReleasePauseGate(string reason)
        {
            if (!EnsureGateResolved())
            {
                LogGateUnavailable();
                return;
            }

            if (!_gateService.IsTokenActive(PauseToken))
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Release ignorado ({reason}) — token '{PauseToken}' não está ativo. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
                ReleaseHandle();
                return;
            }

            if (_activeHandle != null)
            {
                _activeHandle.Dispose();
            }
            else
            {
                _gateService.Release(PauseToken);
            }

            _activeHandle = null;

            DebugUtility.LogVerbose<GamePauseGateBridge>(
                $"[PauseBridge] Gate liberado ({reason}) token='{PauseToken}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
        }

        private bool EnsureGateResolved()
        {
            if (_gateService != null)
            {
                return true;
            }

            if (DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var resolved) && resolved != null)
            {
                _gateService = resolved;
                _loggedMissingGate = false;

                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    "[PauseBridge] ISimulationGateService resolvido via DependencyManager (lazy).");

                return true;
            }

            return false;
        }

        private void ReleaseHandle()
        {
            if (_activeHandle == null)
            {
                return;
            }

            try
            {
                _activeHandle.Dispose();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<GamePauseGateBridge>(
                    $"[PauseBridge] Erro ao liberar handle ativo: {ex}");
            }
            finally
            {
                _activeHandle = null;
            }
        }

        private void LogGateUnavailable()
        {
            if (_loggedMissingGate)
            {
                return;
            }

            DebugUtility.LogWarning<GamePauseGateBridge>(
                "[PauseBridge] ISimulationGateService indisponível; não é possível refletir pause/resume no gate.");
            _loggedMissingGate = true;
        }
    }
}
