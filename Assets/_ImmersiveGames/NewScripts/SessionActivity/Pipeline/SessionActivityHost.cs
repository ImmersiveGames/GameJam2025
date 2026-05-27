using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionActivity.Adapters;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/NewScripts/SessionActivity/Session Activity Host")]
    public sealed class SessionActivityHost : MonoBehaviour, ISessionActivityRouteExitTeardownBoundary, ISessionActivityPredefinedVisualReadinessBoundary
    {
        private const int DumpRecentFactsCount = 24;
        private const int DumpRecentSnapshotsCount = 12;
        private const int DumpRecentTraceCount = 20;

        [Header("Config")]
        // Campo de tooling/QA. Nao e owner de lifecycle e nao pode iniciar Activity automaticamente.
        [SerializeField] private bool autoStart;
        [SerializeField] private string sessionStateId = "SessionActivitySandboxSession";
        [SerializeField] private ActivityCatalogAsset activityCatalog;
        [Header("QA Tooling")]
        [SerializeField] private bool qaObserveAsyncStateChanges = true;
        [SerializeField] private float qaObserveIntervalSeconds = 0.1f;

        private SessionActivityCatalog _catalog;
        private SessionActivityPipeline _pipeline;
        private string _lastObservedStateToken = string.Empty;
        private float _nextObserveAt;
        private bool _globalsRegistered;

        public event Action StateObservedChanged;

        public SessionActivityRuntimeState State => _pipeline?.State;
        public SessionActivityCatalog Catalog => _catalog;
        public SessionActivityPipeline Pipeline => _pipeline;
        public ActivityExecutionBlockingState GateState => _pipeline?.GateState;
        public SessionActivityRailKind CurrentRailKind => _pipeline != null ? _pipeline.ActiveRailKind : SessionActivityRailKind.None;
        public SessionActivityStage CurrentStage => _pipeline != null ? _pipeline.State.CurrentStage : SessionActivityStage.Unknown;
        public bool HasPendingOperation => _pipeline != null && _pipeline.State.CurrentPendingOperation.IsValid;

        private void Awake()
        {
            if (activityCatalog == null)
            {
                throw new InvalidOperationException("SessionActivityHost requires activityCatalog.");
            }

            _catalog = activityCatalog.BuildRuntimeCatalog();
            UnitySessionActivityWindowSceneAdapter windowSceneAdapter = new();
            UnityActivityContentSceneAdapter activityContentSceneAdapter = new();
            UnityActivityContentSceneReleaseAdapter activityContentSceneReleaseAdapter = new();
            _pipeline = new SessionActivityPipeline(
                _catalog,
                sessionStateId,
                new PauseOverlayAdapter(),
                new InputModeAdapter(),
                new SessionActivityTransitionAdapter(),
                new SessionActivityTransitionLoadingAdapter(),
                windowSceneAdapter,
                new UnitySessionActivityPendingOperationRunner(windowSceneAdapter, activityContentSceneAdapter, activityContentSceneReleaseAdapter));
            RegisterGlobal(_catalog);
            RegisterGlobal(_pipeline);
            RegisterGlobal<ISessionActivityEntryHandoffReceiver>(_pipeline);
            RegisterGlobal<ISessionActivitySnapshotPayloadProvider>(_pipeline);
            RegisterGlobal<ISessionActivityRouteExitTeardownBoundary>(this);
            RegisterGlobal<ISessionActivityPredefinedVisualReadinessBoundary>(this);
            _globalsRegistered = true;
            Debug.Log(BuildHostBanner());
        }

        private void Start()
        {
            if (autoStart)
            {
                if (IsQaDebugAllowed())
                {
                    Debug.Log($"[OBS][SessionActivityPipeline][Host] SessionActivityHostAutoStartIgnored sessionStateId='{sessionStateId}' autoStart='true' reason='auto_start_is_not_canonical_in_base11' source='SessionActivityHost/Start'.");
                    return;
                }

                throw new InvalidOperationException($"[FATAL][Config][SessionActivityPipeline][Host] SessionActivityHostAutoStartBlocked sessionStateId='{sessionStateId}' autoStart='true' reason='auto_start_is_not_allowed_in_runtime_normal'.");
            }
        }

        private void OnDisable()
        {
            if (_pipeline == null || !_pipeline.State.HasStarted || _pipeline.State.HasCompleted)
            {
                return;
            }

            SessionActivityStage stage = _pipeline.State.CurrentStage;
            if (stage == SessionActivityStage.Deactivation || stage == SessionActivityStage.Completed || stage == SessionActivityStage.ClosedForRouteExit)
            {
                return;
            }

            throw new InvalidOperationException(
                $"[FATAL][Lifecycle][SessionActivityPipeline][Host] SessionActivityRouteExitWithoutCanonicalDeactivation sessionStateId='{sessionStateId}' stage='{stage}' activityId='{_pipeline.State.CurrentDefinition.ActivityId}' reason='route_exit_or_scene_unload_requires_explicit_activity_closure_before_unload'.");
        }

        private void OnDestroy()
        {
            UnregisterGlobalsIfNeeded();
        }

        private void Update()
        {
            if (!qaObserveAsyncStateChanges || _pipeline == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextObserveAt)
            {
                return;
            }

            float interval = qaObserveIntervalSeconds <= 0f ? 0.1f : qaObserveIntervalSeconds;
            _nextObserveAt = now + interval;

            string currentToken = BuildStateObservationToken();
            if (string.Equals(currentToken, _lastObservedStateToken, StringComparison.Ordinal))
            {
                return;
            }

            _lastObservedStateToken = currentToken;
            StateObservedChanged?.Invoke();
            Debug.Log($"[OBS][SessionActivityPipeline][Host][StateObservedChanged] {currentToken}");
        }

        public void DebugStartActivity()
        {
            throw new InvalidOperationException(
                $"[FATAL][Contract][SessionActivityPipeline][Host] SessionActivityHostDebugStartRemoved sessionStateId='{sessionStateId}' reason='debug_start_is_not_allowed_in_canonical_qa_lifecycle'.");
        }

        public void CompleteCurrentActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.CompleteCurrentActivity(QaSource("CompleteCurrentActivity"), QaReason("CompleteCurrentActivity"));
            LogResult("CompleteCurrentActivity", result);
        }

        public void CompleteActivationWindow()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.CompleteActivationWindow(QaSource("CompleteActivationWindow"), QaReason("CompleteActivationWindow"));
            LogResult("CompleteActivationWindow", result);
        }

        public void CompleteDeactivationWindow()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.CompleteDeactivationWindow(QaSource("CompleteDeactivationWindow"), QaReason("CompleteDeactivationWindow"));
            LogResult("CompleteDeactivationWindow", result);
        }

        public void ContinueToNextActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.ContinueToNextActivity(QaSource("ContinueToNextActivity"), QaReason("ContinueToNextActivity"));
            LogResult("ContinueToNextActivity", result);
        }

        public void GoToNextActivity()
        {
            throw new InvalidOperationException(
                $"[FATAL][Contract][SessionActivityPipeline][Host] SessionActivityHostNavigationBypassBlocked sessionStateId='{sessionStateId}' command='GoToNextActivity' reason='qa_navigation_shortcuts_are_removed_from_canonical_lifecycle'.");
        }

        public void GoToPreviousActivity()
        {
            throw new InvalidOperationException(
                $"[FATAL][Contract][SessionActivityPipeline][Host] SessionActivityHostNavigationBypassBlocked sessionStateId='{sessionStateId}' command='GoToPreviousActivity' reason='qa_navigation_shortcuts_are_removed_from_canonical_lifecycle'.");
        }

        public void RestartCurrentActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.RestartCurrentActivity(QaSource("RestartCurrentActivity"), QaReason("RestartCurrentActivity"));
            LogResult("RestartCurrentActivity", result);
        }

        public void GoToActivity(string activityId)
        {
            throw new InvalidOperationException(
                $"[FATAL][Contract][SessionActivityPipeline][Host] SessionActivityHostNavigationBypassBlocked sessionStateId='{sessionStateId}' command='GoToActivity' targetActivityId='{activityId}' reason='qa_navigation_shortcuts_are_removed_from_canonical_lifecycle'.");
        }

        public void RequestPause()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.PauseRequested(QaSource("RequestPause"), QaReason("RequestPause"));
            LogResult("RequestPause", result);
        }

        public void RequestResume()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.ResumeRequested(QaSource("RequestResume"), QaReason("RequestResume"));
            LogResult("RequestResume", result);
        }

        internal bool QaSubtractActorAttribute(string actorId, string attributeId, float amount = 10f)
        {
            return QaApplyActorAttributeCommand("QaSubtractActorAttribute", actorId, attributeId, ActorAttributeOperation.Subtract, amount, 0f);
        }

        internal bool QaAddActorAttribute(string actorId, string attributeId, float amount = 5f)
        {
            return QaApplyActorAttributeCommand("QaAddActorAttribute", actorId, attributeId, ActorAttributeOperation.Add, amount, 0f);
        }

        internal bool QaSetActorAttribute(string actorId, string attributeId, float value)
        {
            return QaApplyActorAttributeCommand("QaSetActorAttribute", actorId, attributeId, ActorAttributeOperation.Set, 0f, value);
        }

        internal bool QaResetActorAttributeToInitial(string actorId, string attributeId)
        {
            return QaApplyActorAttributeCommand("QaResetActorAttributeToInitial", actorId, attributeId, ActorAttributeOperation.ResetToInitial, 0f, 0f);
        }

        internal bool QaRestoreActorAttributeToMax(string actorId, string attributeId)
        {
            return QaApplyActorAttributeCommand("QaRestoreActorAttributeToMax", actorId, attributeId, ActorAttributeOperation.RestoreToMax, 0f, 0f);
        }

        public SessionActivityRouteExitTeardownResult RequestRouteExitTeardown(string requestedSessionStateId, string source, string reason)
        {
            EnsurePipeline();
            string normalizedSessionStateId = Normalize(requestedSessionStateId);
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);

            if (string.IsNullOrWhiteSpace(normalizedSessionStateId))
            {
                throw new InvalidOperationException("RequestRouteExitTeardown requires sessionStateId.");
            }

            if (!string.Equals(normalizedSessionStateId, sessionStateId, StringComparison.Ordinal))
            {
                return BuildRouteExitTeardownFailed(
                    "route_exit_failed_stale_or_foreign_session_activity_teardown_request",
                    $"teardown request sessionStateId='{normalizedSessionStateId}' does not match host sessionStateId='{sessionStateId}'.");
            }

            SessionActivityStage stage = _pipeline.State.CurrentStage;
            bool hasPendingHandoff = _pipeline.State.CurrentHandoff.IsValid;

            if (!_pipeline.State.HasStarted)
            {
                return new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.NotRequired,
                    sessionStateId,
                    stage,
                    _pipeline.State.CurrentDefinition.ActivityId,
                    hasPendingHandoff,
                    "route_exit_not_required_no_active_session_activity",
                    "Pipeline is not started.");
            }

            if (_pipeline.State.HasCompleted)
            {
                if ((stage == SessionActivityStage.Deactivation || stage == SessionActivityStage.Completed || stage == SessionActivityStage.ClosedForRouteExit) && !hasPendingHandoff)
                {
                    return new SessionActivityRouteExitTeardownResult(
                        SessionActivityRouteExitTeardownKind.Completed,
                        sessionStateId,
                        stage,
                        _pipeline.State.CurrentDefinition.ActivityId,
                        false,
                        "route_exit_completed",
                        "SessionActivity route-exit teardown is already completed.");
                }

                return new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.NotRequired,
                    sessionStateId,
                    stage,
                    _pipeline.State.CurrentDefinition.ActivityId,
                    hasPendingHandoff,
                    "route_exit_not_required_no_active_session_activity",
                    "SessionActivity is already completed without an active route-exit rail.");
            }

            if (_pipeline.State.CurrentPendingOperation.IsValid)
            {
                return BuildRouteExitTeardownInProgress(
                    "route_exit_in_progress_pending_operation_active",
                    $"SessionActivity route-exit teardown is in progress while pending operation is active. pendingOperation='{_pipeline.State.CurrentPendingOperation}'.");
            }

            if (hasPendingHandoff)
            {
                return BuildRouteExitTeardownFailed(
                    "route_exit_failed_pending_handoff_present",
                    $"SessionActivity route-exit teardown cannot complete while handoff is pending. handoff='{_pipeline.State.CurrentHandoff}'.");
            }

            if (stage == SessionActivityStage.Deactivation || stage == SessionActivityStage.Completed || stage == SessionActivityStage.ClosedForRouteExit)
            {
                return new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.Completed,
                    sessionStateId,
                    stage,
                    _pipeline.State.CurrentDefinition.ActivityId,
                    false,
                    "route_exit_completed_already_deactivated",
                    "Activity is already deactivated.");
            }

            if (IsDeactivationTransitionStage(stage) && _pipeline.ActiveRailKind != SessionActivityRailKind.ActivityRouteExitRail)
            {
                return BuildRouteExitTeardownFailed(
                    "deactivation_window_not_route_exit_owned",
                    $"SessionActivity is progressing through local activity transition and route-exit is not owner. stage='{stage}' activityId='{_pipeline.State.CurrentDefinition.ActivityId}' entrySequence='{_pipeline.State.CurrentEntrySequence}'.");
            }

            if (IsRouteExitTransitStage(stage))
            {
                return BuildRouteExitTeardownInProgress(
                    "route_exit_in_progress_transient_stage",
                    $"SessionActivity route-exit teardown is in progress at stage '{stage}'.");
            }

            SessionActivityCommandResult closeForRouteExitResult = _pipeline.CloseForRouteExit(
                normalizedSource,
                $"{normalizedReason}/route_exit_teardown_close_for_route_exit");
            if (!closeForRouteExitResult.IsValid || closeForRouteExitResult.IsRejected || closeForRouteExitResult.IsFailed)
            {
                return BuildRouteExitTeardownFailed(
                    "route_exit_close_for_route_exit_rejected",
                    $"CloseForRouteExit rejected during route exit teardown. resultKind='{closeForRouteExitResult.Kind}' reason='{closeForRouteExitResult.Reason}'.");
            }

            stage = _pipeline.State.CurrentStage;
            hasPendingHandoff = _pipeline.State.CurrentHandoff.IsValid;
            if ((stage == SessionActivityStage.Deactivation || stage == SessionActivityStage.Completed || stage == SessionActivityStage.ClosedForRouteExit) && !hasPendingHandoff)
            {
                return new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.Completed,
                    sessionStateId,
                    stage,
                    _pipeline.State.CurrentDefinition.ActivityId,
                    false,
                    "route_exit_completed",
                    "SessionActivity deactivated before route unload.");
            }

            if (closeForRouteExitResult.IsStarted)
            {
                return new SessionActivityRouteExitTeardownResult(
                    SessionActivityRouteExitTeardownKind.Started,
                    sessionStateId,
                    stage,
                    _pipeline.State.CurrentDefinition.ActivityId,
                    hasPendingHandoff,
                    "route_exit_started_close_for_route_exit_accepted",
                    $"SessionActivity route-exit teardown started at stage '{stage}'.");
            }

            return BuildRouteExitTeardownInProgress(
                "route_exit_in_progress_not_completed_before_unload",
                $"SessionActivity route-exit teardown has started and is not completed yet. stage='{stage}'.");
        }

        public SessionActivityPredefinedVisualReadinessResult ObservePredefinedVisualReadiness(
            string sessionStateId,
            string expectedRouteOperationId,
            string source,
            string reason)
        {
            EnsurePipeline();
            return _pipeline.ObservePredefinedVisualReadiness(sessionStateId, expectedRouteOperationId, source, reason);
        }

        public SessionActivityCommandResult ExecuteCommand(SessionActivityCommand command, string actionLabel)
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.Execute(command);
            LogResult(actionLabel, result);
            return result;
        }

        public string DumpState()
        {
            EnsurePipeline();
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][Host] DumpState");
            builder.AppendLine($"sessionStateId='{sessionStateId}' autoStart='{autoStart}'");
            builder.AppendLine($"pipelineId='{State.PipelineId}' sessionStateId='{State.SessionId}'");
            builder.AppendLine($"entrySequence='{State.CurrentEntrySequence}'");
            builder.AppendLine($"executionState='{State.CurrentExecutionState}'");
            builder.AppendLine($"gateState='{GateState}'");
            builder.AppendLine($"catalog='{_catalog.Summary}'");
            builder.AppendLine($"started='{State.HasStarted}' completed='{State.HasCompleted}' stage='{State.CurrentStage}'");
            builder.AppendLine($"currentActivity='{State.CurrentDefinition.ActivityId}'");
            builder.AppendLine($"definition='{State.CurrentDefinition}'");
            builder.AppendLine($"identity='{State.CurrentIdentity}'");
            builder.AppendLine($"handoff='{State.CurrentHandoff}'");
            builder.AppendLine($"pendingOperation='{State.CurrentPendingOperation}'");
            builder.AppendLine($"activityContentLoadedSet='{State.CurrentActivityContentLoadedSet}'");
            builder.AppendLine($"activitySetupInventory='{FormatActivitySetupInventory(State)}'");
            builder.AppendLine($"pendingHandoffTarget='{GetPendingHandoffTarget()}'");
            builder.AppendLine($"nextExpectedQaAction='{GetNextExpectedQaAction()}'");
            builder.AppendLine("qaLifecycleRail='ActivityRunning -> CompleteCurrentActivity/RestartCurrentActivity; CompleteActivationWindow/CompleteDeactivationWindow apenas quando window stage=Ready; ContinueToNextActivity apenas se policy=ManualContinue'");
            builder.AppendLine($"factsCount='{State.Facts.Count}' snapshotsCount='{State.Snapshots.Count}' traceCount='{State.Trace.Count}'");
            builder.AppendLine($"recentFacts(last={DumpRecentFactsCount}):");
            for (int index = Math.Max(0, State.Facts.Count - DumpRecentFactsCount); index < State.Facts.Count; index++)
            {
                builder.AppendLine($"- {State.Facts[index]}");
            }
            builder.AppendLine("checkpointEvidenceFacts(all):");
            for (int index = 0; index < State.Facts.Count; index++)
            {
                SessionActivityFact fact = State.Facts[index];
                if (fact.Kind == SessionActivityFactKind.ActivityContentLoadSkippedNoContent ||
                    fact.Kind == SessionActivityFactKind.ActivitySetupStarted ||
                    fact.Kind == SessionActivityFactKind.ActivitySetupInventoryBuildStarted ||
                    fact.Kind == SessionActivityFactKind.ActivitySetupInventoryBuilt ||
                    fact.Kind == SessionActivityFactKind.ActivitySetupInventoryValidated ||
                    fact.Kind == SessionActivityFactKind.ActivityParticipantBindingStarted ||
                    fact.Kind == SessionActivityFactKind.ActivityParticipantBindingSkippedNoRequirements ||
                    fact.Kind == SessionActivityFactKind.ActivityParticipantBindingCompleted ||
                    fact.Kind == SessionActivityFactKind.ActivitySetupCompleted ||
                    fact.Kind == SessionActivityFactKind.ActivityActivationStarted ||
                    fact.Kind == SessionActivityFactKind.ActivityRunningEntered ||
                    fact.Kind == SessionActivityFactKind.GameplayContentSkippedNoContent)
                {
                    builder.AppendLine($"- {fact}");
                }
            }

            builder.AppendLine($"recentSnapshots(last={DumpRecentSnapshotsCount}):");
            for (int index = Math.Max(0, State.Snapshots.Count - DumpRecentSnapshotsCount); index < State.Snapshots.Count; index++)
            {
                builder.AppendLine($"- {State.Snapshots[index]}");
            }

            builder.AppendLine($"recentTrace(last={DumpRecentTraceCount}):");
            for (int index = Math.Max(0, State.Trace.Count - DumpRecentTraceCount); index < State.Trace.Count; index++)
            {
                builder.AppendLine($"- {State.Trace[index]}");
            }

            string dump = builder.ToString().TrimEnd();
            Debug.Log(dump);
            return dump;
        }

        public string DumpTrace()
        {
            EnsurePipeline();
            string trace = BuildTraceDump();
            Debug.Log(trace);
            return trace;
        }

        public string DumpActivityContentReleaseEvidence()
        {
            EnsurePipeline();
            StringBuilder builder = new();
            SessionActivityRuntimeState state = State;
            SessionActivityPendingOperation pendingOperation = state.CurrentPendingOperation;

            int currentEntrySequence = state.CurrentEntrySequence;
            int pendingReleaseEntrySequence = _pipeline.PendingActivityContentReleaseEntrySequence;
            int previousActivityEntrySequence = ResolvePreviousActivityEntrySequence(state, currentEntrySequence);
            int lastCompletedActivityEntrySequence = ResolveLastCompletedActivityEntrySequence(state);
            string focusActivityId = ResolveReleaseFocusActivityId(state);

            HashSet<int> targetEntrySequences = new();
            AddEntrySequence(targetEntrySequences, currentEntrySequence);
            AddEntrySequence(targetEntrySequences, pendingReleaseEntrySequence);
            AddEntrySequence(targetEntrySequences, previousActivityEntrySequence);
            AddEntrySequence(targetEntrySequences, lastCompletedActivityEntrySequence);

            builder.AppendLine("[OBS][SessionActivityPipeline][Host] DumpActivityContentReleaseEvidence");
            AppendDumpSummary(builder);
            builder.AppendLine($"pendingActivityContentReleaseContext='{_pipeline.PendingActivityContentReleaseSummary}'");
            builder.AppendLine($"awaitingContinuationAfterActivityContentRelease='{_pipeline.AwaitingContinuationAfterActivityContentRelease}'");
            builder.AppendLine($"focusActivityId='{focusActivityId}'");
            builder.AppendLine($"entrySequenceFilter='current:{currentEntrySequence},pending:{pendingReleaseEntrySequence},previous:{previousActivityEntrySequence},lastCompleted:{lastCompletedActivityEntrySequence}'");

            builder.AppendLine("facts(release_evidence):");
            int factsCount = 0;
            for (int i = 0; i < state.Facts.Count; i++)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!ShouldIncludeReleaseEvidenceFact(fact))
                {
                    continue;
                }

                if (!fact.Identity.IsValid)
                {
                    continue;
                }

                if (!string.Equals(fact.Identity.ActivityId, focusActivityId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!targetEntrySequences.Contains(fact.Identity.EntrySequence))
                {
                    continue;
                }

                factsCount++;
                builder.AppendLine($"- kind='{fact.Kind}' activityId='{fact.Identity.ActivityId}' entrySequence='{fact.Identity.EntrySequence}' stage='{fact.Identity.Stage}' message=\"{fact.Message}\"");
            }

            if (factsCount == 0)
            {
                builder.AppendLine("- <none>");
            }

            builder.AppendLine("trace(filtered_operation_kind):");
            int traceCount = 0;
            for (int i = 0; i < state.Trace.Count; i++)
            {
                string line = state.Trace[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.IndexOf("ActivityContentSceneUnload", StringComparison.Ordinal) < 0 &&
                    line.IndexOf("ActivityContentSceneLoad", StringComparison.Ordinal) < 0 &&
                    line.IndexOf("DeactivationWindowSceneUnload", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                traceCount++;
                builder.AppendLine($"- {line}");
            }

            if (traceCount == 0)
            {
                builder.AppendLine("- <none>");
            }

            builder.AppendLine("scene_state:");
            builder.AppendLine($"- scene='ActivityScene01' isLoaded='{ResolveSceneLoaded("ActivityScene01")}'");
            builder.AppendLine($"- scene='SessionActivitySandboxScene' isLoaded='{ResolveSceneLoaded("SessionActivitySandboxScene")}'");
            builder.AppendLine($"- scene='IntroScene01' isLoaded='{ResolveSceneLoaded("IntroScene01")}'");
            builder.AppendLine($"- scene='IntroScene02' isLoaded='{ResolveSceneLoaded("IntroScene02")}'");

            string dump = builder.ToString().TrimEnd();
            Debug.Log(dump);
            return dump;
        }

        public string DumpCurrentActivityEvidence()
        {
            EnsurePipeline();
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][Host] DumpCurrentActivityEvidence");
            AppendDumpSummary(builder);
            builder.AppendLine("facts(current_activity_entry):");
            AppendFactsByEntrySequence(builder, State.CurrentDefinition.ActivityId, State.CurrentEntrySequence, ShouldIncludeCurrentActivityEvidenceFact);
            string dump = builder.ToString().TrimEnd();
            Debug.Log(dump);
            return dump;
        }

        public string DumpParticipantBindingEvidence()
        {
            EnsurePipeline();
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][Host] DumpParticipantBindingEvidence");
            AppendDumpSummary(builder);
            builder.AppendLine("facts(participant_binding):");
            AppendFactsByEntrySequence(builder, State.CurrentDefinition.ActivityId, State.CurrentEntrySequence, ShouldIncludeParticipantBindingEvidenceFact);
            string dump = builder.ToString().TrimEnd();
            Debug.Log(dump);
            return dump;
        }

        public string DumpTransitionEvidence()
        {
            EnsurePipeline();
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][Host] DumpTransitionEvidence");
            AppendDumpSummary(builder);
            builder.AppendLine("facts(transition):");
            AppendRecentFacts(builder, ShouldIncludeTransitionEvidenceFact);
            builder.AppendLine("trace(filtered_operation_kind):");
            int traceCount = 0;
            for (int i = 0; i < State.Trace.Count; i++)
            {
                string line = State.Trace[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.IndexOf("ActivityTransition", StringComparison.Ordinal) < 0 &&
                    line.IndexOf("DeactivationWindowSceneUnload", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                traceCount++;
                builder.AppendLine($"- {line}");
            }

            if (traceCount == 0)
            {
                builder.AppendLine("- <none>");
            }

            string dump = builder.ToString().TrimEnd();
            Debug.Log(dump);
            return dump;
        }

        public string DumpSceneState()
        {
            EnsurePipeline();
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][Host] DumpSceneState");
            AppendDumpSummary(builder);
            builder.AppendLine("scene_state:");
            builder.AppendLine($"- scene='ActivityScene01' isLoaded='{ResolveSceneLoaded("ActivityScene01")}'");
            builder.AppendLine($"- scene='SessionActivitySandboxScene' isLoaded='{ResolveSceneLoaded("SessionActivitySandboxScene")}'");
            builder.AppendLine($"- scene='IntroScene01' isLoaded='{ResolveSceneLoaded("IntroScene01")}'");
            builder.AppendLine($"- scene='IntroScene02' isLoaded='{ResolveSceneLoaded("IntroScene02")}'");
            string dump = builder.ToString().TrimEnd();
            Debug.Log(dump);
            return dump;
        }

        private void EnsurePipeline()
        {
            if (_pipeline != null)
            {
                return;
            }

            throw new InvalidOperationException("SessionActivityHost pipeline is not initialized.");
        }

        private void LogResult(string action, SessionActivityCommandResult result)
        {
            if (!result.IsValid)
            {
                throw new InvalidOperationException($"Invalid result returned by action '{action}'.");
            }

            string outcome = result.Kind.ToString();
            Debug.Log($"[OBS][SessionActivityPipeline][Host] action='{action}' outcomeKind='{outcome}' reason='{result.Reason}' entrySequence='{State.CurrentEntrySequence}' executionState='{State.CurrentExecutionState}' gateState='{GateState}'");

            for (int index = 0; index < result.Facts.Count; index++)
            {
                Debug.Log($"[OBS][SessionActivityPipeline][Host][ResultFact] {result.Facts[index]}");
            }

            Debug.Log($"[OBS][SessionActivityPipeline][Host] factsCount='{State.Facts.Count}' snapshotsCount='{State.Snapshots.Count}' traceCount='{State.Trace.Count}'");
        }

        private string BuildTraceDump()
        {
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][Host] Trace");
            for (int index = 0; index < State.Trace.Count; index++)
            {
                builder.AppendLine(State.Trace[index]);
            }

            return builder.ToString().TrimEnd();
        }

        private string GetPendingHandoffTarget()
        {
            return State.CurrentHandoff.IsValid
                ? State.CurrentHandoff.NextActivityId
                : "<none>";
        }

        private string GetNextExpectedQaAction()
        {
            SessionActivityStage stage = State.CurrentStage;
            bool hasPendingHandoff = State.CurrentHandoff.IsValid;

            if (stage == SessionActivityStage.ActivityContentProfileResolved ||
                stage == SessionActivityStage.ActivityContentLoadStarted ||
                stage == SessionActivityStage.ActivityContentSceneLoading ||
                stage == SessionActivityStage.ActivityContentSceneLoaded ||
                stage == SessionActivityStage.ActivityContentLoadedSetReady ||
                stage == SessionActivityStage.ActivityContentLoadSkippedNoContent ||
                stage == SessionActivityStage.ActivityContentLoadFailed ||
                stage == SessionActivityStage.ActivitySetupInventoryBuildStarted ||
                stage == SessionActivityStage.ActivitySetupInventoryBuilt ||
                stage == SessionActivityStage.ActivitySetupInventoryValidated ||
                stage == SessionActivityStage.ActivitySetupInventorySkippedNoRequirements ||
                stage == SessionActivityStage.ActivitySetupInventoryValidationFailed ||
                stage == SessionActivityStage.ActivityParticipantBindingStarted ||
                stage == SessionActivityStage.ActivityParticipantBindingSkippedNoRequirements ||
                stage == SessionActivityStage.ActivityParticipantBindingCompleted ||
                stage == SessionActivityStage.ActivityParticipantBindingFailed)
            {
                return "No local QA action";
            }

            if (stage == SessionActivityStage.ActivationWindowReady)
            {
                return "CompleteActivationWindow";
            }

            if (stage == SessionActivityStage.ActivityRunning)
            {
                return "CompleteCurrentActivity";
            }

            if (stage == SessionActivityStage.DeactivationWindowReady)
            {
                return "CompleteDeactivationWindow";
            }

            if (stage == SessionActivityStage.NextActivitySetupCompleted &&
                hasPendingHandoff &&
                ResolveCurrentContinuePolicy() == ActivityTransitionContinuePolicy.ManualContinue)
            {
                return "ContinueToNextActivity";
            }

            if (stage == SessionActivityStage.Completed || stage == SessionActivityStage.ClosedForRouteExit)
            {
                return "No local QA action";
            }

            return "No local QA action";
        }

        private string BuildHostBanner()
        {
            return $"[OBS][SessionActivityPipeline][Host] initialized sessionStateId='{sessionStateId}' autoStart='{autoStart}' entrySequence='{State.CurrentEntrySequence}' executionState='{State.CurrentExecutionState}' gateState='{GateState}' catalog='{_catalog.Summary}'";
        }

        private string BuildStateObservationToken()
        {
            SessionActivityPendingOperation pending = State.CurrentPendingOperation;
            string pendingToken = pending.IsValid
                ? $"{pending.OperationKind}/{pending.OperationId}/{pending.WindowKind}"
                : "<none>";
            string handoffToken = State.CurrentHandoff.IsValid
                ? $"{State.CurrentHandoff.NextActivityId}/{State.CurrentHandoff.ToIdentity.EntrySequence}"
                : "<none>";
            return $"stage='{State.CurrentStage}' entrySequence='{State.CurrentEntrySequence}' activity='{State.CurrentDefinition.ActivityId}' pendingOperation='{pendingToken}' handoff='{handoffToken}'";
        }

        private static string QaSource(string action)
        {
            return $"SessionActivityHost/QA/{action}";
        }

        private static string QaReason(string action)
        {
            return $"SessionActivityHost/QA/{action}";
        }

        private static bool IsQaDebugAllowed()
        {
            return Application.isEditor || Debug.isDebugBuild;
        }

        private static bool IsRouteExitTransitStage(SessionActivityStage stage)
        {
            return stage == SessionActivityStage.ActivityCompletionRequested ||
                   stage == SessionActivityStage.ActivityCompleting ||
                   stage == SessionActivityStage.PlayerActorParticipationExitStageStarted ||
                   stage == SessionActivityStage.PlayerActorParticipationExitStageCompleted ||
                   stage == SessionActivityStage.DeactivationWindowStarted ||
                   stage == SessionActivityStage.DeactivationWindowSceneLoading ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoadStarted ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoaded ||
                   stage == SessionActivityStage.DeactivationWindowReady ||
                   stage == SessionActivityStage.DeactivationWindowCompleted ||
                   stage == SessionActivityStage.DeactivationWindowSceneUnloading ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloadStarted ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloaded ||
                   stage == SessionActivityStage.DeactivationWindowSkippedNoContent;
        }

        private static bool IsDeactivationTransitionStage(SessionActivityStage stage)
        {
            return stage == SessionActivityStage.ActivityCompletionRequested ||
                   stage == SessionActivityStage.ActivityCompleting ||
                   stage == SessionActivityStage.PlayerActorParticipationExitStageStarted ||
                   stage == SessionActivityStage.PlayerActorParticipationExitStageCompleted ||
                   stage == SessionActivityStage.DeactivationWindowStarted ||
                   stage == SessionActivityStage.DeactivationWindowSceneLoading ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoadStarted ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoaded ||
                   stage == SessionActivityStage.DeactivationWindowReady ||
                   stage == SessionActivityStage.DeactivationWindowCompleted ||
                   stage == SessionActivityStage.DeactivationWindowSceneUnloading ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloadStarted ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloaded ||
                   stage == SessionActivityStage.DeactivationWindowSkippedNoContent;
        }

        private static string ResolveSceneLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return "false";
            }

            Scene scene = SceneManager.GetSceneByName(sceneName.Trim());
            return (scene.IsValid() && scene.isLoaded) ? "true" : "false";
        }

        private static bool ShouldIncludeReleaseEvidenceFact(SessionActivityFact fact)
        {
            if (!fact.IsValid)
            {
                return false;
            }

            return fact.Kind == SessionActivityFactKind.ActivityDeactivated ||
                   fact.Kind == SessionActivityFactKind.ActivityContentReleaseStarted ||
                   fact.Kind == SessionActivityFactKind.ActivityContentRetentionPlanResolved ||
                   fact.Kind == SessionActivityFactKind.ActivityContentSceneUnloadCommandIssued ||
                   fact.Kind == SessionActivityFactKind.ActivityContentSceneUnloaded ||
                   fact.Kind == SessionActivityFactKind.ActivityContentReleaseCompleted ||
                   fact.Kind == SessionActivityFactKind.ActivityContentReleaseSkippedNoContent ||
                   fact.Kind == SessionActivityFactKind.ActivityContentReleaseFailed ||
                   fact.Kind == SessionActivityFactKind.ActivityContentSceneUnloadRejected ||
                   fact.Kind == SessionActivityFactKind.ContinueAccepted ||
                   fact.Kind == SessionActivityFactKind.ActivityTransitionFadeInStarted ||
                   fact.Kind == SessionActivityFactKind.ActivityTransitionFadeInCompleted ||
                   fact.Kind == SessionActivityFactKind.ActivityTransitionFadeOutStarted ||
                   fact.Kind == SessionActivityFactKind.ActivityTransitionFadeOutCompleted ||
                   fact.Kind == SessionActivityFactKind.ActivityTransitionCompleted ||
                   fact.Kind == SessionActivityFactKind.ActivityRunningEntered;
        }

        private static bool ShouldIncludeCurrentActivityEvidenceFact(SessionActivityFact fact)
        {
            if (!fact.IsValid)
            {
                return false;
            }

            return fact.Kind == SessionActivityFactKind.ActivitySetupStarted ||
                   fact.Kind == SessionActivityFactKind.ActivitySetupCompleted ||
                   fact.Kind == SessionActivityFactKind.ActivityActivationStarted ||
                   fact.Kind == SessionActivityFactKind.ActivityRunningEntered ||
                   fact.Kind == SessionActivityFactKind.ActivityCompletionRequested ||
                   fact.Kind == SessionActivityFactKind.ActivityCompleting ||
                   fact.Kind == SessionActivityFactKind.ActivityDeactivated ||
                   fact.Kind == SessionActivityFactKind.ContinueAccepted;
        }

        private static bool ShouldIncludeParticipantBindingEvidenceFact(SessionActivityFact fact)
        {
            if (!fact.IsValid)
            {
                return false;
            }

            return fact.Kind == SessionActivityFactKind.ActivitySetupInventoryBuildStarted ||
                   fact.Kind == SessionActivityFactKind.ActivitySetupInventoryBuilt ||
                   fact.Kind == SessionActivityFactKind.ActivitySetupInventoryValidated ||
                   fact.Kind == SessionActivityFactKind.ActivitySetupInventorySkippedNoRequirements ||
                   fact.Kind == SessionActivityFactKind.ActivityParticipantBindingStarted ||
                   fact.Kind == SessionActivityFactKind.ActivityParticipantBindingSkippedNoRequirements ||
                   fact.Kind == SessionActivityFactKind.ActivityParticipantBindingCompleted ||
                   fact.Kind == SessionActivityFactKind.ActivityParticipantBindingFailed;
        }

        private static bool ShouldIncludeTransitionEvidenceFact(SessionActivityFact fact)
        {
            if (!fact.IsValid)
            {
                return false;
            }

            return fact.Kind == SessionActivityFactKind.ContinueAccepted ||
                   fact.Kind == SessionActivityFactKind.ActivityTransitionFadeInStarted ||
                   fact.Kind == SessionActivityFactKind.ActivityTransitionFadeInCompleted ||
                   fact.Kind == SessionActivityFactKind.ActivityTransitionFadeOutStarted ||
                   fact.Kind == SessionActivityFactKind.ActivityTransitionFadeOutCompleted ||
                   fact.Kind == SessionActivityFactKind.ActivityTransitionCompleted ||
                   fact.Kind == SessionActivityFactKind.ActivityRunningEntered;
        }

        private void AppendDumpSummary(StringBuilder builder)
        {
            builder.AppendLine($"currentActivityId='{State.CurrentDefinition.ActivityId}'");
            builder.AppendLine($"currentEntrySequence='{State.CurrentEntrySequence}'");
            builder.AppendLine($"currentStage='{State.CurrentStage}'");
            builder.AppendLine($"pendingOperation='{State.CurrentPendingOperation}'");
            builder.AppendLine($"currentActivityContentLoadedSet='{State.CurrentActivityContentLoadedSet}'");
            builder.AppendLine($"pendingHandoffTarget='{GetPendingHandoffTarget()}'");
            builder.AppendLine($"nextExpectedQaAction='{GetNextExpectedQaAction()}'");
        }

        private void AppendFactsByEntrySequence(StringBuilder builder, string activityId, int entrySequence, Func<SessionActivityFact, bool> includePredicate)
        {
            int count = 0;
            for (int i = 0; i < State.Facts.Count; i++)
            {
                SessionActivityFact fact = State.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid || !includePredicate(fact))
                {
                    continue;
                }

                if (!string.Equals(fact.Identity.ActivityId, activityId, StringComparison.Ordinal) ||
                    fact.Identity.EntrySequence != entrySequence)
                {
                    continue;
                }

                count++;
                builder.AppendLine($"- kind='{fact.Kind}' activityId='{fact.Identity.ActivityId}' entrySequence='{fact.Identity.EntrySequence}' stage='{fact.Identity.Stage}' message=\"{fact.Message}\"");
            }

            if (count == 0)
            {
                builder.AppendLine("- <none>");
            }
        }

        private void AppendRecentFacts(StringBuilder builder, Func<SessionActivityFact, bool> includePredicate)
        {
            int count = 0;
            for (int i = 0; i < State.Facts.Count; i++)
            {
                SessionActivityFact fact = State.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid || !includePredicate(fact))
                {
                    continue;
                }

                count++;
                builder.AppendLine($"- kind='{fact.Kind}' activityId='{fact.Identity.ActivityId}' entrySequence='{fact.Identity.EntrySequence}' stage='{fact.Identity.Stage}' message=\"{fact.Message}\"");
            }

            if (count == 0)
            {
                builder.AppendLine("- <none>");
            }
        }

        private static string ResolveReleaseFocusActivityId(SessionActivityRuntimeState state)
        {
            const string preferredActivityId = "activity_01";
            for (int i = state.Facts.Count - 1; i >= 0; i--)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (string.Equals(fact.Identity.ActivityId, preferredActivityId, StringComparison.Ordinal) &&
                    ShouldIncludeReleaseEvidenceFact(fact))
                {
                    return preferredActivityId;
                }
            }

            return state.CurrentDefinition.ActivityId;
        }

        private static int ResolvePreviousActivityEntrySequence(SessionActivityRuntimeState state, int currentEntrySequence)
        {
            for (int i = state.Facts.Count - 1; i >= 0; i--)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Kind == SessionActivityFactKind.ActivityDeactivated &&
                    fact.Identity.EntrySequence != currentEntrySequence)
                {
                    return fact.Identity.EntrySequence;
                }
            }

            return 0;
        }

        private static int ResolveLastCompletedActivityEntrySequence(SessionActivityRuntimeState state)
        {
            for (int i = state.Facts.Count - 1; i >= 0; i--)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Kind == SessionActivityFactKind.ActivityContentReleaseCompleted)
                {
                    return fact.Identity.EntrySequence;
                }
            }

            return 0;
        }

        private static void AddEntrySequence(HashSet<int> set, int value)
        {
            if (value > 0)
            {
                set.Add(value);
            }
        }

        private SessionActivityRouteExitTeardownResult BuildRouteExitTeardownInProgress(string reason, string detail)
        {
            return new SessionActivityRouteExitTeardownResult(
                SessionActivityRouteExitTeardownKind.InProgress,
                sessionStateId,
                _pipeline.State.CurrentStage,
                _pipeline.State.CurrentDefinition.ActivityId,
                _pipeline.State.CurrentHandoff.IsValid,
                reason,
                detail);
        }

        private SessionActivityRouteExitTeardownResult BuildRouteExitTeardownFailed(string reason, string detail)
        {
            return new SessionActivityRouteExitTeardownResult(
                SessionActivityRouteExitTeardownKind.Failed,
                sessionStateId,
                _pipeline.State.CurrentStage,
                _pipeline.State.CurrentDefinition.ActivityId,
                _pipeline.State.CurrentHandoff.IsValid,
                reason,
                detail);
        }

        private bool QaApplyActorAttributeCommand(
            string action,
            string actorId,
            string attributeId,
            ActorAttributeOperation operation,
            float amount,
            float setValue)
        {
            EnsurePipeline();

            bool applied = _pipeline.TryApplyActorAttributeCommand(
                State.CurrentIdentity,
                actorId,
                operation,
                attributeId,
                amount,
                setValue,
                QaSource(action),
                QaReason(action),
                out ActorAttributeApplyResult result);

            string outcome = applied ? "Applied" : (result.Rejected ? "Rejected" : "Failed");
            Debug.Log(
                $"[OBS][SessionActivityPipeline][Host] action='{action}' outcomeKind='{outcome}' operation='{operation}' actorId='{Normalize(actorId)}' attributeId='{Normalize(attributeId)}' amount='{amount:0.###}' setValue='{setValue:0.###}' reason='{result.Reason}' activityId='{State.CurrentDefinition.ActivityId}' entrySequence='{State.CurrentEntrySequence}'");

            if (applied && result.HasFact)
            {
                ActorAttributeChangedFact fact = result.Fact;
                Debug.Log(
                    $"[OBS][SessionActivityPipeline][Host][ActorAttributeFact] operation='{fact.Operation}' actorId='{Normalize(actorId)}' actorInstanceId='{fact.ActorInstanceId}' attributeId='{fact.AttributeId}' previousValue='{fact.PreviousValue:0.###}' newValue='{fact.NewValue:0.###}' clamped='{fact.Clamped}' activityIdentity='{fact.ActivityIdentity}' pipelineIdentity='{fact.PipelineIdentity}'");
            }

            return applied;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private ActivityTransitionContinuePolicy ResolveCurrentContinuePolicy()
        {
            SessionActivityDefinition current = State.CurrentDefinition;
            return current.IsValid
                ? current.NextActivityTransitionContinuePolicy
                : ActivityTransitionContinuePolicy.Unknown;
        }

        private static string FormatActivitySetupInventory(SessionActivityRuntimeState state)
        {
            if (state == null)
            {
                return "<none>";
            }

            ActivitySetupInventory inventory = state.CurrentActivitySetupInventory;
            if (!inventory.IsValid || string.IsNullOrWhiteSpace(inventory.InventoryId))
            {
                return "<none>";
            }

            return $"inventoryId='{inventory.InventoryId}', totalRequirements='{inventory.TotalRequirementCount}'";
        }

        private static void RegisterGlobal<T>(T instance) where T : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                if (!ReferenceEquals(existing, instance))
                {
                    throw new InvalidOperationException($"Global dependency '{typeof(T).Name}' is already registered with a different instance.");
                }

                return;
            }

            DependencyManager.Provider.RegisterGlobal(instance);
        }

        private void UnregisterGlobalsIfNeeded()
        {
            if (!_globalsRegistered)
            {
                return;
            }

            if (_catalog != null)
            {
                UnregisterGlobal(_catalog);
            }

            if (_pipeline != null)
            {
                UnregisterGlobal(_pipeline);
                UnregisterGlobal<ISessionActivityEntryHandoffReceiver>(_pipeline);
                UnregisterGlobal<ISessionActivitySnapshotPayloadProvider>(_pipeline);
            }

            UnregisterGlobal<ISessionActivityRouteExitTeardownBoundary>(this);
            UnregisterGlobal<ISessionActivityPredefinedVisualReadinessBoundary>(this);
            _globalsRegistered = false;
        }

        private static void UnregisterGlobal<T>(T instance) where T : class
        {
            if (instance == null)
            {
                return;
            }

            DependencyManager.Provider.UnregisterGlobal(instance);
        }
    }
}
