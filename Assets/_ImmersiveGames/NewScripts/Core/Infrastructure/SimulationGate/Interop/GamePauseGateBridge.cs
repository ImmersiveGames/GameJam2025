/*
 * ChangeLog
 * - Ponte de pause: converte PauseStateChangedEvent em gate SimulationGateTokens.Pause sem congelar fisica.
 * - Improvement: resolve gate sob demanda (lazy) para cenarios em que DI global ainda nao esta pronto no ctor.
 * - Fix: ownership deterministico. Bridge NUNCA libera token que nao foi adquirido por ela.
 * - Hardening: Release NAO depende de gate resolvido (evita leak em teardown) e protege Provider nulo.
 */

using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.Core.Infrastructure.SimulationGate.Interop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GamePauseGateBridge : IDisposable
    {
        private const string PauseToken = SimulationGateTokens.Pause;
        private const string ReasonPauseStateChanged = "PauseStateChangedEvent";

        private ISimulationGateService _gateService;
        private readonly EventBinding<PauseStateChangedEvent> _pauseStateBinding;
        private IDisposable _activeHandle;
        private bool _bindingsRegistered;
        private bool _loggedMissingGate;
        private bool _disposed;

        public GamePauseGateBridge(ISimulationGateService gateService)
        {
            _gateService = gateService;
            _pauseStateBinding = new EventBinding<PauseStateChangedEvent>(OnPauseStateChanged);
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
                EventBus<PauseStateChangedEvent>.Unregister(_pauseStateBinding);
                _bindingsRegistered = false;
            }
            ReleasePauseGate("Dispose");
        }

        internal void ReleaseForExitToMenu(string reason)
        {
            ReleasePauseGate(reason);
        }

        private void TryRegisterBindings()
        {
            try
            {
                EventBus<PauseStateChangedEvent>.Register(_pauseStateBinding);
                _bindingsRegistered = true;

                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    "[PauseBridge] Registrado no evento PauseStateChangedEvent -> SimulationGate.");
            }
            catch (Exception ex)
            {
                _bindingsRegistered = false;
                DebugUtility.LogWarning<GamePauseGateBridge>(
                    $"[PauseBridge] EventBus indisponivel; pause nao sera refletido no gate ({ex.GetType().Name}).");
            }
        }

        private void OnPauseStateChanged(PauseStateChangedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            DebugUtility.LogVerbose<GamePauseGateBridge>(
                $"[OBS][GRS] PauseStateChangedEvent consumed consumer='{nameof(GamePauseGateBridge)}' isPaused='{evt.IsPaused}'",
                DebugUtility.Colors.Info);

            if (evt.IsPaused)
            {
                AcquirePauseGate();
            }
            else
            {
                ReleasePauseGate(ReasonPauseStateChanged);
            }
        }

        private void AcquirePauseGate()
        {
            if (!EnsureGateResolved())
            {
                LogGateUnavailable();
                return;
            }

            if (_activeHandle != null)
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Acquire ignorado: handle ja ativo. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
                return;
            }

            _activeHandle = _gateService.Acquire(PauseToken);

            DebugUtility.LogVerbose<GamePauseGateBridge>(
                $"[PauseBridge] Gate adquirido com token='{PauseToken}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
        }

        private void ReleasePauseGate(string reason)
        {
            if (_activeHandle == null)
            {
                if (_gateService != null)
                {
                    DebugUtility.LogVerbose<GamePauseGateBridge>(
                        $"[PauseBridge] Release ignorado ({reason}) - sem handle ativo (ownership inexistente). IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
                }
                else
                {
                    DebugUtility.LogVerbose<GamePauseGateBridge>(
                        $"[PauseBridge] Release ignorado ({reason}) - sem handle ativo (ownership inexistente).");
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

            if (_gateService != null)
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Gate liberado ({reason}) token='{PauseToken}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
            }
            else
            {
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Gate liberado ({reason}) token='{PauseToken}'. (gate indisponivel para snapshot)");
            }
        }

        private bool EnsureGateResolved()
        {
            if (_gateService != null)
            {
                return true;
            }

            var provider = DependencyManager.Provider;
            if (provider == null)
            {
                return false;
            }

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
            {
                return;
            }

            DebugUtility.LogWarning<GamePauseGateBridge>(
                "[PauseBridge] ISimulationGateService indisponivel; nao e possivel refletir pause no gate.");
            _loggedMissingGate = true;
        }
    }
}
