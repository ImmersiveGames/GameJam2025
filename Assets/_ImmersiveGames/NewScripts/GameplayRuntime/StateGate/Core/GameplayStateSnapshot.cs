using System;
using ImmersiveGames.GameJam2025.Infrastructure.SimulationGate;
using ImmersiveGames.GameJam2025.Orchestration.GameLoop.RunLifecycle.Core;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Readiness.Runtime;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.State.Core
{
    internal enum StateDependentServiceState
    {
        Ready,
        Playing,
        Paused
    }

    internal enum StateDependentMoveDecision
    {
        Allowed,
        GateClosed,
        Paused,
        GameplayNotReady,
        NotPlaying
    }

    internal sealed class GameplayStateSnapshot
    {
        private StateDependentServiceState _state = StateDependentServiceState.Ready;
        private bool _hasReadinessSnapshot;
        private bool _gameplayReady;

        private int _lastResetFrame = -1;
        private string _lastResetReason = string.Empty;
        public bool IsGameplayReadyOrUnknown => !_hasReadinessSnapshot || _gameplayReady;

        public void SetState(StateDependentServiceState next)
        {
            _state = next;
        }

        public void UpdateReadiness(ReadinessChangedEvent evt)
        {
            _hasReadinessSnapshot = true;
            _gameplayReady = evt.Snapshot.GameplayReady;
        }

        public bool TryConsumeReset(string reason, int frame)
        {
            if (_lastResetFrame == frame && string.Equals(_lastResetReason, reason, StringComparison.Ordinal))
            {
                return false;
            }

            _lastResetFrame = frame;
            _lastResetReason = reason;
            return true;
        }

        public StateDependentServiceState ResolveServiceState(ISimulationGateService gateService, IGameLoopService gameLoopService)
        {
            if (IsPausedOnlyByGate(gateService))
            {
                return StateDependentServiceState.Paused;
            }

            if (!IsInfraReady(gateService))
            {
                return StateDependentServiceState.Ready;
            }

            StateDependentServiceState? loopState = ResolveFromGameLoop(gameLoopService);
            if (loopState.HasValue)
            {
                return loopState.Value;
            }

            return _state;
        }

        public bool IsPausedOnlyByGate(ISimulationGateService gateService)
        {
            if (gateService == null)
            {
                return false;
            }

            if (!gateService.IsTokenActive(SimulationGateTokens.Pause))
            {
                return false;
            }

            return gateService.ActiveTokenCount == 1;
        }

        public bool IsInfraReady(ISimulationGateService gateService)
        {
            if (gateService is { IsOpen: false })
            {
                return false;
            }

            if (_hasReadinessSnapshot && !_gameplayReady)
            {
                return false;
            }

            return true;
        }

        public bool EvaluateMoveAllowed(
            ISimulationGateService gateService,
            IGameLoopService gameLoopService,
            out StateDependentMoveDecision decision,
            out StateDependentServiceState resolvedState,
            out string loopStateName)
        {
            resolvedState = ResolveServiceState(gateService, gameLoopService);
            loopStateName = gameLoopService?.CurrentStateIdName ?? string.Empty;

            if (gateService is { IsOpen: false })
            {
                decision = IsPausedOnlyByGate(gateService) ? StateDependentMoveDecision.Paused : StateDependentMoveDecision.GateClosed;
                return false;
            }

            if (_hasReadinessSnapshot && !_gameplayReady)
            {
                decision = StateDependentMoveDecision.GameplayNotReady;
                return false;
            }

            if (resolvedState != StateDependentServiceState.Playing)
            {
                decision = StateDependentMoveDecision.NotPlaying;
                return false;
            }

            decision = StateDependentMoveDecision.Allowed;
            return true;
        }

        private static StateDependentServiceState? ResolveFromGameLoop(IGameLoopService gameLoopService)
        {
            if (gameLoopService == null)
            {
                return null;
            }

            string name = gameLoopService.CurrentStateIdName;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return name switch
            {
                nameof(GameLoopStateId.Playing) => StateDependentServiceState.Playing,
                nameof(GameLoopStateId.Paused) => StateDependentServiceState.Paused,
                nameof(GameLoopStateId.Ready) => StateDependentServiceState.Ready,
                nameof(GameLoopStateId.Boot) => StateDependentServiceState.Ready,
                nameof(GameLoopStateId.RunEnded) => StateDependentServiceState.Ready,
                _ => null
            };
        }
    }
}

