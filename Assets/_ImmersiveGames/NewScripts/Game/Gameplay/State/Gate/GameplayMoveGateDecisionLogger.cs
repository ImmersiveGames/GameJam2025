using System;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.State.Core;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.State.Gate
{
    internal sealed class GameplayMoveGateDecisionLogger
    {
        private const float NonGameplayBlockedLogCooldownSeconds = 1f;

        private bool _moveLoggingArmed;
        private bool _hasMoveDecision;
        private StateDependentMoveDecision _lastMoveDecision = StateDependentMoveDecision.Allowed;
        private StateDependentServiceState _lastResolvedState = StateDependentServiceState.Ready;
        private string _lastLoopStateName = string.Empty;
        private float _lastNonGameplayBlockedLogTimestamp = float.NegativeInfinity;
        private string _lastNonGameplayBlockedLogKey = string.Empty;

        public void Arm()
        {
            _moveLoggingArmed = true;
        }

        public bool IsArmed => _moveLoggingArmed;

        public void LogIfChanged(
            ISimulationGateService gateService,
            GameplayStateSnapshot snapshot,
            StateDependentMoveDecision decision,
            StateDependentServiceState resolvedState,
            string loopStateName,
            bool force = false)
        {
            if (!_moveLoggingArmed)
            {
                return;
            }

            string effectiveLoopStateName = NormalizeLoopStateName(loopStateName);
            if (!force &&
                _hasMoveDecision &&
                decision == _lastMoveDecision &&
                resolvedState == _lastResolvedState &&
                string.Equals(effectiveLoopStateName, _lastLoopStateName, StringComparison.Ordinal))
            {
                return;
            }

            _hasMoveDecision = true;
            _lastMoveDecision = decision;
            _lastResolvedState = resolvedState;
            _lastLoopStateName = effectiveLoopStateName;

            bool gateIsOpen = gateService?.IsOpen ?? true;
            int activeTokens = gateService?.ActiveTokenCount ?? 0;
            bool pausedOnly = snapshot.IsPausedOnlyByGate(gateService);
            bool gameplayReady = snapshot.IsGameplayReadyOrUnknown;
            bool isNonGameplayContext = !gameplayReady || !string.Equals(_lastLoopStateName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);

            if (isNonGameplayContext && decision != StateDependentMoveDecision.Allowed)
            {
                float now = Time.unscaledTime;
                string effectiveCauseKey = $"{decision}|gate={gateIsOpen}|paused={pausedOnly}|state={resolvedState}|loop={_lastLoopStateName}";
                bool sameCauseWithinCooldown =
                    string.Equals(effectiveCauseKey, _lastNonGameplayBlockedLogKey, StringComparison.Ordinal) &&
                    (now - _lastNonGameplayBlockedLogTimestamp) < NonGameplayBlockedLogCooldownSeconds;

                if (sameCauseWithinCooldown)
                {
                    return;
                }

                _lastNonGameplayBlockedLogKey = effectiveCauseKey;
                _lastNonGameplayBlockedLogTimestamp = now;
            }
            else if (decision == StateDependentMoveDecision.Allowed)
            {
                _lastNonGameplayBlockedLogKey = string.Empty;
                _lastNonGameplayBlockedLogTimestamp = float.NegativeInfinity;
            }

            DebugUtility.LogVerbose<GameplayStateGate>(
                decision == StateDependentMoveDecision.Allowed
                    ? $"[StateDependent] Action 'Move' liberada (gateOpen={gateIsOpen}, gameplayReady={gameplayReady}, paused={pausedOnly}, serviceState={resolvedState}, gameLoopState='{_lastLoopStateName}', activeTokens={activeTokens})."
                    : $"[StateDependent] Action 'Move' bloqueada: {decision} (gateOpen={gateIsOpen}, gameplayReady={gameplayReady}, paused={pausedOnly}, serviceState={resolvedState}, gameLoopState='{_lastLoopStateName}', activeTokens={activeTokens}).");
        }

        private static string NormalizeLoopStateName(string loopStateName)
        {
            return string.IsNullOrWhiteSpace(loopStateName)
                ? string.Empty
                : loopStateName;
        }
    }
}
