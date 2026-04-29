using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Context;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.StateGate.Core
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
        private bool _hasSceneReadinessSnapshot;
        private bool _sceneGameplayReady;
        private bool _hasActorsOperationalReadinessSnapshot;
        private bool _actorsOperationalReady;
        private bool _hasGameplayInteractionReadinessSnapshot;
        private bool _gameplayInteractionReady;
        private string _interactionReadinessReason = string.Empty;
        private bool _hasGameRunStarted;

        private int _lastResetFrame = -1;
        private string _lastResetReason = string.Empty;
        public bool HasSceneReadinessSnapshot => _hasSceneReadinessSnapshot;
        public bool IsSceneGameplayReady => _hasSceneReadinessSnapshot && _sceneGameplayReady;
        public bool IsSceneGameplayReadyOrUnknown => !_hasSceneReadinessSnapshot || _sceneGameplayReady;
        public bool HasActorsOperationalReadinessSnapshot => _hasActorsOperationalReadinessSnapshot;
        public bool IsActorsOperationalReady => _hasActorsOperationalReadinessSnapshot && _actorsOperationalReady;
        public bool HasGameplayInteractionReadinessSnapshot => _hasGameplayInteractionReadinessSnapshot;
        public bool IsGameplayInteractionReady => _hasGameplayInteractionReadinessSnapshot && _gameplayInteractionReady;
        public bool HasGameRunStarted => _hasGameRunStarted;
        public bool IsPaused => _state == StateDependentServiceState.Paused;

        public void SetState(StateDependentServiceState next)
        {
            _state = next;
        }

        public void SetGameRunStarted()
        {
            _hasGameRunStarted = true;
            _state = StateDependentServiceState.Playing;
        }

        public void SetGameRunEnded()
        {
            _hasGameRunStarted = false;
            _state = StateDependentServiceState.Ready;
        }

        public void UpdateSceneReadiness(ReadinessChangedEvent evt)
        {
            _hasSceneReadinessSnapshot = true;
            _sceneGameplayReady = evt.Snapshot.GameplayReady;
        }

        public void UpdateGameplayInteractionReadiness(GameplayInteractionReadinessSnapshot snapshot)
        {
            _hasActorsOperationalReadinessSnapshot = snapshot.HasActorsOperationalReadyObservation;
            _actorsOperationalReady = snapshot.ActorsOperationalReady;
            _hasGameplayInteractionReadinessSnapshot = snapshot.HasCanonicalPayload ||
                                                       snapshot.HasActorsOperationalReadyObservation ||
                                                       snapshot.HasSceneTransitionCompletedObservation ||
                                                       snapshot.HasIntroStageStatusObservation;
            _gameplayInteractionReady = snapshot.IsGameplayInteractionReady;
            _interactionReadinessReason = snapshot.ReadinessReason;

            // SceneFlow readiness tecnico nao deve ser derrubado por snapshot de interacao sem observacao de cena.
            // Mismatch de interacao fecha o gate via _gameplayInteractionReady, preservando sceneReady quando SceneFlow ja completou.
            if (snapshot.HasSceneTransitionCompletedObservation)
            {
                _hasSceneReadinessSnapshot = true;
                _sceneGameplayReady = snapshot.SceneTransitionCompleted;
            }
        }

        public string DescribeGameplayReadinessReason()
        {
            if (!_hasGameplayInteractionReadinessSnapshot || !_gameplayInteractionReady)
            {
                return string.IsNullOrWhiteSpace(_interactionReadinessReason)
                    ? "waiting_for_gameplay_interaction_ready"
                    : _interactionReadinessReason;
            }

            if (!_hasGameRunStarted)
            {
                return "waiting_for_game_run_started";
            }

            return "ready";
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
            if (loopState == StateDependentServiceState.Paused)
            {
                return StateDependentServiceState.Paused;
            }

            if (_hasGameRunStarted || loopState == StateDependentServiceState.Playing)
            {
                if (loopState.HasValue)
                {
                    return loopState.Value;
                }

                return StateDependentServiceState.Playing;
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

            if (!_hasGameplayInteractionReadinessSnapshot || !_gameplayInteractionReady)
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

            if (!_hasGameplayInteractionReadinessSnapshot || !_gameplayInteractionReady)
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

