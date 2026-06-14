using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public sealed class OperationalFactRecorder
    {
        private readonly SessionOperationalRuntimeState _state;
        private readonly string _sessionOperationalPipelineId;
        private readonly SessionOperationalStageOrderPolicy _stageOrderPolicy;

        public OperationalFactRecorder(
            SessionOperationalRuntimeState state,
            string sessionOperationalPipelineId,
            SessionOperationalStageOrderPolicy stageOrderPolicy)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _sessionOperationalPipelineId = Normalize(sessionOperationalPipelineId);
            _stageOrderPolicy = stageOrderPolicy ?? throw new ArgumentNullException(nameof(stageOrderPolicy));

            if (string.IsNullOrWhiteSpace(_sessionOperationalPipelineId))
            {
                throw new ArgumentException("sessionOperationalPipelineId is required.", nameof(sessionOperationalPipelineId));
            }
        }

        public bool TryRecordStage(
            SessionOperationalStage stage,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason,
            string message)
        {
            string normalizedRouteOperationId = Normalize(routeOperationId);
            string normalizedTransitionId = Normalize(transitionId);
            string normalizedRouteId = Normalize(routeId);
            string normalizedRouteProfileId = Normalize(routeProfileId);
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);
            string normalizedMessage = Normalize(message);

            if (stage == SessionOperationalStage.Unknown ||
                string.IsNullOrWhiteSpace(normalizedRouteOperationId) ||
                string.IsNullOrWhiteSpace(normalizedTransitionId) ||
                transitionSequence <= 0 ||
                string.IsNullOrWhiteSpace(normalizedRouteId) ||
                string.IsNullOrWhiteSpace(normalizedRouteProfileId) ||
                string.IsNullOrWhiteSpace(normalizedSource) ||
                string.IsNullOrWhiteSpace(normalizedReason))
            {
                return Reject(
                    SessionOperationalFactKind.IgnoredForeignOrStale,
                    stage,
                    normalizedSource,
                    normalizedReason,
                    $"Stage '{stage}' ignored because the identity payload is incomplete.");
            }

            bool isPrimaryStage = (int)stage <= 17;

            // Etapa 4: order and transition consistency enforcement for primary/macro stages now lives in the recorder
            // (using the injected policy). This consolidates the high-level fact orchestration that was duplicated
            // in the Pipeline's local wrapper. Granular stages bypass this (isPrimaryStage false).
            if (isPrimaryStage)
            {
                if (_state.HasStarted)
                {
                    if (normalizedRouteOperationId != _state.RouteOperationId ||
                        normalizedTransitionId != _state.TransitionId ||
                        transitionSequence != _state.TransitionSequence ||
                        normalizedRouteId != _state.RouteId ||
                        normalizedRouteProfileId != _state.RouteProfileId)
                    {
                        return Reject(
                            SessionOperationalFactKind.IgnoredForeignOrStale,
                            stage,
                            normalizedSource,
                            normalizedReason,
                            $"Stage '{stage}' ignored because it is foreign or stale (transition mismatch).");
                    }
                }

                if (!_state.HasStarted)
                {
                    if (!_stageOrderPolicy.CanStart(stage))
                    {
                        return Reject(
                            SessionOperationalFactKind.IgnoredForeignOrStale,
                            stage,
                            normalizedSource,
                            normalizedReason,
                            $"Stage '{stage}' ignored because it cannot start the operation.");
                    }
                }
                else if (_state.HasCompleted)
                {
                    return Reject(
                        SessionOperationalFactKind.IgnoredForeignOrStale,
                        stage,
                        normalizedSource,
                        normalizedReason,
                        $"Stage '{stage}' ignored because the operation is already completed.");
                }
                else if (!_stageOrderPolicy.CanAdvance(_state.CurrentStage, stage))
                {
                    return Reject(
                        SessionOperationalFactKind.IgnoredForeignOrStale,
                        stage,
                        normalizedSource,
                        normalizedReason,
                        $"Stage '{stage}' ignored because it is out of order.");
                }
            }

            SessionOperationalIdentity identity = new(
                _sessionOperationalPipelineId,
                normalizedRouteOperationId,
                normalizedTransitionId,
                transitionSequence,
                normalizedRouteId,
                normalizedRouteProfileId,
                normalizedSource,
                normalizedReason,
                stage);

            SessionOperationalFact fact = new(
                MapFactKind(stage),
                identity,
                normalizedSource,
                normalizedReason,
                normalizedMessage);

            if (!fact.IsValid)
            {
                throw new InvalidOperationException($"Cannot emit invalid operational fact for stage '{stage}'.");
            }

            // Etapa 3 fix: only primary/macro stages (original enum <=17) update the high-level CurrentStage and machine.
            // Granular per-op stages (Fade etc >=18, added for fact canonization) still record fact+trace for observability
            // but do not pollute _state.CurrentStage / order policy used by completion logic.
            if (isPrimaryStage)
            {
                _state.SetCurrentIdentity(identity);
                _state.MarkStarted();
            }

            _state.AppendFact(fact);
            _state.AppendTrace(
                $"fact='{fact.Kind}' stage='{fact.Identity.Stage}' routeOperationId='{fact.Identity.RouteOperationId}' transitionId='{fact.Identity.TransitionId}' transitionSequence='{fact.Identity.TransitionSequence}' routeId='{fact.Identity.RouteId}' routeProfileId='{fact.Identity.RouteProfileId}' source='{fact.Source}' reason='{fact.Reason}' message='{fact.Message}'");

            if (stage == SessionOperationalStage.InputCapabilityPrepared ||
                stage == SessionOperationalStage.InitialInputModePrepared)
            {
                _state.AppendTrace(
                    $"fact='{fact.Kind}' stage='{fact.Identity.Stage}' routeId='{fact.Identity.RouteId}' routeProfileId='{fact.Identity.RouteProfileId}' operationalSurfaceKind='{Normalize(_state.RouteClass)}' inputPolicy='{_state.CurrentInputPolicy}' inputMode='{_state.CurrentInitialInputMode}' source='{fact.Source}' reason='{fact.Reason}'");
            }

            if (stage == SessionOperationalStage.Completed)
            {
                _state.MarkCompleted();
            }

            return true;
        }


        public void SetInputModeContext(
            string routeClass,
            SessionOperationalInputPolicy inputPolicy,
            SessionOperationalInputModeKind initialInputMode)
        {
            _state.SetInputModeContext(Normalize(routeClass), inputPolicy, initialInputMode);
        }

        public SessionOperationalIdentity CurrentIdentity => _state.CurrentIdentity;
        public System.Collections.Generic.IReadOnlyList<SessionOperationalFact> Facts => _state.Facts;

        public bool TryRecordInputStage(
            SessionOperationalStage stage,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string routeClass,
            SessionOperationalInputPolicy inputPolicy,
            SessionOperationalInputModeKind initialInputMode,
            string source,
            string reason,
            string message)
        {
            SetInputModeContext(routeClass, inputPolicy, initialInputMode);
            return TryRecordStage(
                stage,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                message);
        }

        public bool Reject(
            SessionOperationalFactKind factKind,
            SessionOperationalStage stage,
            string source,
            string reason,
            string message)
        {
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);
            string normalizedMessage = Normalize(message);

            SessionOperationalIdentity identity = new(
                _sessionOperationalPipelineId,
                _state.RouteOperationId,
                _state.TransitionId,
                _state.TransitionSequence,
                _state.RouteId,
                _state.RouteProfileId,
                normalizedSource,
                normalizedReason,
                stage);

            if (!identity.IsValid)
            {
                _state.AppendTrace(
                    $"rejected_stage='{stage}' source='{normalizedSource}' reason='{normalizedReason}' message='{normalizedMessage}'");
                return false;
            }

            SessionOperationalFact fact = new(factKind, identity, normalizedSource, normalizedReason, normalizedMessage);
            _state.AppendFact(fact);
            _state.AppendTrace(
                $"fact='{fact.Kind}' stage='{fact.Identity.Stage}' source='{fact.Source}' reason='{fact.Reason}' message='{fact.Message}'");
            return false;
        }

        public string DumpState()
        {
            return $"pipelineId='{_state.SessionOperationalPipelineId}' routeOperationId='{_state.RouteOperationId}' transitionId='{_state.TransitionId}' transitionSequence='{_state.TransitionSequence}' routeId='{_state.RouteId}' routeProfileId='{_state.RouteProfileId}' routeClass='{_state.RouteClass}' inputPolicy='{_state.CurrentInputPolicy}' initialInputMode='{_state.CurrentInitialInputMode}' stage='{_state.CurrentStage}' started='{_state.HasStarted}' completed='{_state.HasCompleted}' factsCount='{_state.Facts.Count}'";
        }

        // Etapa 3: helpers for per-operation fact recording (canonization - stages should use these or TryRecordStage)
        public bool TryRecordOperationStage(SessionOperationalStage stage, string source, string reason, string message)
        {
            var id = CurrentIdentity;
            if (!id.IsValid)
            {
                return false;
            }
            return TryRecordStage(
                stage,
                id.RouteOperationId,
                id.TransitionId,
                id.TransitionSequence,
                id.RouteId,
                id.RouteProfileId,
                source,
                reason,
                message);
        }

        // Etapa 5: enhanced helper to centralize emission of canonical operational signals (fact + rich log).
        // This eliminates the duplication of _factRecorder call + separate DebugUtility.Log* in each stage.
        // New stages only need to call this with the full rich "OperationalXXX..." message (as used in the README checklist).
        // The log is emitted from the central place (recorder), trace gets the rich message too.
        public bool TryRecordOperationStage(SessionOperationalStage stage, string source, string reason, string richLogMessage, Type ownerType, string color = null)
        {
            bool recorded = TryRecordOperationStage(stage, source, reason, richLogMessage);
            if (recorded)
            {
                DebugUtility.LogVerbose(ownerType, richLogMessage, color ?? DebugUtility.Colors.Info);
            }
            return recorded;
        }
        private static SessionOperationalFactKind MapFactKind(SessionOperationalStage stage)
        {
            return stage switch
            {
                SessionOperationalStage.RouteOperationStarted => SessionOperationalFactKind.RouteOperationStarted,
                SessionOperationalStage.NavigationIntentObserved => SessionOperationalFactKind.NavigationIntentObserved,
                SessionOperationalStage.RouteResolved => SessionOperationalFactKind.RouteResolved,
                SessionOperationalStage.TransitionRequested => SessionOperationalFactKind.TransitionRequested,
                SessionOperationalStage.TransitionStarted => SessionOperationalFactKind.TransitionStarted,
                SessionOperationalStage.CurtainClosed => SessionOperationalFactKind.CurtainClosed,
                SessionOperationalStage.PreviousRouteTeardownSkipped => SessionOperationalFactKind.PreviousRouteTeardownSkipped,
                SessionOperationalStage.RoutePhysicalApplyObserved => SessionOperationalFactKind.RoutePhysicalApplyObserved,
                SessionOperationalStage.ScenesReadyObserved => SessionOperationalFactKind.ScenesReadyObserved,
                SessionOperationalStage.SessionOperationalSetupNoOp => SessionOperationalFactKind.SessionOperationalSetupNoOp,
                SessionOperationalStage.PlayerParticipationSeedObserved => SessionOperationalFactKind.PlayerParticipationSeedObserved,
                SessionOperationalStage.InputCapabilityPrepared => SessionOperationalFactKind.InputCapabilityPrepared,
                SessionOperationalStage.InitialInputModePrepared => SessionOperationalFactKind.InitialInputModePrepared,
                SessionOperationalStage.PauseCapabilityPrepared => SessionOperationalFactKind.PauseCapabilityPrepared,
                SessionOperationalStage.ReadyToOpenCurtain => SessionOperationalFactKind.ReadyToOpenCurtain,
                SessionOperationalStage.TransitionCompletedObserved => SessionOperationalFactKind.TransitionCompletedObserved,
                SessionOperationalStage.Completed => SessionOperationalFactKind.Completed,
                // Etapa 3: map new granular stages
                SessionOperationalStage.Fade => SessionOperationalFactKind.Fade,
                SessionOperationalStage.SceneComposition => SessionOperationalFactKind.SceneComposition,
                SessionOperationalStage.HandoffExit => SessionOperationalFactKind.HandoffExit,
                SessionOperationalStage.RouteAudio => SessionOperationalFactKind.RouteAudio,
                SessionOperationalStage.RouteCameraPresentation => SessionOperationalFactKind.RouteCameraPresentation,
                SessionOperationalStage.ActivityCameraPresentation => SessionOperationalFactKind.ActivityCameraPresentation,
                SessionOperationalStage.ConsumerEntryAndReadiness => SessionOperationalFactKind.ConsumerEntryAndReadiness,
                SessionOperationalStage.PlayerParticipation => SessionOperationalFactKind.PlayerParticipation,
                SessionOperationalStage.Loading => SessionOperationalFactKind.Loading,
                SessionOperationalStage.TransitionBlackout => SessionOperationalFactKind.TransitionBlackout,
                SessionOperationalStage.RouteReveal => SessionOperationalFactKind.RouteReveal,
                SessionOperationalStage.RouteSetup => SessionOperationalFactKind.RouteSetup,
                _ => SessionOperationalFactKind.Unknown
            };
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

