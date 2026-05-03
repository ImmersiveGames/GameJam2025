using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SimulationGate;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline
{
    public sealed class SessionActivityPipeline
    {
        private const string PipelineId = "SessionActivityPipeline.Base11.Sandbox";
        private readonly SessionActivityMiniCatalog _catalog;
        private readonly SessionActivityRuntimeState _state;
        private readonly SimulationGateService _simulationGate;
        private readonly string _sessionId;

        public SessionActivityPipeline(SessionActivityMiniCatalog catalog, string sessionId = "SessionActivitySandboxSession")
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _state = new SessionActivityRuntimeState();
            _simulationGate = new SimulationGateService();
            _sessionId = Normalize(sessionId);

            if (string.IsNullOrWhiteSpace(_sessionId))
            {
                throw new ArgumentException("sessionId is required.", nameof(sessionId));
            }

            if (!_catalog.TryGetFirst(out SessionActivityDefinition firstDefinition) || !firstDefinition.IsValid)
            {
                throw new InvalidOperationException("SessionActivityMiniCatalog requires Activity 01.");
            }
        }

        public SessionActivityRuntimeState State => _state;
        public SessionActivityMiniCatalog Catalog => _catalog;
        public SimulationGateState GateState => _simulationGate.State;

        public SessionActivityCommand BuildStartCommand(string source, string reason)
        {
            if (!_catalog.TryGetFirst(out SessionActivityDefinition firstDefinition) || !firstDefinition.IsValid)
            {
                throw new InvalidOperationException("Activity 01 is required to start the pipeline.");
            }

            return new SessionActivityCommand(
                SessionActivityCommandKind.StartDemo,
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
                return RejectTerminalCommand(SessionActivityCommandKind.StartDemo, source, reason);
            }

            if (_state.HasStarted)
            {
                return RejectStartCommand(source, reason);
            }

            return Execute(BuildStartCommand(source, reason));
        }

        public SessionActivityCommandResult DebugDirectStart(string source, string reason)
        {
            return Start(source, reason);
        }

        public SessionActivityCommandResult StartFromPreparedHandoff(
            _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline.SessionActivityEntryHandoff handoff,
            string source,
            string reason)
        {
            if (!handoff.IsValid)
            {
                throw new InvalidOperationException("SessionActivityEntryHandoff is invalid.");
            }

            if (IsTerminalCompleted())
            {
                return RejectTerminalCommand(SessionActivityCommandKind.StartDemo, source, reason);
            }

            if (_state.HasStarted)
            {
                return RejectStartCommand(source, reason);
            }

            SessionActivityDefinition initialDefinition = ResolveActivityByIdOrFail(handoff.ActivityId);
            if (initialDefinition.ActivityOrdinal != handoff.ActivityOrdinal)
            {
                throw new InvalidOperationException($"Prepared handoff activity ordinal mismatch. expected='{initialDefinition.ActivityOrdinal}' got='{handoff.ActivityOrdinal}'.");
            }

            if (handoff.EntrySequence <= 0)
            {
                throw new InvalidOperationException("Prepared handoff requires a positive entry sequence.");
            }

            SessionActivityIdentity activationIdentity = BuildIdentity(initialDefinition, SessionActivityStage.ActivationExecuting, handoff.EntrySequence);
            SessionActivityCommand command = new(
                SessionActivityCommandKind.StartDemo,
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
            _state.AppendTrace($"[OBS][SessionActivityPipeline] start_from_prepared_handoff handoff='{handoff}' source='{source}' reason='{reason}'");

            EmitFact(emittedFacts, SessionActivityFactKind.PipelineStarted, activationIdentity, source, reason, "Mini pipeline started from prepared handoff.");
            EmitSnapshot(emittedSnapshots, "pipeline_started_from_handoff", source, reason, "Pipeline started from prepared handoff.");

            EnterActivity(initialDefinition, command, emittedFacts, emittedSnapshots, handoff.EntrySequence);

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

        public SessionActivityCommandResult PauseSimulation(string source, string reason)
        {
            return ExecuteSimulationCommand(SessionActivityCommandKind.PauseSimulation, source, reason);
        }

        public SessionActivityCommandResult ResumeSimulation(string source, string reason)
        {
            return ExecuteSimulationCommand(SessionActivityCommandKind.ResumeSimulation, source, reason);
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

            if ((command.Kind == SessionActivityCommandKind.PauseSimulation ||
                command.Kind == SessionActivityCommandKind.ResumeSimulation) &&
                !_state.HasStarted)
            {
                return RejectWithoutActiveIdentity(command.Kind, command.Source, command.Reason, "pipeline_not_started");
            }

            List<SessionActivityFact> emittedFacts = new();
            List<SessionActivitySnapshot> emittedSnapshots = new();

            if (TryRejectStaleOrForeignCommand(command, emittedFacts, out SessionActivityCommandResult rejectedResult))
            {
                return rejectedResult;
            }

            switch (command.Kind)
            {
                case SessionActivityCommandKind.StartDemo:
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
                case SessionActivityCommandKind.PauseSimulation:
                    EmitPauseSimulation(command, emittedFacts, emittedSnapshots);
                    break;
                case SessionActivityCommandKind.ResumeSimulation:
                    EmitResumeSimulation(command, emittedFacts, emittedSnapshots);
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
                    SessionActivityFactKind.PhaseResultPresentationSkippedNoContent => SessionActivityCommandResultKind.SkipNoContent,
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

            EmitFact(facts, SessionActivityFactKind.PipelineStarted, activationIdentity, command.Source, command.Reason, "Mini pipeline started.");
            EmitSnapshot(snapshots, "pipeline_started", command.Source, command.Reason, "Pipeline started.");

            EnterActivity(firstDefinition, command, facts, snapshots, entrySequence);
        }

        private void EmitComplete(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (!EnsureExpectedStage(command, facts, SessionActivityStage.GameplayRunning, "complete_current_activity"))
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

            if (!current.HasPhaseResultPresentation)
            {
                SessionActivityIdentity skipIdentity = BuildIdentity(current, SessionActivityStage.PhaseResultPresentationSkippedNoContent, currentEntrySequence);
                _state.SetCurrentIdentity(skipIdentity, SessionActivityStage.PhaseResultPresentationSkippedNoContent);
                EmitFact(facts, SessionActivityFactKind.PhaseResultPresentationSkippedNoContent, skipIdentity, command.Source, command.Reason, $"'{current.ActivityId}' has no phase result presentation.");
                EmitSnapshot(snapshots, "phase_result_presentation_skipped_no_content", command.Source, command.Reason, $"'{current.ActivityId}' has no phase result presentation.");
            }
            else
            {
                SessionActivityIdentity presentationIdentity = BuildIdentity(current, SessionActivityStage.PhaseResultPresentationExecuting, currentEntrySequence);
                _state.SetCurrentIdentity(presentationIdentity, SessionActivityStage.PhaseResultPresentationExecuting);
                EmitFact(facts, SessionActivityFactKind.PhaseResultPresentationEntered, presentationIdentity, command.Source, command.Reason, $"'{current.ActivityId}' phase result presentation entered.");
                EmitSnapshot(snapshots, "phase_result_presentation_entered", command.Source, command.Reason, $"'{current.ActivityId}' phase result presentation entered.");
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
            if (!EnsureExpectedStage(command, facts, SessionActivityStage.PhaseResultPresentationExecuting, "continue_to_next_activity"))
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
            if (!EnsureExpectedStage(command, facts, SessionActivityStage.GameplayRunning, "navigation"))
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
                SessionActivityIdentity skipIdentity = BuildIdentity(definition, SessionActivityStage.GameplayRunning, entrySequence);
                _state.SetCurrentIdentity(skipIdentity, SessionActivityStage.GameplayRunning);
                _state.SetSimulationState(SessionActivitySimulationState.Running);
                EmitFact(facts, SessionActivityFactKind.GameplayContentSkippedNoContent, skipIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' gameplay content skipped as no-content.");
                EmitSnapshot(snapshots, "gameplay_content_skipped_no_content", command.Source, command.Reason, $"'{definition.ActivityId}' gameplay content skipped as no-content.");
                return;
            }

            SessionActivityIdentity runningIdentity = BuildIdentity(definition, SessionActivityStage.GameplayRunning, entrySequence);
            _state.SetCurrentIdentity(runningIdentity, SessionActivityStage.GameplayRunning);
            _state.SetSimulationState(SessionActivitySimulationState.Running);
            EmitFact(facts, SessionActivityFactKind.GameplayRunningEntered, runningIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' running.");
            EmitSnapshot(snapshots, "gameplay_running_entered", command.Source, command.Reason, $"'{definition.ActivityId}' running.");
        }

        private void EmitPauseSimulation(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (!EnsureExpectedStage(command, facts, SessionActivityStage.GameplayRunning, "pause_simulation"))
            {
                return;
            }

            if (!EnsureIdentityMatches(command, facts, _state.CurrentIdentity, "pause_simulation"))
            {
                return;
            }

            if (_state.CurrentSimulationState == SessionActivitySimulationState.Paused)
            {
                EmitRejected(command, facts, "simulation_already_paused", "Simulation is already paused.", _state.CurrentIdentity, true);
                return;
            }

            if (_state.CurrentSimulationState != SessionActivitySimulationState.Running)
            {
                throw new InvalidOperationException("Simulation state is invalid for pause.");
            }

            SimulationGateResult gateResult = ApplyActivityGateCommand(SimulationGateCommandKind.BlockActivitySimulation, command);
            if (gateResult.IsRejected)
            {
                EmitRejected(
                    command,
                    facts,
                    gateResult.Reason,
                    $"SimulationGate rejected pause command. {gateResult.Snapshot}",
                    _state.CurrentIdentity,
                    true);
                return;
            }

            _state.SetSimulationState(SessionActivitySimulationState.Paused);
            EmitFact(facts, SessionActivityFactKind.SimulationPaused, _state.CurrentIdentity, command.Source, command.Reason, "Simulation paused.");
            EmitSnapshot(snapshots, "simulation_paused", command.Source, command.Reason, "Simulation paused.");
        }

        private void EmitResumeSimulation(SessionActivityCommand command, List<SessionActivityFact> facts, List<SessionActivitySnapshot> snapshots)
        {
            if (!EnsureExpectedStage(command, facts, SessionActivityStage.GameplayRunning, "resume_simulation"))
            {
                return;
            }

            if (!EnsureIdentityMatches(command, facts, _state.CurrentIdentity, "resume_simulation"))
            {
                return;
            }

            if (_state.CurrentSimulationState == SessionActivitySimulationState.Running)
            {
                EmitRejected(command, facts, "simulation_not_paused", "Simulation is not paused.", _state.CurrentIdentity, true);
                return;
            }

            if (_state.CurrentSimulationState != SessionActivitySimulationState.Paused)
            {
                throw new InvalidOperationException("Simulation state is invalid for resume.");
            }

            SimulationGateResult gateResult = ApplyActivityGateCommand(SimulationGateCommandKind.ReleaseActivitySimulation, command);
            if (gateResult.IsRejected)
            {
                EmitRejected(
                    command,
                    facts,
                    gateResult.Reason,
                    $"SimulationGate rejected resume command. {gateResult.Snapshot}",
                    _state.CurrentIdentity,
                    true);
                return;
            }

            _state.SetSimulationState(SessionActivitySimulationState.Running);
            EmitFact(facts, SessionActivityFactKind.SimulationResumed, _state.CurrentIdentity, command.Source, command.Reason, "Simulation resumed.");
            EmitSnapshot(snapshots, "simulation_resumed", command.Source, command.Reason, "Simulation resumed.");
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

            if (_state.CurrentStage != SessionActivityStage.GameplayRunning)
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
                    SessionActivityCommandKind.StartDemo,
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

            return RejectWithoutActiveIdentity(SessionActivityCommandKind.StartDemo, source, reason, "pipeline_already_started");
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
                $"Operation '{kind}' requires stage '{SessionActivityStage.GameplayRunning}', but current stage is '{_state.CurrentStage}'.",
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
            if (kind == SessionActivityCommandKind.StartDemo)
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

            if (kind == SessionActivityCommandKind.PauseSimulation ||
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
