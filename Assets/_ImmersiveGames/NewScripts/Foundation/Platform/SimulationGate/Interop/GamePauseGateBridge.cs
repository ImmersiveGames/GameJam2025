using System;
using ImmersiveGames.GameJam2025.Core.Events;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Orchestration.GameLoop.RunLifecycle.Core;

namespace ImmersiveGames.GameJam2025.Infrastructure.SimulationGate.Interop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GamePauseGateBridge : IDisposable
    {
        private const string PauseToken = SimulationGateTokens.Pause;
        private const string ReasonPauseStateChanged = "PauseStateChangedEvent";

        private readonly ISimulationGateService _gateService;
        private readonly EventBinding<PauseStateChangedEvent> _pauseStateBinding;
        private IDisposable _activeHandle;
        private bool _bindingsRegistered;
        private bool _disposed;

        // Bridge legitima: conecta o gate global ao estado de pause emitido pelo GameLoopService.
        // Nasce no GameLoopBootstrap, depois que o GameLoopService ja foi composto.
        public GamePauseGateBridge(ISimulationGateService gateService)
        {
            _gateService = gateService ?? throw new InvalidOperationException("[FATAL][Config][PauseBridge] ISimulationGateService obrigatorio ausente para GamePauseGateBridge.");
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
                    "[PauseBridge] Registrado no evento PauseStateChangedEvent -> SimulationGate com owner ISimulationGateService explícito.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("[FATAL][Config][PauseBridge] Falha ao registrar GamePauseGateBridge no EventBus.", ex);
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
                DebugUtility.LogVerbose<GamePauseGateBridge>(
                    $"[PauseBridge] Release ignorado ({reason}) - sem handle ativo (ownership inexistente). IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
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

            DebugUtility.LogVerbose<GamePauseGateBridge>(
                $"[PauseBridge] Gate liberado ({reason}) token='{PauseToken}'. IsOpen={_gateService.IsOpen} Active={_gateService.ActiveTokenCount}");
        }
    }
}

