using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.SimulationGate;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    public sealed class SessionActivityPipeline : ISessionActivityEntryHandoffReceiver
    {
        private const string PipelineId = "SessionActivityPipeline.Base11.Sandbox";
        private readonly SessionActivityCatalog _catalog;
        private readonly SessionActivityRuntimeState _state;
        private readonly SimulationGateService _simulationGate;
        private readonly ISessionActivityPauseOverlayAdapter _pauseOverlayAdapter;
        private readonly ISessionActivityInputModeAdapter _inputModeAdapter;
        private readonly string _sessionId;

        public SessionActivityPipeline(
            SessionActivityCatalog catalog,
            string sessionStateId,
            ISessionActivityPauseOverlayAdapter pauseOverlayAdapter,
            ISessionActivityInputModeAdapter inputModeAdapter)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _state = new SessionActivityRuntimeState();
            _simulationGate = new SimulationGateService();
            _pauseOverlayAdapter = pauseOverlayAdapter ?? throw new ArgumentNullException(nameof(pauseOverlayAdapter));
            _inputModeAdapter = inputModeAdapter ?? throw new ArgumentNullException(nameof(inputModeAdapter));
            _sessionId = Normalize(sessionStateId);

            if (string.IsNullOrWhiteSpace(_sessionId))
            {
                throw new ArgumentException("sessionStateId is required.", nameof(sessionStateId));
            }

            if (!_catalog.TryGetFirst(out SessionActivityDefinition firstDefinition) || !firstDefinition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityCatalog requires Activity 01.");
            }
        }

        public SessionActivityRuntimeState State => _state;
        public SessionActivityCatalog Catalog => _catalog;
        public SimulationGateState GateState => _simulationGate.State;
        public string SessionId => _sessionId;

        public SessionActivityCommand BuildStartCommand(string source, string reason)
        {
            if (!_catalog.TryGetFirst(out SessionActivityDefinition firstDefinition) || !firstDefinition.IsValid)
            {
                throw new InvalidOperationException("Activity 01 is required to start the pipeline.");
            }

            return new SessionActivityCommand(
                SessionActivityCommandKind.StartActivity,
                BuildIdentity(firstDefinition, SessionActivityStage.ActivationExecuting, 1),
                source,
                reason);
        }

        public SessionActivityCommand BuildCompleteCurrentActivityCommand(string source, string reason)
        {
            EnsureStartedOrFail("CompleteCurrentActivityCommand");
            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteCurrentActivity,
                _state.CurrentIdentity,
                source,
                reason);
        }

        public SessionActivityCommand BuildContinueToNextActivityCommand(string source, string reason)
        {
            EnsureStartedOrFail("ContinueToNextActivityCommand");
            if (!_state.CurrentHandoff.IsValid)
            {
                throw new InvalidOperationException("No handoff is available to continue.");
            }

            return new SessionActivityCommand(
                SessionActivityCommandKind.ContinueToNextActivity,
                _state.CurrentHandoff.ToIdentity,
                source,
                reason);
        }

        public SessionActivityCommandResult Start(string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.StartActivity, source, reason);
            }

            if (_state.HasStarted)
            {
                return RejectStartCommand(source, reason);
            }

            return Execute(BuildStartCommand(source, reason));
        }

        public SessionActivityCommandResult DebugStartActivity(string source, string reason)
        {
            return Start(source, reason);
        }

        public SessionActivityCommandResult StartFromPreparedHandoff(
            SessionActivityEntryHandoff handoff,
            string source,
            string reason)
        {
            if (!handoff.IsValid)
            {
                throw new InvalidOperationException("SessionActivityEntryHandoff is invalid.");
            }

            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.StartActivity, source, reason);
            }

            if (_state.HasStarted)
            {
                return RejectStartCommand(source, reason);
            }

            if (!string.Equals(handoff.SessionStateId, _sessionId, StringComparison.Ordinal))
            {
                return RejectPreparedHandoff(
                    handoff,
                    source,
                    reason,
                    "stale_or_foreign_handoff",
                    $"Prepared handoff session state '{handoff.SessionStateId}' does not match pipeline session '{_sessionId}'.");
            }

            SessionActivityDefinition initialDefinition = handoff.HasResolvedActivity
                ? ResolveActivityByIdOrFail(handoff.ActivityId)
                : ResolveActivityByOrdinalOrFail(1);

            if (handoff.HasResolvedActivity && initialDefinition.ActivityOrdinal != handoff.ActivityOrdinal)
            {
                throw new InvalidOperationException($"Prepared handoff activity ordinal mismatch. expected='{initialDefinition.ActivityOrdinal}' got='{handoff.ActivityOrdinal}'.");
            }

            int entrySequence = ResolveNextEntrySequence();
            if (entrySequence <= 0)
            {
                throw new InvalidOperationException("Prepared handoff could not allocate the next entry sequence.");
            }

            SessionActivityIdentity activationIdentity = BuildIdentity(initialDefinition, SessionActivityStage.ActivationExecuting, entrySequence);
            SessionActivityCommand command = new(
                SessionActivityCommandKind.StartActivity,
                activationIdentity,
                source,
                reason);

            List<SessionActivityFact> emittedFacts = new();
            List<SessionActivitySnapshot> emittedSnapshots = new();

            if (TryRejectStaleOrForeignCommand(command, emittedFacts, out SessionActivityCommandResult rejectedResult))
            {
                return rejectedResult;
            }

            _state.Reset(PipelineId, _sessionId);
            _state.SetCurrentDefinition(initialDefinition);
            _state.SetCurrentIdentity(activationIdentity, SessionActivityStage.ActivationExecuting);
            _state.MarkStarted();
            if (!handoff.HasResolvedActivity)
            {
                _state.AppendTrace($"[OBS][SessionActivityPipeline] FirstCatalogActivityResolved activityId='{initialDefinition.ActivityId}' activityOrdinal='{initialDefinition.ActivityOrdinal}' handoff='{handoff}' source='{source}' reason='{reason}'");
            }
            _state.AppendTrace($"[OBS][SessionActivityPipeline] start_from_prepared_handoff handoff='{handoff}' source='{source}' reason='{reason}'");
            _state.AppendTrace($"[OBS][SessionActivityPipeline] SessionActivityEntryHandoffAccepted handoff='{handoff}' source='{source}' reason='{reason}'");

            EmitFact(emittedFacts, SessionActivityFactKind.PipelineStarted, activationIdentity, source, reason, "SessionActivityPipeline started from prepared handoff.");
            EmitSnapshot(emittedSnapshots, "pipeline_started_from_handoff", source, reason, "Pipeline started from prepared handoff.");

            EnterActivity(initialDefinition, command, emittedFacts, emittedSnapshots, entrySequence);

            SessionActivityCommandResult result = new(
                SessionActivityCommandResultKind.Accepted,
                command,
                emittedFacts,
                emittedFacts.Count > 0 ? emittedFacts[emittedFacts.Count - 1].Reason : string.Empty);
            _state.AppendTrace($"[OBS][SessionActivityPipeline] start_from_prepared_handoff_completed handoff='{handoff}' result='{result.Kind}'");
            return result;
        }

        public SessionActivityCommandResult CompleteCurrentActivity(string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.CompleteCurrentActivity, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(
                    SessionActivityCommandKind.CompleteCurrentActivity,
                    source,
                    reason,
                    "pipeline_not_started");
            }

            return Execute(BuildCompleteCurrentActivityCommand(source, reason));
        }

        public SessionActivityCommandResult ContinueToNextActivity(string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.ContinueToNextActivity, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(
                    SessionActivityCommandKind.ContinueToNextActivity,
                    source,
                    reason,
                    "pipeline_not_started");
            }

            if (!_state.CurrentHandoff.IsValid)
            {
                if (_state.CurrentIdentity.IsValid)
                {
                    return RejectNoHandoffAvailable(source, reason);
                }

                return RejectWithoutActiveIdentity(
                    SessionActivityCommandKind.ContinueToNextActivity,
                    source,
                    reason,
                    "no_handoff_available");
            }

            return Execute(BuildContinueToNextActivityCommand(source, reason));
        }

        public SessionActivityCommandResult GoToNextActivity(string source, string reason)
        {
            return ExecuteNavigationCommand(SessionActivityCommandKind.GoToNextActivity, source, reason);
        }

        public SessionActivityCommandResult GoToPreviousActivity(string source, string reason)
        {
            return ExecuteNavigationCommand(SessionActivityCommandKind.GoToPreviousActivity, source, reason);
        }

        public SessionActivityCommandResult RestartCurrentActivity(string source, string reason)
        {
            return ExecuteNavigationCommand(SessionActivityCommandKind.RestartCurrentActivity, source, reason);
        }

        public SessionActivityCommandResult GoToActivity01(string source, string reason)
        {
            return ExecuteNavigationCommand(SessionActivityCommandKind.GoToActivity01, source, reason);
        }

        public SessionActivityCommandResult GoToActivity02(string source, string reason)
        {
            return ExecuteNavigationCommand(SessionActivityCommandKind.GoToActivity02, source, reason);
        }

        public SessionActivityCommandResult PauseRequested(string source, string reason)
        {
            return ExecutePauseRequested(source, reason);
        }

        public SessionActivityCommandResult ResumeRequested(string source, string reason)
        {
            return ExecuteResumeRequested(source, reason);
        }

        public SessionActivityCommandResult Execute(SessionActivityCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("SessionActivityCommand is invalid.");
            }

            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(command.Kind, command.Source, command.Reason);
            }

            if (command.Kind == SessionActivityCommandKind.PauseRequested ||
                command.Kind == SessionActivityCommandKind.PauseSimulation)
            {
                return ExecutePauseRequested(command.Source, command.Reason, command.Identity);
            }

            if (command.Kind == SessionActivityCommandKind.ResumeRequested ||
                command.Kind == SessionActivityCommandKind.ResumeSimulation)
            {
                return ExecuteResumeRequested(command.Source, command.Reason, command.Identity);
            }

            List<SessionActivityFact> emittedFacts = new();
            List<SessionActivitySnapshot> emittedSnapshots = new();

            if (TryRejectStaleOrForeignCommand(command, emittedFacts, out SessionActivityCommandResult rejectedResult))
            {
                return rejectedResult;
            }

            switch (command.Kind)
            {
                case SessionActivityCommandKind.StartActivity:
                    EmitStart(command, emittedFacts, emittedSnapshots);
                    break;
                case SessionActivityCommandKind.CompleteCurrentActivity:
                    EmitComplete(command, emittedFacts, emittedSnapshots);
                    break;
                case SessionActivityCommandKind.ContinueToNextActivity:
                    EmitContinue(command, emittedFacts, emittedSnapshots);
                    break;
                case SessionActivityCommandKind.GoToNextActivity:
                case SessionActivityCommandKind.GoToPreviousActivity:
                case SessionActivityCommandKind.RestartCurrentActivity:
                case SessionActivityCommandKind.GoToActivity01:
                case SessionActivityCommandKind.GoToActivity02:
                    EmitNavigation(command, emittedFacts, emittedSnapshots);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported command kind '{command.Kind}'.");
            }

            SessionActivityCommandResultKind resultKind = emittedFacts.Count == 0
                ? SessionActivityCommandResultKind.Accepted
                : emittedFacts[emittedFacts.Count - 1].Kind switch
                {
                    SessionActivityFactKind.CommandRejected => SessionActivityCommandResultKind.Rejected,
                    SessionActivityFactKind.PipelineCompleted => SessionActivityCommandResultKind.Completed,
                    SessionActivityFactKind.ActivationSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.GameplayContentSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.ActivityResultPresentationSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
                    SessionActivityFactKind.SimulationPaused => SessionActivityCommandResultKind.Accepted,
                    SessionActivityFactKind.SimulationResumed => SessionActivityCommandResultKind.Accepted,
                    _ => SessionActivityCommandResultKind.Accepted,
                };

            return new SessionActivityCommandResult(resultKind, command, emittedFacts, emittedFacts.Count > 0 ? emittedFacts[emittedFacts.Count - 1].Reason : string.Empty);
        }

        public IReadOnlyList<SessionActivityFact> Facts => _state.Facts;
        public IReadOnlyList<string> Trace => _state.Trace;

        private void EmitStart(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (_state.HasStarted)
            {
                EmitRejected(command, facts, "pipeline_already_started", "SessionActivityPipeline already started.", _state.CurrentIdentity, true);
                return;
            }

            SessionActivityDefinition firstDefinition = ResolveActivityByOrdinalOrFail(1);
            int entrySequence = 1;
            SessionActivityIdentity activationIdentity = BuildIdentity(firstDefinition, SessionActivityStage.ActivationExecuting, entrySequence);
            SessionActivitySnapshot pipelineStartedSnapshot = new(
                activationIdentity,
                firstDefinition,
                default,
                command.Source,
                command.Reason,
                "pipeline_started: Pipeline started.");

            if (!activationIdentity.IsValid)
            {
                throw new InvalidOperationException("Activation identity is invalid.");
            }

            if (!pipelineStartedSnapshot.IsValid)
            {
                throw new InvalidOperationException("PipelineStarted snapshot is invalid.");
            }

            _state.Reset(PipelineId, _sessionId);
            _state.SetCurrentDefinition(firstDefinition);
            _state.SetCurrentIdentity(activationIdentity, SessionActivityStage.ActivationExecuting);
            _state.MarkStarted();

            EmitFact(facts, SessionActivityFactKind.PipelineStarted, activationIdentity, command.Source, command.Reason, "SessionActivityPipeline started.");
            EmitSnapshot(snapshots, "pipeline_started", command.Source, command.Reason, "Pipeline started.");

            EnterActivity(firstDefinition, command, facts, snapshots, entrySequence);
        }

        private void EmitComplete(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (!EnsureExpectedStage(command, facts, SessionActivityStage.ActivityRunning, "complete_current_activity"))
            {
                return;
            }

            if (!EnsureIdentityMatches(command, facts, _state.CurrentIdentity, "complete_current_activity"))
            {
                return;
            }

            SessionActivityDefinition current = _state.CurrentDefinition;
            int currentEntrySequence = _state.CurrentEntrySequence;
            ReleaseActivityGateIfBlocked(command);
            _state.SetSimulationState(SessionActivitySimulationState.Stopped);
            SessionActivityIdentity deactivationIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, currentEntrySequence);
            _state.SetCurrentIdentity(deactivationIdentity, SessionActivityStage.Deactivation);
            EmitFact(facts, SessionActivityFactKind.ActivityDeactivated, deactivationIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");
            EmitSnapshot(snapshots, "deactivation", command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");

            if (!current.HasActivityResult)
            {
                SessionActivityIdentity skipIdentity = BuildIdentity(current, SessionActivityStage.ActivityResultPresentationSkippedNoContent, currentEntrySequence);
                _state.SetCurrentIdentity(skipIdentity, SessionActivityStage.ActivityResultPresentationSkippedNoContent);
                EmitFact(facts, SessionActivityFactKind.ActivityResultPresentationSkippedNoContent, skipIdentity, command.Source, command.Reason, $"'{current.ActivityId}' has no activity result presentation.");
                EmitSnapshot(snapshots, "activity_result_presentation_skipped_no_content", command.Source, command.Reason, $"'{current.ActivityId}' has no activity result presentation.");
            }
            else
            {
                SessionActivityIdentity presentationIdentity = BuildIdentity(current, SessionActivityStage.ActivityResultPresentationExecuting, currentEntrySequence);
                _state.SetCurrentIdentity(presentationIdentity, SessionActivityStage.ActivityResultPresentationExecuting);
                EmitFact(facts, SessionActivityFactKind.ActivityResultPresentationEntered, presentationIdentity, command.Source, command.Reason, $"'{current.ActivityId}' activity result presentation entered.");
                EmitSnapshot(snapshots, "activity_result_presentation_entered", command.Source, command.Reason, $"'{current.ActivityId}' activity result presentation entered.");
            }

            if (!current.HasNextActivity)
            {
                _state.SetCurrentIdentity(BuildIdentity(current, SessionActivityStage.Completed, currentEntrySequence), SessionActivityStage.Completed);
                _state.MarkCompleted();
                EmitFact(facts, SessionActivityFactKind.PipelineCompleted, _state.CurrentIdentity, command.Source, command.Reason, $"'{current.ActivityId}' completed and no next activity is configured.");
                EmitSnapshot(snapshots, "pipeline_completed", command.Source, command.Reason, $"'{current.ActivityId}' completed and no next activity is configured.");
                return;
            }

            SessionActivityDefinition next = ResolveActivityByIdOrFail(current.NextActivityId);
            int nextEntrySequence = ResolveNextEntrySequence();
            SessionActivityIdentity nextActivationIdentity = BuildIdentity(next, SessionActivityStage.ActivationExecuting, nextEntrySequence);
            SessionActivityHandoff handoff = new(
                BuildIdentity(current, _state.CurrentStage, currentEntrySequence),
                nextActivationIdentity,
                next.ActivityId,
                command.Source,
                command.Reason);

            if (!handoff.IsValid)
            {
                throw new InvalidOperationException("Generated handoff is invalid.");
            }

            _state.SetHandoff(handoff);
            EmitFact(facts, SessionActivityFactKind.ActivityHandoffPrepared, nextActivationIdentity, command.Source, command.Reason, $"Handoff prepared for '{next.ActivityId}'.", handoff);
            EmitSnapshot(snapshots, "handoff_created", command.Source, command.Reason, $"Handoff prepared for '{next.ActivityId}'.");
        }

        private void EmitContinue(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (!EnsureExpectedStage(command, facts, SessionActivityStage.ActivityResultPresentationExecuting, "continue_to_next_activity"))
            {
                return;
            }

            if (!EnsureIdentityMatches(command, facts, _state.CurrentHandoff.ToIdentity, "continue_to_next_activity"))
            {
                return;
            }

            SessionActivityHandoff handoff = _state.CurrentHandoff;
            EmitFact(facts, SessionActivityFactKind.ContinueAccepted, _state.CurrentIdentity, command.Source, command.Reason, $"Continue accepted to '{handoff.NextActivityId}'.", handoff);
            EmitSnapshot(snapshots, "continue_accepted", command.Source, command.Reason, $"Continue accepted to '{handoff.NextActivityId}'.");

            SessionActivityDefinition next = ResolveActivityByIdOrFail(handoff.NextActivityId);
            int nextEntrySequence = handoff.ToIdentity.EntrySequence;
            _state.ClearHandoff();
            _state.SetCurrentDefinition(next);

            EnterActivity(next, command, facts, snapshots, nextEntrySequence);
        }

        private void EmitNavigation(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (!EnsureExpectedStage(command, facts, SessionActivityStage.ActivityRunning, "navigation"))
            {
                return;
            }

            SessionActivityDefinition current = _state.CurrentDefinition;
            if (!current.IsValid)
            {
                throw new InvalidOperationException("Current activity definition is invalid.");
            }

            int nextEntrySequence = ResolveNextEntrySequence();

            if (!TryResolveNavigationTarget(command.Kind, current, out SessionActivityDefinition target, out string rejectionReason))
            {
                EmitRejected(
                    command,
                    facts,
                    rejectionReason,
                    ResolveNavigationRejectionMessage(command.Kind, current, rejectionReason),
                    _state.CurrentIdentity,
                    true);
                return;
            }

            if (command.Kind != SessionActivityCommandKind.RestartCurrentActivity &&
                string.Equals(target.ActivityId, current.ActivityId, StringComparison.OrdinalIgnoreCase))
            {
                EmitRejected(
                    command,
                    facts,
                    "already_on_activity",
                    $"Navigation target '{target.ActivityId}' is already active.",
                    _state.CurrentIdentity,
                    true);
                return;
            }

            EmitNavigationTransition(command, current, target, nextEntrySequence, facts, snapshots);
        }

        private void EmitNavigationTransition(
            SessionActivityCommand command,
            SessionActivityDefinition current,
            SessionActivityDefinition target,
            int targetEntrySequence,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            int currentEntrySequence = _state.CurrentEntrySequence;
            ReleaseActivityGateIfBlocked(command);
            _state.SetSimulationState(SessionActivitySimulationState.Stopped);
            SessionActivityIdentity deactivationIdentity = BuildIdentity(current, SessionActivityStage.Deactivation, currentEntrySequence);
            _state.SetCurrentIdentity(deactivationIdentity, SessionActivityStage.Deactivation);
            EmitFact(facts, SessionActivityFactKind.ActivityDeactivated, deactivationIdentity, command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");
            EmitSnapshot(snapshots, "deactivation", command.Source, command.Reason, $"'{current.ActivityId}' deactivated.");

            SessionActivityIdentity targetActivationIdentity = BuildIdentity(target, SessionActivityStage.ActivationExecuting, targetEntrySequence);
            SessionActivityHandoff handoff = new(
                deactivationIdentity,
                targetActivationIdentity,
                target.ActivityId,
                command.Source,
                command.Reason);

            if (!handoff.IsValid)
            {
                throw new InvalidOperationException("Generated navigation handoff is invalid.");
            }

            _state.SetHandoff(handoff);
            EmitFact(facts, SessionActivityFactKind.ActivityHandoffPrepared, targetActivationIdentity, command.Source, command.Reason, $"Handoff prepared for '{target.ActivityId}'.", handoff);
            EmitSnapshot(snapshots, "handoff_created", command.Source, command.Reason, $"Handoff prepared for '{target.ActivityId}'.");

            _state.ClearHandoff();
            _state.SetCurrentDefinition(target);

            EnterActivity(target, command, facts, snapshots, targetEntrySequence);
        }

        private void EnterActivity(SessionActivityDefinition definition, SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots, int entrySequence)
        {
            if (!definition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityDefinition is invalid.");
            }

            if (!definition.HasActivation)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(definition, SessionActivityStage.ActivationSkippedNoContent, entrySequence);
                _state.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActivationSkippedNoContent);
                EmitFact(facts, SessionActivityFactKind.ActivationSkippedNoContent, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activation skipped as no-content.");
                EmitSnapshot(snapshots, "activation_skipped_no_content", command.Source, command.Reason, $"'{definition.ActivityId}' activation skipped as no-content.");
            }
            else
            {
                SessionActivityIdentity activationIdentity = BuildIdentity(definition, SessionActivityStage.ActivationExecuting, entrySequence);
                _state.SetCurrentIdentity(activationIdentity, SessionActivityStage.ActivationExecuting);
                EmitFact(facts, SessionActivityFactKind.ActivationEntered, activationIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' activation entered.");
                EmitSnapshot(snapshots, "activation_entered", command.Source, command.Reason, $"'{definition.ActivityId}' activation entered.");
            }

            if (!definition.HasGameplayContent)
            {
                SessionActivityIdentity skipIdentity = BuildIdentity(definition, SessionActivityStage.ActivityRunning, entrySequence);
                _state.SetCurrentIdentity(skipIdentity, SessionActivityStage.ActivityRunning);
                _state.SetSimulationState(SessionActivitySimulationState.Running);
                EmitFact(facts, SessionActivityFactKind.GameplayContentSkippedNoContent, skipIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' gameplay content skipped as no-content.");
                EmitSnapshot(snapshots, "gameplay_content_skipped_no_content", command.Source, command.Reason, $"'{definition.ActivityId}' gameplay content skipped as no-content.");
                return;
            }

            SessionActivityIdentity runningIdentity = BuildIdentity(definition, SessionActivityStage.ActivityRunning, entrySequence);
            _state.SetCurrentIdentity(runningIdentity, SessionActivityStage.ActivityRunning);
            _state.SetSimulationState(SessionActivitySimulationState.Running);
            EmitFact(facts, SessionActivityFactKind.GameplayRunningEntered, runningIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' running.");
            EmitSnapshot(snapshots, "activity_running_entered", command.Source, command.Reason, $"'{definition.ActivityId}' running.");
        }

        private SessionActivityCommandResult ExecutePauseRequested(string source, string reason)
        {
            return ExecutePauseRequested(BuildPauseCommand(SessionActivityCommandKind.PauseRequested, source, reason));
        }

        private SessionActivityCommandResult ExecutePauseRequested(string source, string reason, SessionActivityIdentity identity)
        {
            SessionActivityCommand command = identity.IsValid
                ? new SessionActivityCommand(SessionActivityCommandKind.PauseRequested, identity, source, reason)
                : BuildPauseCommand(SessionActivityCommandKind.PauseRequested, source, reason);

            return ExecutePauseRequested(command);
        }

        private SessionActivityCommandResult ExecuteResumeRequested(string source, string reason)
        {
            return ExecuteResumeRequested(BuildPauseCommand(SessionActivityCommandKind.ResumeRequested, source, reason));
        }

        private SessionActivityCommandResult ExecuteResumeRequested(string source, string reason, SessionActivityIdentity identity)
        {
            SessionActivityCommand command = identity.IsValid
                ? new SessionActivityCommand(SessionActivityCommandKind.ResumeRequested, identity, source, reason)
                : BuildPauseCommand(SessionActivityCommandKind.ResumeRequested, source, reason);

            return ExecuteResumeRequested(command);
        }

        private SessionActivityCommandResult ExecutePauseRequested(SessionActivityCommand command)
        {
            return ExecutePauseOrResume(
                command,
                SessionActivityCommandKind.PauseRequested,
                SessionActivitySimulationState.Paused,
                SimulationGateCommandKind.BlockActivitySimulation,
                "pause_requested",
                "Pause requested.");
        }

        private SessionActivityCommandResult ExecuteResumeRequested(SessionActivityCommand command)
        {
            return ExecutePauseOrResume(
                command,
                SessionActivityCommandKind.ResumeRequested,
                SessionActivitySimulationState.Running,
                SimulationGateCommandKind.ReleaseActivitySimulation,
                "resume_requested",
                "Resume requested.");
        }

        private SessionActivityCommandResult ExecutePauseOrResume(
            SessionActivityCommand command,
            SessionActivityCommandKind expectedKind,
            SessionActivitySimulationState targetState,
            SimulationGateCommandKind gateCommandKind,
            string snapshotKind,
            string message)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(command.Kind, command.Source, command.Reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(command.Kind, command.Source, command.Reason, "pipeline_not_started");
            }

            if (_state.CurrentStage != SessionActivityStage.ActivityRunning)
            {
                return RejectPauseCommand(
                    command,
                    "unexpected_stage",
                    $"Operation '{expectedKind}' requires stage '{SessionActivityStage.ActivityRunning}', but current stage is '{_state.CurrentStage}'.");
            }

            if (!command.Identity.IsValid || !_state.CurrentIdentity.IsValid || command.Identity != _state.CurrentIdentity)
            {
                return RejectPauseCommand(
                    command,
                    "stale_or_foreign_command",
                    $"Command identity '{command.Identity}' does not match the active cycle identity '{_state.CurrentIdentity}'.");
            }

            if (expectedKind == SessionActivityCommandKind.PauseRequested)
            {
                if (_state.CurrentSimulationState == SessionActivitySimulationState.Paused)
                {
                    return RejectPauseCommand(command, "simulation_already_paused", "Simulation is already paused.");
                }

                if (_state.CurrentSimulationState != SessionActivitySimulationState.Running)
                {
                    throw new InvalidOperationException("Simulation state is invalid for pause.");
                }
            }
            else
            {
                if (_state.CurrentSimulationState == SessionActivitySimulationState.Running)
                {
                    return RejectPauseCommand(command, "simulation_not_paused", "Simulation is not paused.");
                }

                if (_state.CurrentSimulationState != SessionActivitySimulationState.Paused)
                {
                    throw new InvalidOperationException("Simulation state is invalid for resume.");
                }
            }

            List<SessionActivityFact> emittedFacts = new();
            List<SessionActivitySnapshot> emittedSnapshots = new();

            if (expectedKind == SessionActivityCommandKind.PauseRequested)
            {
                SimulationGateResult gateResult = ApplyActivityGateCommand(gateCommandKind, command);
                if (gateResult.IsRejected)
                {
                    return RejectPauseCommand(
                        command,
                        gateResult.Reason,
                        $"SimulationGate rejected pause command. {gateResult.Snapshot}");
                }

                _state.SetSimulationState(targetState);
                _pauseOverlayAdapter.Show(_state.CurrentIdentity, command.Source, command.Reason);
            }
            else
            {
                _state.SetSimulationState(targetState);
                _pauseOverlayAdapter.Hide(_state.CurrentIdentity, command.Source, command.Reason);

                SimulationGateResult gateResult = ApplyActivityGateCommand(gateCommandKind, command);
                if (gateResult.IsRejected)
                {
                    return RejectPauseCommand(
                        command,
                        gateResult.Reason,
                        $"SimulationGate rejected resume command. {gateResult.Snapshot}");
                }
            }

            EmitPauseResolution(
                command,
                emittedFacts,
                emittedSnapshots,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? SessionActivityFactKind.PauseResolved
                    : SessionActivityFactKind.ResumeResolved,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "pause_resolved"
                    : "resume_resolved",
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "Pause accepted."
                    : "Resume accepted.");

            ApplyActivityInputMode(
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? SessionActivityInputModeKind.PauseOverlay
                    : SessionActivityInputModeKind.ActivityGameplay,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? SessionActivityFactKind.PauseResolved
                    : SessionActivityFactKind.ResumeResolved,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "pause_resolved"
                    : "resume_resolved",
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "Pause accepted."
                    : "Resume accepted.",
                command);

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Accepted,
                command,
                emittedFacts,
                emittedFacts.Count > 0 ? emittedFacts[emittedFacts.Count - 1].Reason : string.Empty);
        }

        private SessionActivityCommand BuildPauseCommand(SessionActivityCommandKind kind, string source, string reason)
        {
            if (_state.CurrentIdentity.IsValid)
            {
                return new SessionActivityCommand(kind, _state.CurrentIdentity, source, reason);
            }

            return BuildNoActiveIdentityCommand(kind, source, reason);
        }

        private SessionActivityCommandResult RejectPauseCommand(
            SessionActivityCommand command,
            string reason,
            string message)
        {
            List<SessionActivityFact> rejectedFacts = new();
            List<SessionActivitySnapshot> rejectedSnapshots = new();
            EmitPauseRejection(
                command,
                rejectedFacts,
                rejectedSnapshots,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? SessionActivityFactKind.PauseRejected
                    : SessionActivityFactKind.ResumeRejected,
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "pause_rejected"
                    : "resume_rejected",
                command.Kind == SessionActivityCommandKind.PauseRequested
                    ? "Pause rejected."
                    : "Resume rejected.",
                reason,
                message);

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                rejectedFacts,
                reason);
        }

        private void EmitPauseResolution(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityFactKind factKind,
            string snapshotKind,
            string message)
        {
            SessionActivityFact fact = EmitFact(
                facts,
                factKind,
                _state.CurrentIdentity,
                command.Source,
                command.Reason,
                message);

            EmitSnapshot(snapshots, snapshotKind, command.Source, command.Reason, message);

            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Pause] command='{command.Kind}' fact='{factKind}' simulationState='{_state.CurrentSimulationState}' gateState='{_simulationGate.State}' identity='{_state.CurrentIdentity}' reason='{command.Reason}' source='{command.Source}' outcome='accepted'.",
                DebugUtility.Colors.Success);

            if (!fact.IsValid)
            {
                throw new InvalidOperationException($"{factKind} fact is invalid.");
            }
        }

        private SessionActivityFact EmitPauseRejection(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityFactKind factKind,
            string snapshotKind,
            string message,
            string reason,
            string detailMessage)
        {
            SessionActivityFact fact = EmitFact(
                facts,
                factKind,
                command.Identity,
                command.Source,
                reason,
                detailMessage);

            EmitSnapshot(snapshots, snapshotKind, command.Source, reason, message);

            _state.AppendTrace(
                $"[OBS][SessionActivityPipeline][Pause] command='{command.Kind}' fact='{factKind}' simulationState='{_state.CurrentSimulationState}' gateState='{_simulationGate.State}' identity='{command.Identity}' reason='{reason}' source='{command.Source}' message='{detailMessage}' outcome='rejected'.");

            DebugUtility.Log(typeof(SessionActivityPipeline),
                $"[OBS][SessionActivityPipeline][Pause] command='{command.Kind}' fact='{factKind}' simulationState='{_state.CurrentSimulationState}' gateState='{_simulationGate.State}' identity='{command.Identity}' reason='{reason}' source='{command.Source}' outcome='rejected'.",
                DebugUtility.Colors.Warning);

            return fact;
        }

        private void ApplyActivityInputMode(
            SessionActivityInputModeKind mode,
            SessionActivityFactKind reasonFactKind,
            string snapshotKind,
            string message,
            SessionActivityCommand command)
        {
            SessionActivityInputModeCommand inputModeCommand = new(
                mode,
                _state.CurrentIdentity,
                command.Source,
                reasonFactKind.ToString());

            SessionActivityInputModeObservation observation = _inputModeAdapter.Apply(inputModeCommand);
            if (!observation.IsValid)
            {
                throw new InvalidOperationException("SessionActivityInputModeAdapter returned an invalid observation.");
            }

            _state.AppendTrace(
                $"[OBS][SessionActivityPipeline][InputMode] command='ApplyActivityInputMode' mode='{mode}' reason='{reasonFactKind}' snapshot='{snapshotKind}' message='{message}' outcome='{observation.Outcome}' identity='{_state.CurrentIdentity}' source='{command.Source}' reasonText='{command.Reason}'");
        }

        private SimulationGateResult ApplyActivityGateCommand(
            SimulationGateCommandKind gateCommandKind,
            SessionActivityCommand command)
        {
            SimulationGateCommand gateCommand = BuildActivityGateCommand(gateCommandKind, command);
            SimulationGateResult gateResult = _simulationGate.Execute(gateCommand);
            RecordSimulationGateResult(gateResult);

            return gateResult;
        }

        private void ReleaseActivityGateIfBlocked(SessionActivityCommand command)
        {
            if (_state.CurrentSimulationState == SessionActivitySimulationState.Paused && !_simulationGate.State.ActivityBlocked)
            {
                throw new InvalidOperationException("Paused activity requires a blocked simulation gate.");
            }

            if (!_simulationGate.State.ActivityBlocked)
            {
                return;
            }

            SessionActivityIdentity currentIdentity = _state.CurrentIdentity;
            if (!currentIdentity.IsValid)
            {
                throw new InvalidOperationException("Activity gate release requires an active identity.");
            }

            SimulationGateIdentity expectedGateIdentity = BuildActivityGateIdentity(currentIdentity, command.Source, command.Reason);
            if (!_simulationGate.State.ActivityIdentity.MatchesActivityScope(expectedGateIdentity))
            {
                throw new InvalidOperationException("Activity gate is blocked by a foreign identity.");
            }

            SimulationGateResult gateResult = _simulationGate.Execute(BuildActivityGateCommand(SimulationGateCommandKind.ReleaseActivitySimulation, command));
            RecordSimulationGateResult(gateResult);

            if (gateResult.IsRejected)
            {
                throw new InvalidOperationException($"SimulationGate rejected release command. reason='{gateResult.Reason}'.");
            }
        }

        private SimulationGateCommand BuildActivityGateCommand(
            SimulationGateCommandKind gateCommandKind,
            SessionActivityCommand command)
        {
            return new SimulationGateCommand(
                gateCommandKind,
                BuildActivityGateIdentity(_state.CurrentIdentity, command.Source, command.Reason),
                command.Source,
                command.Reason);
        }

        private static SimulationGateIdentity BuildActivityGateIdentity(
            SessionActivityIdentity identity,
            string source,
            string reason)
        {
            return new SimulationGateIdentity(
                identity.PipelineId,
                identity.SessionId,
                identity.ActivityId,
                identity.ActivityOrdinal,
                identity.EntrySequence,
                identity.Stage,
                source,
                reason);
        }

        private void RecordSimulationGateResult(SimulationGateResult gateResult)
        {
            if (!gateResult.IsValid)
            {
                throw new InvalidOperationException("SimulationGate result is invalid.");
            }

            if (gateResult.Facts.Count == 0)
            {
                throw new InvalidOperationException("SimulationGate result emitted no facts.");
            }

            SimulationGateFact fact = gateResult.Facts[gateResult.Facts.Count - 1];
            SimulationGateSnapshot snapshot = gateResult.Snapshot;
            _state.AppendTrace($"[OBS][SimulationGate][Pipeline] commandKind='{gateResult.Command.Kind}' factKind='{fact.Kind}' pipelineId='{snapshot.CommandIdentity.PipelineId}' sessionStateId='{snapshot.CommandIdentity.SessionStateId}' activityId='{snapshot.CommandIdentity.ActivityId}' activityOrdinal='{snapshot.CommandIdentity.ActivityOrdinal}' entrySequence='{snapshot.CommandIdentity.EntrySequence}' stage='{snapshot.CommandIdentity.Stage}' sessionBlocked='{snapshot.SessionBlocked}' activityBlocked='{snapshot.ActivityBlocked}' source='{snapshot.Source}' reason='{snapshot.Reason}' decisionSource='pipeline.command'.");
            _state.AppendTrace($"[OBS][SimulationGate][Pipeline] fact='{fact}'");
            _state.AppendTrace($"[OBS][SimulationGate][Pipeline] snapshot='{snapshot}'");
        }

        private bool EnsureExpectedStage(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            SessionActivityStage expectedStage,
            string operation)
        {
            if (_state.CurrentStage == expectedStage)
            {
                return true;
            }

            EmitRejected(command, facts, "unexpected_stage", $"Operation '{operation}' requires stage '{expectedStage}', but current stage is '{_state.CurrentStage}'.", _state.CurrentIdentity, true);
            return false;
        }

        private bool EnsureIdentityMatches(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            SessionActivityIdentity expected,
            string operation)
        {
            if (expected.IsValid && command.Identity == expected)
            {
                return true;
            }

            EmitRejected(command, facts, "identity_mismatch", $"Operation '{operation}' received an identity that does not match the active cycle.", _state.CurrentIdentity, true);
            return false;
        }

        private SessionActivityCommandResult ExecuteNavigationCommand(SessionActivityCommandKind kind, string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(kind, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(kind, source, reason, "pipeline_not_started");
            }

            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Active pipeline identity is invalid.");
            }

            return Execute(BuildNavigationCommand(kind, source, reason));
        }

        private SessionActivityCommandResult ExecuteSimulationCommand(SessionActivityCommandKind kind, string source, string reason)
        {
            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(kind, source, reason);
            }

            if (!_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(kind, source, reason, "pipeline_not_started");
            }

            if (_state.CurrentStage != SessionActivityStage.ActivityRunning)
            {
                return RejectStageForSimulation(kind, source, reason, "unexpected_stage");
            }

            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Active pipeline identity is invalid.");
            }

            if (kind == SessionActivityCommandKind.PauseSimulation && _state.CurrentSimulationState == SessionActivitySimulationState.Paused)
            {
                SessionActivityCommand command = new(kind, _state.CurrentIdentity, source, reason);
                List<SessionActivityFact> rejectedFacts = new();
                EmitRejected(command, rejectedFacts, "simulation_already_paused", "Simulation is already paused.", _state.CurrentIdentity, true);
                return new SessionActivityCommandResult(SessionActivityCommandResultKind.Rejected, command, rejectedFacts, "simulation_already_paused");
            }

            if (kind == SessionActivityCommandKind.ResumeSimulation && _state.CurrentSimulationState != SessionActivitySimulationState.Paused)
            {
                SessionActivityCommand command = new(kind, _state.CurrentIdentity, source, reason);
                List<SessionActivityFact> rejectedFacts = new();
                EmitRejected(command, rejectedFacts, "simulation_not_paused", "Simulation is not paused.", _state.CurrentIdentity, true);
                return new SessionActivityCommandResult(SessionActivityCommandResultKind.Rejected, command, rejectedFacts, "simulation_not_paused");
            }

            return Execute(new SessionActivityCommand(kind, _state.CurrentIdentity, source, reason));
        }

        private SessionActivityFact EmitRejected(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            string reason,
            string message,
            SessionActivityIdentity identityOverride = default,
            bool useIdentityOverride = false)
        {
            SessionActivityIdentity identity = useIdentityOverride ? identityOverride : command.Identity;
            SessionActivityFact fact = EmitFact(
                facts,
                SessionActivityFactKind.CommandRejected,
                identity,
                command.Source,
                reason,
                message);

            _state.AppendTrace($"[OBS][SessionActivityPipeline] command_rejected reason='{reason}' command='{command}' message='{message}'");
            _state.AppendTrace($"[OBS][SessionActivityPipeline] command_rejected_state simulationState='{_state.CurrentSimulationState}' stage='{_state.CurrentStage}' entrySequence='{_state.CurrentEntrySequence}' identity='{_state.CurrentIdentity}'");
            if (!fact.IsValid)
            {
                throw new InvalidOperationException("CommandRejected fact is invalid.");
            }

            return fact;
        }

        private SessionActivityCommandResult RejectStartCommand(string source, string reason)
        {
            if (_state.CurrentIdentity.IsValid)
            {
                List<SessionActivityFact> rejectedFacts = new();
                SessionActivityCommand command = new(
                    SessionActivityCommandKind.StartActivity,
                    _state.CurrentIdentity,
                    source,
                    reason);

                EmitRejected(command, rejectedFacts, "pipeline_already_started", "SessionActivityPipeline already started.", _state.CurrentIdentity, true);
                return new SessionActivityCommandResult(
                    SessionActivityCommandResultKind.Rejected,
                    command,
                    rejectedFacts,
                    "pipeline_already_started");
            }

            return RejectWithoutActiveIdentity(SessionActivityCommandKind.StartActivity, source, reason, "pipeline_already_started");
        }

        private SessionActivityCommandResult RejectWithoutActiveIdentity(SessionActivityCommandKind kind, string source, string reason, string rejectionReason)
        {
            SessionActivityCommand command = BuildNoActiveIdentityCommand(kind, source, reason);
            if (!command.IsValid)
            {
                throw new InvalidOperationException("Rejected command cannot be constructed.");
            }

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                Array.Empty<SessionActivityFact>(),
                rejectionReason);
        }

        private SessionActivityCommandResult RejectNoHandoffAvailable(string source, string reason)
        {
            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("No-handoff rejection requires an active identity.");
            }

            SessionActivityCommand command = new(
                SessionActivityCommandKind.ContinueToNextActivity,
                _state.CurrentIdentity,
                source,
                reason);

            List<SessionActivityFact> rejectedFacts = new();
            EmitRejected(command, rejectedFacts, "no_handoff_available", "No handoff is available to continue.", _state.CurrentIdentity, true);

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                rejectedFacts,
                "no_handoff_available");
        }

        private SessionActivityCommandResult RejectPreparedHandoff(
            SessionActivityEntryHandoff handoff,
            string source,
            string reason,
            string rejectionReason,
            string message)
        {
            SessionActivityDefinition firstDefinition = ResolveActivityByOrdinalOrFail(1);
            int entrySequence = ResolveNextEntrySequence();
            SessionActivityCommand command = new(
                SessionActivityCommandKind.StartActivity,
                BuildIdentity(firstDefinition, SessionActivityStage.ActivationExecuting, entrySequence),
                source,
                reason);

            List<SessionActivityFact> rejectedFacts = new();
            EmitRejected(command, rejectedFacts, rejectionReason, message, command.Identity, true);
            _state.AppendTrace($"[OBS][SessionActivityPipeline] SessionActivityEntryHandoffRejected handoff='{handoff}' source='{source}' reason='{reason}' rejectionReason='{rejectionReason}' message='{message}'");

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                rejectedFacts,
                rejectionReason);
        }

        private SessionActivityCommandResult RejectStageForSimulation(SessionActivityCommandKind kind, string source, string reason, string rejectionReason)
        {
            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Simulation-stage rejection requires an active identity.");
            }

            SessionActivityCommand command = new(kind, _state.CurrentIdentity, source, reason);
            List<SessionActivityFact> rejectedFacts = new();
            EmitRejected(
                command,
                rejectedFacts,
                rejectionReason,
                $"Operation '{kind}' requires stage '{SessionActivityStage.ActivityRunning}', but current stage is '{_state.CurrentStage}'.",
                _state.CurrentIdentity,
                true);

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                rejectedFacts,
                rejectionReason);
        }

        private SessionActivityCommandResult RejectTerminalCommand(SessionActivityCommandKind kind, string source, string reason)
        {
            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Terminal rejection requires an active completed identity.");
            }

            SessionActivityCommand command = new(kind, _state.CurrentIdentity, source, reason);
            List<SessionActivityFact> rejectedFacts = new();
            EmitRejected(command, rejectedFacts, "pipeline_completed", "SessionActivityPipeline already completed.", _state.CurrentIdentity, true);

            return new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                rejectedFacts,
                "pipeline_completed");
        }

        private bool IsTerminalCompleted()
        {
            return _state.HasCompleted || _state.CurrentStage == SessionActivityStage.Completed;
        }

        private bool TryRejectStaleOrForeignCommand(
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            out SessionActivityCommandResult rejectedResult)
        {
            rejectedResult = default;

            if (_state.HasStarted && !_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Active pipeline identity is invalid.");
            }

            SessionActivityIdentity expectedIdentity = ResolveExpectedCommandIdentity(command.Kind);
            if (expectedIdentity.IsValid && command.Identity == expectedIdentity)
            {
                return false;
            }

            if (_state.CurrentIdentity.IsValid)
            {
                List<SessionActivityFact> rejectedFacts = facts;
                EmitRejected(
                    command,
                    rejectedFacts,
                    "stale_or_foreign_command",
                    $"Command identity '{command.Identity}' does not match the active cycle identity '{_state.CurrentIdentity}'.",
                    _state.CurrentIdentity,
                    true);

                rejectedResult = new SessionActivityCommandResult(
                    SessionActivityCommandResultKind.Rejected,
                    command,
                    rejectedFacts,
                    "stale_or_foreign_command");
                return true;
            }

            rejectedResult = new SessionActivityCommandResult(
                SessionActivityCommandResultKind.Rejected,
                command,
                Array.Empty<SessionActivityFact>(),
                "stale_or_foreign_command");
            return true;
        }

        private SessionActivityIdentity ResolveExpectedCommandIdentity(SessionActivityCommandKind kind)
        {
            if (kind == SessionActivityCommandKind.StartActivity)
            {
                if (_state.HasStarted && _state.CurrentIdentity.IsValid)
                {
                    return _state.CurrentIdentity;
                }

                SessionActivityDefinition firstDefinition = ResolveActivityByOrdinalOrFail(1);
                return BuildIdentity(firstDefinition, SessionActivityStage.ActivationExecuting, 1);
            }

            if (!_state.HasStarted)
            {
                return default;
            }

            if (kind == SessionActivityCommandKind.CompleteCurrentActivity)
            {
                return _state.CurrentIdentity;
            }

            if (kind == SessionActivityCommandKind.ContinueToNextActivity)
            {
                if (_state.CurrentHandoff.IsValid)
                {
                    return _state.CurrentHandoff.ToIdentity;
                }

                if (_state.CurrentIdentity.IsValid)
                {
                    return _state.CurrentIdentity;
                }
            }

            if (kind == SessionActivityCommandKind.PauseRequested ||
                kind == SessionActivityCommandKind.ResumeRequested ||
                kind == SessionActivityCommandKind.PauseSimulation ||
                kind == SessionActivityCommandKind.ResumeSimulation)
            {
                return _state.CurrentIdentity;
            }

            if (kind == SessionActivityCommandKind.GoToNextActivity ||
                kind == SessionActivityCommandKind.GoToPreviousActivity ||
                kind == SessionActivityCommandKind.RestartCurrentActivity ||
                kind == SessionActivityCommandKind.GoToActivity01 ||
                kind == SessionActivityCommandKind.GoToActivity02)
            {
                return _state.CurrentIdentity;
            }

            return default;
        }

        private SessionActivityCommand BuildNoActiveIdentityCommand(SessionActivityCommandKind kind, string source, string reason)
        {
            SessionActivityDefinition firstDefinition = ResolveActivityByOrdinalOrFail(1);
            return new SessionActivityCommand(
                kind,
                BuildIdentity(firstDefinition, SessionActivityStage.ActivationExecuting, 1),
                source,
                reason);
        }

        private SessionActivityCommand BuildNavigationCommand(SessionActivityCommandKind kind, string source, string reason)
        {
            if (!_state.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException("Navigation command requires an active pipeline identity.");
            }

            return new SessionActivityCommand(
                kind,
                _state.CurrentIdentity,
                source,
                reason);
        }

        private SessionActivityDefinition ResolveActivityByOrdinalOrFail(int ordinal)
        {
            if (_catalog.TryGetByOrdinal(ordinal, out SessionActivityDefinition definition) && definition.IsValid)
            {
                return definition;
            }

            throw new InvalidOperationException($"Missing required activity ordinal '{ordinal}'.");
        }

        private SessionActivityDefinition ResolveActivityByIdOrFail(string activityId)
        {
            if (string.IsNullOrWhiteSpace(activityId))
            {
                throw new InvalidOperationException("activityId is required.");
            }

            for (int index = 0; index < _catalog.Definitions.Count; index++)
            {
                SessionActivityDefinition candidate = _catalog.Definitions[index];
                if (string.Equals(candidate.ActivityId, activityId, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException($"Missing required activity '{activityId}'.");
        }

        private bool TryResolveNavigationTarget(
            SessionActivityCommandKind kind,
            SessionActivityDefinition current,
            out SessionActivityDefinition target,
            out string rejectionReason)
        {
            target = default;
            rejectionReason = string.Empty;

            switch (kind)
            {
                case SessionActivityCommandKind.GoToNextActivity:
                    if (!current.HasNextActivity)
                    {
                        rejectionReason = "no_next_activity";
                        return false;
                    }

                    return TryResolveActivityByOrdinal(current.ActivityOrdinal + 1, out target, out rejectionReason);

                case SessionActivityCommandKind.GoToPreviousActivity:
                    if (current.ActivityOrdinal <= 1)
                    {
                        rejectionReason = "no_previous_activity";
                        return false;
                    }

                    return TryResolveActivityByOrdinal(current.ActivityOrdinal - 1, out target, out rejectionReason);

                case SessionActivityCommandKind.RestartCurrentActivity:
                    target = current;
                    return true;

                case SessionActivityCommandKind.GoToActivity01:
                    if (current.ActivityOrdinal == 1)
                    {
                        rejectionReason = "already_on_activity";
                        return false;
                    }

                    return TryResolveActivityByOrdinal(1, out target, out rejectionReason);

                case SessionActivityCommandKind.GoToActivity02:
                    if (current.ActivityOrdinal == 2)
                    {
                        rejectionReason = "already_on_activity";
                        return false;
                    }

                    return TryResolveActivityByOrdinal(2, out target, out rejectionReason);

                default:
                    throw new InvalidOperationException($"Unsupported navigation command kind '{kind}'.");
            }
        }

        private bool TryResolveActivityByOrdinal(int ordinal, out SessionActivityDefinition definition, out string rejectionReason)
        {
            if (_catalog.TryGetByOrdinal(ordinal, out definition) && definition.IsValid)
            {
                rejectionReason = string.Empty;
                return true;
            }

            definition = default;
            rejectionReason = "activity_not_found";
            return false;
        }

        private static string ResolveNavigationRejectionMessage(
            SessionActivityCommandKind kind,
            SessionActivityDefinition current,
            string rejectionReason)
        {
            return rejectionReason switch
            {
                "no_previous_activity" => $"Navigation '{kind}' has no previous activity before '{current.ActivityId}'.",
                "no_next_activity" => $"Navigation '{kind}' has no next activity after '{current.ActivityId}'.",
                "activity_not_found" => $"Navigation '{kind}' target activity was not found.",
                _ => $"Navigation '{kind}' cannot proceed from activity '{current.ActivityId}'."
            };
        }

        private SessionActivityIdentity BuildIdentity(SessionActivityDefinition definition, SessionActivityStage stage, int entrySequence)
        {
            if (!definition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityDefinition is invalid.");
            }

            return new SessionActivityIdentity(
                PipelineId,
                _sessionId,
                definition.ActivityId,
                definition.ActivityOrdinal,
                entrySequence,
                stage,
                "SessionActivityPipeline");
        }

        private int ResolveNextEntrySequence()
        {
            return _state.CurrentEntrySequence > 0 ? _state.CurrentEntrySequence + 1 : 1;
        }

        private SessionActivityFact EmitFact(
            List<SessionActivityFact> emittedFacts,
            SessionActivityFactKind kind,
            SessionActivityIdentity identity,
            string source,
            string reason,
            string message,
            SessionActivityHandoff handoff = default)
        {
            SessionActivityFact fact = new(kind, identity, source, reason, message, handoff);
            if (!fact.IsValid)
            {
                throw new InvalidOperationException($"Cannot emit invalid fact '{kind}'.");
            }

            emittedFacts.Add(fact);
            _state.AppendFact(fact);
            _state.AppendTrace($"[OBS][SessionActivityPipeline] fact='{fact.Kind}' stage='{fact.Identity.Stage}' entrySequence='{fact.Identity.EntrySequence}' simulationState='{_state.CurrentSimulationState}' activity='{fact.Identity.ActivityId}' source='{fact.Source}' reason='{fact.Reason}' message='{fact.Message}'");
            return fact;
        }

        private SessionActivitySnapshot EmitSnapshot(
            List<SessionActivitySnapshot> emittedSnapshots,
            string snapshotKind,
            string source,
            string reason,
            string message)
        {
            SessionActivitySnapshot snapshot = new SessionActivitySnapshot(
                _state.CurrentIdentity,
                _state.CurrentDefinition,
                _state.CurrentHandoff,
                source,
                reason,
                $"{snapshotKind}: {message}");

            if (!snapshot.IsValid)
            {
                throw new InvalidOperationException($"Cannot emit invalid snapshot '{snapshotKind}'.");
            }

            emittedSnapshots.Add(snapshot);
            _state.AppendSnapshot(snapshot);
            _state.AppendTrace($"[OBS][SessionActivityPipeline] snapshot='{snapshotKind}' identity='{snapshot.Identity}' entrySequence='{snapshot.Identity.EntrySequence}' simulationState='{_state.CurrentSimulationState}' activity='{snapshot.Definition.ActivityId}' source='{snapshot.Source}' reason='{snapshot.Reason}' message='{snapshot.Message}'");
            return snapshot;
        }

        private void EnsureStartedOrFail(string operation)
        {
            if (!_state.HasStarted)
            {
                throw new InvalidOperationException($"Operation '{operation}' requires the pipeline to be started.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

    }
}

