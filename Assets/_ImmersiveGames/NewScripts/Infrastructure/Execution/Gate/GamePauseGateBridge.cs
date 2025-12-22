/*
 * ChangeLog
 * - Nova ponte de pause: converte GamePauseEvent/GameResumeRequestedEvent em gate SimulationGateTokens.Pause sem congelar física.
 */
using System;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate
{
    /// <summary>
    /// Bridge de infraestrutura: traduz eventos de pause/resume em tokens do SimulationGateService.
    /// Mantém idempotência e não altera timeScale/física.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GamePauseGateBridge : IDisposable
    {
        private const string Token = SimulationGateTokens.Pause;

        private readonly ISimulationGateService _gateService;
        private readonly EventBinding<GamePauseEvent> _pauseBinding;
        private readonly EventBinding<GameResumeRequestedEvent> _resumeBinding;

        private IDisposable _activeHandle;
        private bool _bindingsRegistered;
        private bool _loggedMissingGate;
        private bool _disposed;

        public GamePauseGateBridge(ISimulationGateService gateService)
        {
            _gateService = gateService ?? ResolveGateService();

            _pauseBinding = new EventBinding<GamePauseEvent>(OnGamePause);
            _resumeBinding = new EventBinding<GameResumeRequestedEvent>(_ => ReleasePauseGate("GameResumeRequestedEvent"));

            TryRegisterBindings();
        }

        private ISimulationGateService ResolveGateService()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var resolved) && resolved != null)
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    "[PauseBridge] ISimulationGateService resolvido via DependencyManager.");
                return resolved;
            }

            DebugUtility.LogWarning<GamePauseGateBridge>(
                "[PauseBridge] ISimulationGateService indisponível; bridge ficará inativo até que um gate seja registrado.");
            _loggedMissingGate = true;
            return null;
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
                EventBus<GamePauseEvent>.Unregister(_pauseBinding);
                EventBus<GameResumeRequestedEvent>.Unregister(_resumeBinding);
                _bindingsRegistered = false;
            }

            ReleasePauseGate("Dispose");
        }

        private void TryRegisterBindings()
        {
            try
            {
                EventBus<GamePauseEvent>.Register(_pauseBinding);
                EventBus<GameResumeRequestedEvent>.Register(_resumeBinding);
                _bindingsRegistered = true;

                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    "[PauseBridge] Registrado nos eventos GamePauseEvent/GameResumeRequestedEvent → SimulationGate.");
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<GamePauseGateBridge>(
                    $"[PauseBridge] EventBus indisponível; pause/resume não serão refletidos no gate ({ex.GetType().Name}).");
                _bindingsRegistered = false;
            }
        }

        private void OnGamePause(GamePauseEvent evt)
        {
            var shouldPause = evt is { IsPaused: true };
            if (shouldPause)
            {
                AcquirePauseGate();
            }
            else
            {
                ReleasePauseGate("GamePauseEvent(paused=false)");
            }
        }

        private void AcquirePauseGate()
        {
            if (_gateService == null)
            {
                LogGateUnavailable();
                return;
            }

            if (_gateService.IsTokenActive(Token))
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Gate já bloqueado com token='{Token}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
                return;
            }

            ReleaseHandle();
            _activeHandle = _gateService.Acquire(Token);

            DebugUtility.LogVerbose<GamePauseGateBridge>(
                $"[PauseBridge] Gate adquirido com token='{Token}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
        }

        private void ReleasePauseGate(string reason)
        {
            if (_gateService == null)
            {
                LogGateUnavailable();
                return;
            }

            if (!_gateService.IsTokenActive(Token))
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Release ignorado ({reason}) — token '{Token}' não está ativo. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
                ReleaseHandle();
                return;
            }

            if (_activeHandle != null)
            {
                _activeHandle.Dispose();
            }
            else
            {
                _gateService.Release(Token);
            }

            _activeHandle = null;

            DebugUtility.LogVerbose<GamePauseGateBridge>(
                $"[PauseBridge] Gate liberado ({reason}) token='{Token}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
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
