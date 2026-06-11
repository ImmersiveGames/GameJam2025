using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/SessionActivity/Session Activity Debug Panel")]
    public sealed class SessionActivityDebugPanel : MonoBehaviour
    {
        private const int PanelWidth = 1040;
        private const int PanelHeight = 980;
        private const int ButtonHeight = 52;
        private const int SectionSpacing = 8;
        private const int TextAreaHeight = 170;

        [Header("Refs")]
        [SerializeField] private SessionActivityHost host;

        [Header("Layout")]
        [SerializeField] private bool showOnGUI = true;
        [SerializeField] private bool showForeignStaleQa = false;
        [SerializeField] private bool qaAutoDumpOnObservedStateChange = false;

        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _dumpStyle;
        private int _observedStateRevision;
        private string _lastActivity01ToActivity02CheckpointToken = string.Empty;
        private int _activeCheckpointFromEntrySequence;
        private int _activeCheckpointToEntrySequence;
        private bool _activeCheckpointObservedActivity02Running;
        private int _frozenPassedCheckpointFromEntrySequence;
        private int _frozenPassedCheckpointToEntrySequence;
        private string _lastRestartCurrentActivityCheckpointToken = string.Empty;
        private string _frozenRestartCheckpointActivityId = string.Empty;
        private int _frozenRestartCheckpointFromEntrySequence;
        private int _frozenRestartCheckpointToEntrySequence;
        private string _activeRestartCheckpointActivityId = string.Empty;
        private int _activeRestartCheckpointFromEntrySequence;
        private int _activeRestartCheckpointToEntrySequence;
        private bool _activeRestartCheckpointReleaseSceneSafeLatched;
        private string _lastRouteExitBackToMenuCheckpointToken = string.Empty;
        private string _frozenRouteExitActivityId = string.Empty;
        private int _frozenRouteExitEntrySequence;
        private string _lastOnGuiErrorToken = string.Empty;
        private readonly Dictionary<string, string> _lastActivityObjectContributorDiscoveryCheckpointTokenByEntry = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _lastActivityObjectResetCheckpointTokenByEntry = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _lastActivityObjectSnapshotCaptureCheckpointTokenByEntry = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _lastActivityObjectSnapshotContractValidationCheckpointTokenByEntry = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _lastActivityObjectSnapshotRestoreCheckpointTokenByEntry = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _lastActivityObjectReleaseCheckpointTokenByEntry = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _lastActivityObjectContributorUnregisterCheckpointTokenByEntry = new(StringComparer.Ordinal);

        private void OnEnable()
        {
            TryBindHostStateObservation();
            SceneManager.sceneLoaded -= OnSceneLoadedForQaCheckpoint;
            SceneManager.sceneLoaded += OnSceneLoadedForQaCheckpoint;
            SceneManager.activeSceneChanged -= OnActiveSceneChangedForQaCheckpoint;
            SceneManager.activeSceneChanged += OnActiveSceneChangedForQaCheckpoint;
        }

        private void OnDisable()
        {
            if (host != null)
            {
                host.StateObservedChanged -= OnHostStateObservedChanged;
            }

            SceneManager.sceneLoaded -= OnSceneLoadedForQaCheckpoint;
            SceneManager.activeSceneChanged -= OnActiveSceneChangedForQaCheckpoint;
        }

        private void OnSceneLoadedForQaCheckpoint(Scene scene, LoadSceneMode mode)
        {
            TryEmitRouteExitBackToMenuCheckpointIfPossible();
        }

        private void OnActiveSceneChangedForQaCheckpoint(Scene previousScene, Scene nextScene)
        {
            TryEmitRouteExitBackToMenuCheckpointIfPossible();
        }

        private void TryEmitRouteExitBackToMenuCheckpointIfPossible()
        {
            if (host == null)
            {
                return;
            }

            TryEmitRouteExitBackToMenuCheckpoint();
        }

        [ContextMenu("CompleteCurrentActivity")]
        public void CompleteCurrentActivity()
        {
            EnsureHost();
            host.CompleteCurrentActivity();
        }

        [ContextMenu("RestartCurrentActivity")]
        public void RestartCurrentActivity()
        {
            EnsureHost();
            host.RestartCurrentActivity();
        }

        [ContextMenu("CompleteActivationWindow")]
        public void CompleteActivationWindow()
        {
            EnsureHost();
            host.CompleteActivationWindow();
        }

        [ContextMenu("CompleteDeactivationWindow")]
        public void CompleteDeactivationWindow()
        {
            EnsureHost();
            host.CompleteDeactivationWindow();
        }

        [ContextMenu("SendStaleFirstActivityCommand")]
        public void SendStaleFirstActivityCommand()
        {
            EnsureHost();
            host.ExecuteCommand(BuildStaleFirstActivityCommand(), "SendStaleFirstActivityCommand");
        }

        [ContextMenu("SendForeignSessionCommand")]
        public void SendForeignSessionCommand()
        {
            EnsureHost();
            host.ExecuteCommand(BuildForeignSessionCommand(), "SendForeignSessionCommand");
        }

        [ContextMenu("SendForeignPipelineCommand")]
        public void SendForeignPipelineCommand()
        {
            EnsureHost();
            host.ExecuteCommand(BuildForeignPipelineCommand(), "SendForeignPipelineCommand");
        }

        [ContextMenu("DumpState")]
        public void DumpState()
        {
            EnsureHost();
            host.DumpState();
        }

        [ContextMenu("Trace")]
        public void Trace()
        {
            EnsureHost();
            host.DumpTrace();
        }

        [ContextMenu("DumpActivityContentReleaseEvidence")]
        public void DumpActivityContentReleaseEvidence()
        {
            EnsureHost();
            host.DumpActivityContentReleaseEvidence();
        }

        [ContextMenu("DumpCurrentActivityEvidence")]
        public void DumpCurrentActivityEvidence()
        {
            EnsureHost();
            host.DumpCurrentActivityEvidence();
        }

        [ContextMenu("DumpParticipantBindingEvidence")]
        public void DumpParticipantBindingEvidence()
        {
            EnsureHost();
            host.DumpParticipantBindingEvidence();
        }

        [ContextMenu("DumpTransitionEvidence")]
        public void DumpTransitionEvidence()
        {
            EnsureHost();
            host.DumpTransitionEvidence();
        }

        [ContextMenu("DumpSceneState")]
        public void DumpSceneState()
        {
            EnsureHost();
            host.DumpSceneState();
        }

        [ContextMenu("Dump Current Evidence")]
        public void SmokeDumpCurrentEvidence()
        {
            EnsureHost();
            DumpCurrentActivityEvidence();
            DumpActivityContentReleaseEvidence();
        }

        [ContextMenu("Reset Current Player Actor")]
        public void ResetCurrentPlayerActor()
        {
            EnsureHost();
            host.QaResetCurrentPlayerActor();
        }

        [ContextMenu("Reset Current Activity Objects")]
        public void ResetCurrentActivityObjects()
        {
            EnsureHost();
            host.QaResetCurrentActivityObjects();
        }

        [ContextMenu("Capture Current Activity Snapshot Envelope")]
        public void CaptureCurrentActivitySnapshotEnvelope()
        {
            EnsureHost();
            host.QaCaptureCurrentActivitySnapshotPayload();
        }

        [ContextMenu("Save Captured Activity Snapshot Envelope")]
        public void SaveCapturedActivitySnapshotEnvelope()
        {
            EnsureHost();
            if (!TryResolveOperationalPipeline(out SessionOperationalPipeline operationalPipeline, out string failureReason))
            {
                Debug.LogWarning(
                    $"[OBS][SessionActivityPipeline][QA] event='RouteActivitySaveQaRejected' reason='{failureReason}' activityId='{host.State.CurrentDefinition.ActivityId}' entrySequence='{host.State.CurrentEntrySequence}' stage='{host.State.CurrentStage}'.");
                return;
            }

            bool saved = operationalPipeline.TryQaSaveCurrentActivitySnapshot(
                host.State.SessionId,
                host.State.CurrentDefinition.ActivityId,
                "SessionActivityHost/QA/SaveCapturedActivitySnapshotEnvelope",
                "SessionActivityHost/QA/SaveCapturedActivitySnapshotEnvelope",
                out string outcomeReason);

            string normalizedOutcomeReason = string.IsNullOrWhiteSpace(outcomeReason) ? string.Empty : outcomeReason.Trim();
            string outcomeKind = normalizedOutcomeReason.StartsWith("qa_save_skipped_", StringComparison.Ordinal)
                ? "Skipped"
                : saved
                    ? "Saved"
                    : "Failed";

            Debug.Log(
                $"[OBS][SessionActivityPipeline][QA] event='RouteActivitySaveQaSubmitted' outcomeKind='{outcomeKind}' reason='{outcomeReason}' saveOwnerActivityIdentity='{host.State.SessionId}' payloadActivityIdentity='{host.State.CurrentDefinition.ActivityId}' entrySequence='{host.State.CurrentEntrySequence}' stage='{host.State.CurrentStage}'.");
        }

        [ContextMenu("Capture And Save Current Activity Snapshot Envelope")]
        public void CaptureAndSaveCurrentActivitySnapshotEnvelope()
        {
            EnsureHost();
            bool captured = host.QaCaptureCurrentActivitySnapshotPayload();
            if (!captured && host.State.CurrentDefinition.HasGameplayContent)
            {
                Debug.LogWarning(
                    $"[OBS][SessionActivityPipeline][QA] event='RouteActivitySaveQaRejected' reason='snapshot_capture_not_completed' activityId='{host.State.CurrentDefinition.ActivityId}' entrySequence='{host.State.CurrentEntrySequence}' stage='{host.State.CurrentStage}'.");
                return;
            }

            if (!captured)
            {
                Debug.Log(
                    $"[OBS][SessionActivityPipeline][QA] event='RouteActivitySaveQaContinuedAfterNoContentCapture' reason='no_content_capture_should_be_classified_by_save_qa' activityId='{host.State.CurrentDefinition.ActivityId}' entrySequence='{host.State.CurrentEntrySequence}' stage='{host.State.CurrentStage}'.");
            }

            SaveCapturedActivitySnapshotEnvelope();
        }

        private void OnGUI()
        {
            if (!showOnGUI)
            {
                return;
            }

            bool areaBegun = false;
            try
            {
                if (!TryEnsureHost())
                {
                    EnsureStyles();
                    GUILayout.BeginArea(new Rect(20, 20, PanelWidth, PanelHeight), _windowStyle);
                    areaBegun = true;
                    GUILayout.Label("Session Activity", _titleStyle);
                    GUILayout.Space(SectionSpacing);
                    GUILayout.Label("SessionActivity unavailable / failed initialization", _labelStyle);
                    GUILayout.Label("Host/Pipeline ainda nao disponivel.", _labelStyle);
                    return;
                }

                EnsureStyles();

                if (!TryEnsureHostOperational())
                {
                    GUILayout.BeginArea(new Rect(20, 20, PanelWidth, PanelHeight), _windowStyle);
                    areaBegun = true;
                    GUILayout.Label("Session Activity", _titleStyle);
                    GUILayout.Space(SectionSpacing);
                    GUILayout.Label("SessionActivity host encontrado, pipeline indisponivel.", _labelStyle);
                    GUILayout.Label("Estado parcial detectado; aguardando composicao/boot valido.", _labelStyle);
                    return;
                }

                GUILayout.BeginArea(new Rect(20, 20, PanelWidth, PanelHeight), _windowStyle);
                areaBegun = true;
                GUILayout.Label("Session Activity", _titleStyle);
                GUILayout.Space(SectionSpacing);
                GUILayout.Label("QA canonicamente restrito: use apenas o lifecycle local sem atalhos de navegacao.", _labelStyle);
                GUILayout.Label("Restart canônico: use RestartCurrentActivity em ActivityRunning; nao usar GoTo*/DebugStartActivity como rail de restart.", _labelStyle);
                GUILayout.Space(SectionSpacing);

                GUILayout.Space(SectionSpacing);

                bool canCompleteActivationWindow = CanCompleteActivationWindow();
                bool canCompleteCurrentActivity = CanCompleteCurrentActivity();
                bool canRestartCurrentActivity = CanRestartCurrentActivity();
                bool canCompleteDeactivationWindow = CanCompleteDeactivationWindow();
                bool canResetCurrentPlayerActor = CanResetCurrentPlayerActor();
                bool canResetCurrentActivityObjects = CanResetCurrentActivityObjects();

                GUI.enabled = canCompleteActivationWindow;
                if (GUILayout.Button("CompleteActivationWindow", _buttonStyle))
                {
                    CompleteActivationWindow();
                }
                GUI.enabled = true;

                GUILayout.Space(SectionSpacing);

                GUI.enabled = canCompleteCurrentActivity;
                if (GUILayout.Button("CompleteCurrentActivity", _buttonStyle))
                {
                    CompleteCurrentActivity();
                }
                GUI.enabled = true;

                GUILayout.Space(SectionSpacing);

                GUI.enabled = canRestartCurrentActivity;
                if (GUILayout.Button("RestartCurrentActivity (rail canonico de restart local)", _buttonStyle))
                {
                    RestartCurrentActivity();
                }
                GUI.enabled = true;

                GUILayout.Space(SectionSpacing);

                GUI.enabled = canResetCurrentPlayerActor;
                if (GUILayout.Button("Reset Current Player Actor", _buttonStyle))
                {
                    ResetCurrentPlayerActor();
                }
                GUI.enabled = true;

                GUILayout.Space(SectionSpacing);

                GUI.enabled = canResetCurrentActivityObjects;
                if (GUILayout.Button("Reset Current Activity Objects", _buttonStyle))
                {
                    ResetCurrentActivityObjects();
                }
                GUI.enabled = true;

                GUILayout.Space(SectionSpacing);

                bool canCaptureSnapshotForQa = CanCaptureCurrentActivitySnapshotEnvelope(out string captureSnapshotQaGuardReason);
                bool canSaveSnapshotForQa = CanSaveCapturedActivitySnapshotEnvelope(out string saveSnapshotQaGuardReason);
                GUILayout.Label($"Snapshot QA: canCapture='{canCaptureSnapshotForQa}' captureGuard='{captureSnapshotQaGuardReason}' canSave='{canSaveSnapshotForQa}' saveGuard='{saveSnapshotQaGuardReason}' contentMode='{host.State.CurrentDefinition.ActivityContentMode}' hasGameplayContent='{host.State.CurrentDefinition.HasGameplayContent}' stage='{host.State.CurrentStage}'", _labelStyle);
                GUI.enabled = canCaptureSnapshotForQa;
                if (GUILayout.Button("Capture Snapshot Envelope (QA)", _buttonStyle))
                {
                    CaptureCurrentActivitySnapshotEnvelope();
                }
                GUI.enabled = true;

                GUILayout.Space(SectionSpacing);

                GUI.enabled = canCaptureSnapshotForQa;
                if (GUILayout.Button("Capture + Save Snapshot Envelope (QA)", _buttonStyle))
                {
                    CaptureAndSaveCurrentActivitySnapshotEnvelope();
                }
                GUI.enabled = true;

                GUILayout.Space(SectionSpacing);

                GUI.enabled = canSaveSnapshotForQa;
                if (GUILayout.Button("Save Captured Snapshot Envelope (QA)", _buttonStyle))
                {
                    SaveCapturedActivitySnapshotEnvelope();
                }
                GUI.enabled = true;

                GUILayout.Space(SectionSpacing);

                GUI.enabled = canCompleteDeactivationWindow;
                if (GUILayout.Button("CompleteDeactivationWindow", _buttonStyle))
                {
                    CompleteDeactivationWindow();
                }
                GUI.enabled = true;

                if (showForeignStaleQa)
                {
                    GUILayout.Space(SectionSpacing);
                    GUILayout.Label("Foreign/Stale QA", _labelStyle);

                    if (GUILayout.Button("SendStaleFirstActivityCommand", _buttonStyle))
                    {
                        SendStaleFirstActivityCommand();
                    }

                    GUILayout.Space(SectionSpacing);

                    if (GUILayout.Button("SendForeignSessionCommand", _buttonStyle))
                    {
                        SendForeignSessionCommand();
                    }

                    GUILayout.Space(SectionSpacing);

                    if (GUILayout.Button("SendForeignPipelineCommand", _buttonStyle))
                    {
                        SendForeignPipelineCommand();
                    }
                }

                GUILayout.Space(SectionSpacing);
                GUILayout.Label("Current State", _labelStyle);
                GUILayout.Label($"Observed State Revision: {_observedStateRevision}", _labelStyle);

                string stateSummary = BuildStateSummary();
                GUILayout.TextArea(stateSummary, _dumpStyle, GUILayout.Height(TextAreaHeight));
            }
            catch (Exception exception)
            {
                string token = exception.GetType().FullName + "|" + exception.Message;
                if (!string.Equals(token, _lastOnGuiErrorToken, StringComparison.Ordinal))
                {
                    _lastOnGuiErrorToken = token;
                    Debug.LogError($"[FATAL][SessionActivityDebugPanel] OnGUI render failed. error='{exception}'.");
                }
            }
            finally
            {
                GUI.enabled = true;
                if (areaBegun)
                {
                    GUILayout.EndArea();
                }
            }
        }

        private void EnsureHost()
        {
            if (host != null)
            {
                return;
            }

            host = FindFirstObjectByType<SessionActivityHost>(FindObjectsInactive.Include);
            if (host == null)
            {
                throw new InvalidOperationException("SessionActivityDebugPanel requires SessionActivityHost in the same scene.");
            }

            TryBindHostStateObservation();
        }

        private bool TryEnsureHost()
        {
            if (host != null)
            {
                return true;
            }

            host = FindFirstObjectByType<SessionActivityHost>(FindObjectsInactive.Include);
            if (host == null)
            {
                return false;
            }

            TryBindHostStateObservation();
            return host != null;
        }

        private bool TryEnsureHostOperational()
        {
            return host != null &&
                host.Pipeline != null &&
                host.State != null;
        }

        private string BuildDumpText()
        {
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][QA] DumpState");
            builder.AppendLine($"host='{host.name}'");
            builder.AppendLine($"pipelineId='{host.State.PipelineId}' sessionStateId='{host.State.SessionId}'");
            builder.AppendLine($"entrySequence='{host.State.CurrentEntrySequence}'");
            builder.AppendLine($"executionState='{host.State.CurrentExecutionState}'");
            builder.AppendLine($"gateState='{host.GateState}'");
            builder.AppendLine($"started='{host.State.HasStarted}' completed='{host.State.HasCompleted}' stage='{host.State.CurrentStage}' currentActivity='{host.State.CurrentDefinition.ActivityId}'");
            builder.AppendLine($"definition='{host.State.CurrentDefinition}'");
            builder.AppendLine($"identity='{host.State.CurrentIdentity}'");
            builder.AppendLine($"handoff='{host.State.CurrentHandoff}'");
            builder.AppendLine($"pendingOperation='{host.State.CurrentPendingOperation}'");
            builder.AppendLine($"activityContentLoadedSet='{host.Pipeline.GetCurrentActivityContentLoadedSet()}'");
            builder.AppendLine($"activitySetupInventory='{FormatActivitySetupInventory()}'");
            builder.AppendLine($"pendingHandoffTarget='{GetPendingHandoffTarget()}'");
            builder.AppendLine($"nextExpectedQaAction='{GetNextExpectedQaAction()}'");
            builder.AppendLine("qaLifecycleRail='ActivityRunning -> CompleteCurrentActivity/RestartCurrentActivity; CompleteActivationWindow/CompleteDeactivationWindow apenas quando window stage=Ready; ContinueToNextActivity apenas se policy=ManualContinue'");
            builder.AppendLine("checkpointEvidenceFacts(currentActivity):");
            string currentActivityId = host.State.CurrentDefinition.ActivityId;
            int currentEntrySequence = host.State.CurrentEntrySequence;
            for (int index = 0; index < host.State.Facts.Count; index++)
            {
                SessionActivityFact fact = host.State.Facts[index];
                if (ShouldIncludeCheckpointEvidenceFact(fact, currentActivityId, currentEntrySequence))
                {
                    builder.AppendLine($"- {fact}");
                }
            }
            builder.AppendLine("facts:");

            for (int index = 0; index < host.State.Facts.Count; index++)
            {
                builder.AppendLine($"- {host.State.Facts[index]}");
            }

            builder.AppendLine("snapshots:");
            for (int index = 0; index < host.State.Snapshots.Count; index++)
            {
                builder.AppendLine($"- {host.State.Snapshots[index]}");
            }

            builder.AppendLine("trace:");
            for (int index = 0; index < host.State.Trace.Count; index++)
            {
                builder.AppendLine($"- {host.State.Trace[index]}");
            }

            return builder.ToString().TrimEnd();
        }

        private static bool ShouldIncludeCheckpointEvidenceFact(SessionActivityFact fact, string currentActivityId, int currentEntrySequence)
        {
            if (!fact.IsValid || !fact.Identity.IsValid)
            {
                return false;
            }

            if (!string.Equals(fact.Identity.ActivityId, currentActivityId, StringComparison.Ordinal) ||
                fact.Identity.EntrySequence != currentEntrySequence)
            {
                return false;
            }

            return fact.Kind == SessionActivityFactKind.ActivityContentLoadSkippedNoContent ||
                   fact.Kind == SessionActivityFactKind.ActivitySetupStarted ||
                   fact.Kind == SessionActivityFactKind.ActivitySetupInventoryBuildStarted ||
                   fact.Kind == SessionActivityFactKind.ActivitySetupInventoryBuilt ||
                   fact.Kind == SessionActivityFactKind.ActivitySetupInventorySkippedNoRequirements ||
                   fact.Kind == SessionActivityFactKind.ActivitySetupInventoryValidated ||
                   fact.Kind == SessionActivityFactKind.ActivityParticipantBindingStarted ||
                   fact.Kind == SessionActivityFactKind.ActivityParticipantBindingSkippedNoRequirements ||
                   fact.Kind == SessionActivityFactKind.ActivityParticipantBindingCompleted ||
                   fact.Kind == SessionActivityFactKind.ActivitySetupCompleted ||
                   fact.Kind == SessionActivityFactKind.ActivityActivationStarted ||
                   fact.Kind == SessionActivityFactKind.ActivityRunningEntered ||
                   fact.Kind == SessionActivityFactKind.GameplayContentSkippedNoContent;
        }

        private string BuildStateSummary()
        {
            StringBuilder builder = new();
            builder.AppendLine($"pipelineId='{host.State.PipelineId}'");
            builder.AppendLine($"sessionStateId='{host.State.SessionId}'");
            builder.AppendLine($"entrySequence='{host.State.CurrentEntrySequence}'");
            builder.AppendLine($"executionState='{host.State.CurrentExecutionState}'");
            builder.AppendLine($"gateState='{host.GateState}'");
            builder.AppendLine($"started='{host.State.HasStarted}' completed='{host.State.HasCompleted}' stage='{host.State.CurrentStage}'");
            builder.AppendLine($"currentActivity='{host.State.CurrentDefinition.ActivityId}'");
            builder.AppendLine($"pendingHandoffTarget='{GetPendingHandoffTarget()}'");
            builder.AppendLine($"nextExpectedQaAction='{GetNextExpectedQaAction()}'");
            builder.AppendLine($"identity='{host.State.CurrentIdentity}'");
            builder.AppendLine($"handoff='{host.State.CurrentHandoff}'");
            builder.AppendLine($"pendingOperation='{host.State.CurrentPendingOperation}'");
            builder.AppendLine($"activityContentLoadedSet='{host.Pipeline.GetCurrentActivityContentLoadedSet()}'");
            builder.AppendLine($"activitySetupInventory='{FormatActivitySetupInventory()}'");
            if (host.GateState != null)
            {
                builder.AppendLine($"gateSessionBlocked='{host.GateState.SessionBlocked}' gateActivityBlocked='{host.GateState.ActivityBlocked}'");
                builder.AppendLine($"gateLastFact='{host.GateState.LastFact}'");
                builder.AppendLine($"gateLastSnapshot='{host.GateState.LastSnapshot}'");
            }
            return builder.ToString().TrimEnd();
        }

        private bool TryResolveOperationalPipeline(out SessionOperationalPipeline pipeline, out string failureReason)
        {
            pipeline = null;
            failureReason = string.Empty;

            if (DependencyManager.Provider == null)
            {
                failureReason = "dependency_provider_missing";
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out pipeline) || pipeline == null)
            {
                failureReason = "session_operational_pipeline_missing";
                return false;
            }

            failureReason = "resolved";
            return true;
        }

        private bool CanCaptureCurrentActivitySnapshotEnvelope()
        {
            return CanCaptureCurrentActivitySnapshotEnvelope(out _);
        }

        private bool CanCaptureCurrentActivitySnapshotEnvelope(out string guardReason)
        {
            return CanAttemptCurrentActivitySnapshotQa(out guardReason);
        }

        private bool CanSaveCapturedActivitySnapshotEnvelope()
        {
            return CanSaveCapturedActivitySnapshotEnvelope(out _);
        }

        private bool CanSaveCapturedActivitySnapshotEnvelope(out string guardReason)
        {
            return CanAttemptCurrentActivitySnapshotQa(out guardReason);
        }

        private bool CanAttemptCurrentActivitySnapshotQa(out string guardReason)
        {
            guardReason = "unknown";

            if (!IsHostStateAvailable())
            {
                guardReason = "host_state_unavailable";
                return false;
            }

            if (host.State.CurrentPendingOperation.IsValid)
            {
                guardReason = "pending_operation_active";
                return false;
            }

            if (!host.State.CurrentIdentity.IsValid)
            {
                guardReason = "current_identity_invalid";
                return false;
            }

            if (host.State.CurrentStage != SessionActivityStage.ActivityRunning)
            {
                guardReason = "activity_not_running";
                return false;
            }

            guardReason = host.State.CurrentDefinition.HasGameplayContent
                ? "ready_with_content"
                : "ready_no_content_expected_skip";
            return true;
        }

        private bool CanResetCurrentActivityObjects()
        {
            if (!IsHostStateAvailable())
            {
                return false;
            }

            return !host.State.CurrentPendingOperation.IsValid &&
                   host.State.CurrentIdentity.IsValid &&
                   host.State.CurrentStage == SessionActivityStage.ActivityRunning;
        }

        private bool CanCompleteActivationWindow()
        {
            if (!IsHostStateAvailable())
            {
                return false;
            }

            return !host.State.CurrentPendingOperation.IsValid &&
                   host.State.CurrentStage == SessionActivityStage.ActivationWindowReady;
        }

        private bool CanCompleteCurrentActivity()
        {
            if (!IsHostStateAvailable())
            {
                return false;
            }

            return !host.State.CurrentPendingOperation.IsValid &&
                   host.State.CurrentStage == SessionActivityStage.ActivityRunning;
        }

        private bool CanRestartCurrentActivity()
        {
            if (!IsHostStateAvailable())
            {
                return false;
            }

            return !host.State.CurrentPendingOperation.IsValid &&
                   host.State.CurrentStage == SessionActivityStage.ActivityRunning;
        }

        private bool CanCompleteDeactivationWindow()
        {
            if (!IsHostStateAvailable())
            {
                return false;
            }

            return !host.State.CurrentPendingOperation.IsValid &&
                   host.State.CurrentStage == SessionActivityStage.DeactivationWindowReady;
        }

        private bool CanContinueToNextActivity()
        {
            if (!IsHostStateAvailable())
            {
                return false;
            }

            return !host.State.CurrentPendingOperation.IsValid &&
                   host.State.CurrentHandoff.IsValid &&
                   ResolveCurrentContinuePolicy() == ActivityTransitionContinuePolicy.ManualContinue &&
                   host.State.CurrentStage == SessionActivityStage.NextActivitySetupCompleted;
        }

        private bool IsHostStateAvailable()
        {
            return host != null && host.State != null;
        }

        private string GetPendingHandoffTarget()
        {
            return host.State.CurrentHandoff.IsValid
                ? host.State.CurrentHandoff.NextActivityId
                : "<none>";
        }

        private string GetNextExpectedQaAction()
        {
            SessionActivityStage stage = host.State.CurrentStage;
            bool hasPendingHandoff = host.State.CurrentHandoff.IsValid;

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

        private SessionActivityCommand BuildStaleFirstActivityCommand()
        {
            EnsureActiveIdentityOrFail("SendStaleFirstActivityCommand");
            if (!host.Catalog.TryGetFirst(out SessionActivityDefinition firstDefinition) || !firstDefinition.IsValid)
            {
                throw new InvalidOperationException("Stale command requires a valid first activity in runtime catalog.");
            }

            SessionActivityIdentity identity = new(
                host.State.PipelineId,
                host.State.SessionId,
                firstDefinition.ActivityId,
                firstDefinition.ActivityOrdinal,
                host.State.CurrentEntrySequence > 1 ? host.State.CurrentEntrySequence - 1 : 1,
                SessionActivityStage.ActivityRunning,
                "SessionActivityDebugPanel");

            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteCurrentActivity,
                identity,
                "SessionActivityDebugPanel",
                "Send stale first activity command.");
        }

        private SessionActivityCommand BuildForeignSessionCommand()
        {
            EnsureActiveIdentityOrFail("SendForeignSessionCommand");
            SessionActivityIdentity current = host.State.CurrentIdentity;
            SessionActivityIdentity identity = new(
                host.State.PipelineId,
                "ForeignSessionState",
                current.ActivityId,
                current.ActivityOrdinal,
                current.EntrySequence,
                current.Stage,
                "SessionActivityDebugPanel");

            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteCurrentActivity,
                identity,
                "SessionActivityDebugPanel",
                "Send foreign session command.");
        }

        private SessionActivityCommand BuildForeignPipelineCommand()
        {
            EnsureActiveIdentityOrFail("SendForeignPipelineCommand");
            SessionActivityIdentity current = host.State.CurrentIdentity;
            SessionActivityIdentity identity = new(
                "ForeignPipeline",
                host.State.SessionId,
                current.ActivityId,
                current.ActivityOrdinal,
                current.EntrySequence,
                current.Stage,
                "SessionActivityDebugPanel");

            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteCurrentActivity,
                identity,
                "SessionActivityDebugPanel",
                "Send foreign pipeline command.");
        }

        private void EnsureActiveIdentityOrFail(string action)
        {
            if (host.State == null || !host.State.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException($"Action '{action}' requires an active pipeline identity.");
            }
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null)
            {
                return;
            }

            GUIStyle baseWindow = new(GUI.skin.window)
            {
                padding = new RectOffset(18, 18, 18, 18),
                fontSize = 16,
                richText = false,
            };

            GUIStyle baseLabel = new(GUI.skin.label)
            {
                fontSize = 20,
                wordWrap = true,
                richText = false,
            };

            GUIStyle baseButton = new(GUI.skin.button)
            {
                fontSize = 22,
                fixedHeight = ButtonHeight,
                alignment = TextAnchor.MiddleCenter,
            };

            GUIStyle baseTextArea = new(GUI.skin.textArea)
            {
                fontSize = 18,
                wordWrap = true,
                richText = false,
                padding = new RectOffset(10, 10, 10, 10),
                alignment = TextAnchor.UpperLeft,
            };

            _windowStyle = baseWindow;
            _titleStyle = new GUIStyle(baseLabel)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            _labelStyle = baseLabel;
            _buttonStyle = baseButton;
            _dumpStyle = baseTextArea;
        }

        private void TryBindHostStateObservation()
        {
            if (host == null)
            {
                return;
            }

            host.StateObservedChanged -= OnHostStateObservedChanged;
            host.StateObservedChanged += OnHostStateObservedChanged;
        }

        private void OnHostStateObservedChanged()
        {
            _observedStateRevision++;
            bool routeExitCheckpointActive = TryEmitRouteExitBackToMenuCheckpoint();
            if (!routeExitCheckpointActive)
            {
                bool restartCheckpointActive = TryEmitRestartCurrentActivityCheckpoint();
                if (!restartCheckpointActive)
                {
                    TryEmitActivity01ToActivity02Checkpoint();
                }
            }
            TryEmitActivityObjectContributorDiscoveryCheckpoint();
            TryEmitActivityObjectSnapshotContractValidationCheckpoint();
            TryEmitActivityObjectResetCheckpoint();
            TryEmitActivityObjectSnapshotCaptureCheckpoint();
            TryEmitActivityObjectSnapshotRestoreCheckpoint();
            TryEmitActivityObjectReleaseCheckpoint();
            TryEmitActivityObjectContributorUnregisterCheckpoint();
            if (qaAutoDumpOnObservedStateChange)
            {
                host.DumpCurrentActivityEvidence();
            }
        }

        private bool TryEmitRouteExitBackToMenuCheckpoint()
        {
            SessionActivityRuntimeState state = host.State;
            if (!TryResolveLatestRouteExitContext(out string activityId, out int entrySequence))
            {
                return false;
            }

            if (string.Equals(activityId, _frozenRouteExitActivityId, StringComparison.Ordinal) &&
                entrySequence == _frozenRouteExitEntrySequence)
            {
                return false;
            }

            bool releaseStarted = false;
            bool releaseCompleted = false;
            string releaseSceneName = "<none>";
            string releaseStatus = "<none>";
            bool closedForRouteExit = false;

            for (int i = 0; i < state.Facts.Count; i++)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (!string.Equals(fact.Identity.ActivityId, activityId, StringComparison.Ordinal) ||
                    fact.Identity.EntrySequence != entrySequence)
                {
                    continue;
                }

                if (fact.Kind == SessionActivityFactKind.ActivityContentReleaseStarted)
                {
                    releaseStarted = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityContentReleaseCompleted)
                {
                    releaseCompleted = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityContentSceneUnloaded)
                {
                    releaseSceneName = ExtractToken(fact.Message, "sceneName");
                    releaseStatus = ExtractToken(fact.Message, "releaseStatus");
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityRouteExitCompleted)
                {
                    closedForRouteExit = true;
                }
            }

            if (!closedForRouteExit &&
                string.Equals(state.CurrentDefinition.ActivityId, activityId, StringComparison.Ordinal) &&
                state.CurrentEntrySequence == entrySequence &&
                state.CurrentStage == SessionActivityStage.ClosedForRouteExit)
            {
                closedForRouteExit = true;
            }

            bool releaseSceneIsLoadedAfterRelease = false;
            if (releaseCompleted && !closedForRouteExit)
            {
                releaseSceneIsLoadedAfterRelease = ResolveSceneLoaded("ActivityScene01");
            }

            bool routeExitTeardownCompleted = closedForRouteExit;
            bool menuRouteApplied = routeExitTeardownCompleted && ResolveSceneLoadedOrActive("MenuScene");
            bool releaseCompletedWithValidStatus =
                releaseCompleted &&
                (string.Equals(releaseStatus, "<none>", StringComparison.Ordinal) ||
                 string.Equals(releaseStatus, "Unloaded", StringComparison.Ordinal));

            string checkpointStatus = "Waiting";
            string failedCriterion = "<none>";

            if (releaseCompleted &&
                !string.Equals(releaseStatus, "<none>", StringComparison.Ordinal) &&
                !string.Equals(releaseStatus, "Unloaded", StringComparison.Ordinal))
            {
                checkpointStatus = "Failed";
                failedCriterion = "releaseStatus";
            }
            else if (closedForRouteExit && !releaseCompleted)
            {
                checkpointStatus = "Failed";
                failedCriterion = "closedForRouteExitBeforeReleaseCompleted";
            }
            else if (releaseCompleted && !closedForRouteExit && releaseSceneIsLoadedAfterRelease)
            {
                checkpointStatus = "Failed";
                failedCriterion = "releaseSceneIsLoadedAfterRelease";
            }
            else if (releaseCompletedWithValidStatus &&
                     closedForRouteExit &&
                     routeExitTeardownCompleted &&
                     menuRouteApplied)
            {
                checkpointStatus = "Passed";
            }

            string token =
                $"{activityId}|{entrySequence}|{checkpointStatus}|{failedCriterion}|{releaseStarted}|{releaseCompleted}|{releaseSceneName}|{releaseStatus}|{releaseSceneIsLoadedAfterRelease}|{closedForRouteExit}|{routeExitTeardownCompleted}|{menuRouteApplied}";
            if (!string.Equals(token, _lastRouteExitBackToMenuCheckpointToken, StringComparison.Ordinal))
            {
                _lastRouteExitBackToMenuCheckpointToken = token;
                Debug.Log(
                    $"[OBS][SessionActivityPipeline][QACheckpoint] checkpoint='RouteExitBackToMenu' checkpointStatus='{checkpointStatus}' failedCriterion='{failedCriterion}' " +
                    $"activityId='{activityId}' entrySequence='{entrySequence}' " +
                    $"releaseStarted='{releaseStarted.ToString().ToLowerInvariant()}' releaseCompleted='{releaseCompleted.ToString().ToLowerInvariant()}' " +
                    $"releaseSceneName='{releaseSceneName}' releaseStatus='{releaseStatus}' releaseSceneIsLoadedAfterRelease='{releaseSceneIsLoadedAfterRelease.ToString().ToLowerInvariant()}' " +
                    $"closedForRouteExit='{closedForRouteExit.ToString().ToLowerInvariant()}' routeExitTeardownCompleted='{routeExitTeardownCompleted.ToString().ToLowerInvariant()}' " +
                    $"menuRouteApplied='{menuRouteApplied.ToString().ToLowerInvariant()}'");
            }

            if (string.Equals(checkpointStatus, "Passed", StringComparison.Ordinal))
            {
                _frozenRouteExitActivityId = activityId;
                _frozenRouteExitEntrySequence = entrySequence;
            }

            return !string.Equals(checkpointStatus, "Passed", StringComparison.Ordinal);
        }

        private bool TryEmitRestartCurrentActivityCheckpoint()
        {
            SessionActivityRuntimeState state = host.State;
            if (!TryResolveLatestRestartAccepted(out string fromActivity, out int fromEntrySequence, out int toEntrySequence))
            {
                return false;
            }

            if (string.Equals(fromActivity, _frozenRestartCheckpointActivityId, StringComparison.Ordinal) &&
                fromEntrySequence == _frozenRestartCheckpointFromEntrySequence &&
                toEntrySequence == _frozenRestartCheckpointToEntrySequence)
            {
                return false;
            }

            bool isNewActiveRestartContext =
                !string.Equals(fromActivity, _activeRestartCheckpointActivityId, StringComparison.Ordinal) ||
                fromEntrySequence != _activeRestartCheckpointFromEntrySequence ||
                toEntrySequence != _activeRestartCheckpointToEntrySequence;
            if (isNewActiveRestartContext)
            {
                _activeRestartCheckpointActivityId = fromActivity;
                _activeRestartCheckpointFromEntrySequence = fromEntrySequence;
                _activeRestartCheckpointToEntrySequence = toEntrySequence;
                _activeRestartCheckpointReleaseSceneSafeLatched = false;
            }

            const string checkpointName = "RestartCurrentActivity";
            string toActivity = fromActivity;
            bool releaseStarted = false;
            bool releaseCompleted = false;
            bool releaseSkippedNoContent = false;
            string releaseSceneName = "<none>";
            string releaseStatus = "<none>";
            bool newEntryStarted = false;
            bool newEntryReachedActivationWindowReady = false;
            bool newEntryReachedActivityRunning = false;

            for (int i = 0; i < state.Facts.Count; i++)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (string.Equals(fact.Identity.ActivityId, fromActivity, StringComparison.Ordinal) &&
                    fact.Identity.EntrySequence == fromEntrySequence)
                {
                    if (fact.Kind == SessionActivityFactKind.ActivityContentReleaseStarted)
                    {
                        releaseStarted = true;
                    }
                    else if (fact.Kind == SessionActivityFactKind.ActivityContentReleaseCompleted)
                    {
                        releaseCompleted = true;
                    }
                    else if (fact.Kind == SessionActivityFactKind.ActivityContentReleaseSkippedNoContent)
                    {
                        releaseSkippedNoContent = true;
                    }
                    else if (fact.Kind == SessionActivityFactKind.ActivityContentSceneUnloaded)
                    {
                        releaseSceneName = ExtractToken(fact.Message, "sceneName");
                        releaseStatus = ExtractToken(fact.Message, "releaseStatus");
                    }
                }

                if (!string.Equals(fact.Identity.ActivityId, toActivity, StringComparison.Ordinal) ||
                    fact.Identity.EntrySequence != toEntrySequence)
                {
                    continue;
                }

                if (fact.Kind == SessionActivityFactKind.ActivityActivationStarted ||
                    fact.Kind == SessionActivityFactKind.ActivityContentLoadStarted ||
                    fact.Kind == SessionActivityFactKind.ActivityContentSceneLoadCommandIssued ||
                    fact.Kind == SessionActivityFactKind.ActivityContentLoadedSetReady ||
                    fact.Kind == SessionActivityFactKind.ActivitySetupStarted ||
                    fact.Kind == SessionActivityFactKind.ActivationWindowStarted ||
                    fact.Kind == SessionActivityFactKind.ActivationWindowReady ||
                    fact.Kind == SessionActivityFactKind.ActivityRunningEntered)
                {
                    newEntryStarted = true;
                }

                if (fact.Kind == SessionActivityFactKind.ActivationWindowReady)
                {
                    newEntryReachedActivationWindowReady = true;
                }

                if (fact.Kind == SessionActivityFactKind.ActivityRunningEntered)
                {
                    newEntryReachedActivityRunning = true;
                }
            }

            if (string.Equals(state.CurrentDefinition.ActivityId, fromActivity, StringComparison.Ordinal) &&
                state.CurrentEntrySequence == toEntrySequence &&
                state.CurrentStage == SessionActivityStage.ActivityRunning)
            {
                newEntryStarted = true;
                newEntryReachedActivityRunning = true;
            }

            bool releaseSceneIsLoadedAfterRelease = false;
            bool shouldEvaluateReleaseScene = releaseCompleted &&
                                              !newEntryStarted &&
                                              !_activeRestartCheckpointReleaseSceneSafeLatched &&
                                              state.CurrentEntrySequence <= toEntrySequence;
            if (shouldEvaluateReleaseScene)
            {
                releaseSceneIsLoadedAfterRelease = ResolveSceneLoaded("ActivityScene01");
                if (!releaseSceneIsLoadedAfterRelease)
                {
                    _activeRestartCheckpointReleaseSceneSafeLatched = true;
                }
            }
            else if (_activeRestartCheckpointReleaseSceneSafeLatched)
            {
                // Uma vez comprovado "false" antes da nova entry, congelamos para este checkpoint.
                releaseSceneIsLoadedAfterRelease = false;
            }

            bool hasReleasePath = releaseStarted ||
                                  releaseCompleted ||
                                  !string.Equals(releaseSceneName, "<none>", StringComparison.Ordinal) ||
                                  !string.Equals(releaseStatus, "<none>", StringComparison.Ordinal);
            bool releasePass = releaseCompleted &&
                               string.Equals(releaseSceneName, "ActivityScene01", StringComparison.Ordinal) &&
                               string.Equals(releaseStatus, "Unloaded", StringComparison.Ordinal) &&
                               !releaseSceneIsLoadedAfterRelease;
            bool entryPass = string.Equals(toActivity, fromActivity, StringComparison.Ordinal) &&
                             toEntrySequence == fromEntrySequence + 1;
            bool readyOrRunning = newEntryReachedActivationWindowReady || newEntryReachedActivityRunning;
            bool noContentPass = (releaseSkippedNoContent || (!releaseStarted && !releaseCompleted)) &&
                                 newEntryStarted &&
                                 newEntryReachedActivityRunning;

            bool checkpointPassed = entryPass && (
                (hasReleasePath && releasePass && newEntryStarted && readyOrRunning) ||
                noContentPass);

            string checkpointStatus = checkpointPassed ? "Passed" : "Waiting";
            string failedCriterion = "<none>";
            if (!entryPass && (newEntryStarted || newEntryReachedActivationWindowReady || newEntryReachedActivityRunning))
            {
                checkpointStatus = "Failed";
                failedCriterion = "restartEntrySequenceMismatch";
            }
            else if (releaseCompleted &&
                !string.Equals(releaseStatus, "<none>", StringComparison.Ordinal) &&
                !string.Equals(releaseStatus, "Unloaded", StringComparison.Ordinal))
            {
                checkpointStatus = "Failed";
                failedCriterion = "releaseStatus";
            }
            else if (releaseCompleted && !newEntryStarted && releaseSceneIsLoadedAfterRelease)
            {
                checkpointStatus = "Failed";
                failedCriterion = "releaseSceneIsLoadedAfterRelease";
            }

            if (string.Equals(checkpointStatus, "Passed", StringComparison.Ordinal))
            {
                _frozenRestartCheckpointActivityId = fromActivity;
                _frozenRestartCheckpointFromEntrySequence = fromEntrySequence;
                _frozenRestartCheckpointToEntrySequence = toEntrySequence;
            }

            string token =
                $"{fromActivity}|{fromEntrySequence}|{toActivity}|{toEntrySequence}|{checkpointStatus}|{failedCriterion}|{releaseStarted}|{releaseCompleted}|{releaseSceneName}|{releaseStatus}|{releaseSceneIsLoadedAfterRelease}|{newEntryStarted}|{newEntryReachedActivationWindowReady}|{newEntryReachedActivityRunning}";
            if (!string.Equals(token, _lastRestartCurrentActivityCheckpointToken, StringComparison.Ordinal))
            {
                _lastRestartCurrentActivityCheckpointToken = token;
                Debug.Log(
                    $"[OBS][SessionActivityPipeline][QACheckpoint] checkpoint='{checkpointName}' checkpointStatus='{checkpointStatus}' failedCriterion='{failedCriterion}' " +
                    $"fromActivity='{fromActivity}' toActivity='{toActivity}' fromEntrySequence='{fromEntrySequence}' toEntrySequence='{toEntrySequence}' " +
                    $"releaseStarted='{releaseStarted.ToString().ToLowerInvariant()}' releaseCompleted='{releaseCompleted.ToString().ToLowerInvariant()}' " +
                    $"releaseSceneName='{releaseSceneName}' releaseStatus='{releaseStatus}' releaseSceneIsLoadedAfterRelease='{releaseSceneIsLoadedAfterRelease.ToString().ToLowerInvariant()}' " +
                    $"newEntryStarted='{newEntryStarted.ToString().ToLowerInvariant()}' newEntryReachedActivationWindowReady='{newEntryReachedActivationWindowReady.ToString().ToLowerInvariant()}' " +
                    $"newEntryReachedActivityRunning='{newEntryReachedActivityRunning.ToString().ToLowerInvariant()}'");
            }

            return !string.Equals(checkpointStatus, "Passed", StringComparison.Ordinal);
        }

        private void TryEmitActivityObjectContributorDiscoveryCheckpoint()
        {
            SessionActivityRuntimeState state = host.State;
            if (state == null || state.Facts == null || state.Facts.Count == 0)
            {
                return;
            }

            Dictionary<string, ActivityObjectContributorDiscoveryCheckpointAggregation> byEntry = new(StringComparer.Ordinal);
            for (int i = 0; i < state.Facts.Count; i++)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Kind != SessionActivityFactKind.ActivityObjectContributorDiscoveryStarted &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectContributorDiscovered &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectContributorDiscoverySkippedNoContent &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectContributorDiscoveryCompleted &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectContributorDiscoveryFailed)
                {
                    continue;
                }

                string key = $"{fact.Identity.ActivityId}|{fact.Identity.EntrySequence}";
                if (!byEntry.TryGetValue(key, out ActivityObjectContributorDiscoveryCheckpointAggregation aggregation))
                {
                    aggregation = new ActivityObjectContributorDiscoveryCheckpointAggregation(
                        fact.Identity.ActivityId,
                        fact.Identity.EntrySequence);
                }

                if (fact.Kind == SessionActivityFactKind.ActivityObjectContributorDiscoveryStarted)
                {
                    aggregation.DiscoveryStarted = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectContributorDiscovered)
                {
                    aggregation.DiscoveredCount += 1;

                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                    }

                    string roleId = ExtractToken(fact.Message, "roleId");
                    if (!string.Equals(roleId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.RoleIds.Add(roleId);
                    }

                    string contributorKind = ExtractToken(fact.Message, "contributorKind");
                    if (!string.Equals(contributorKind, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.ContributorKinds.Add(contributorKind);
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectContributorDiscoverySkippedNoContent)
                {
                    aggregation.SkippedNoContent = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectContributorDiscoveryCompleted)
                {
                    aggregation.DiscoveryCompleted = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectContributorDiscoveryFailed)
                {
                    aggregation.DiscoveryFailed = true;
                }

                byEntry[key] = aggregation;
            }

            foreach (KeyValuePair<string, ActivityObjectContributorDiscoveryCheckpointAggregation> pair in byEntry)
            {
                ActivityObjectContributorDiscoveryCheckpointAggregation aggregation = pair.Value;
                string checkpointStatus = ResolveActivityObjectContributorDiscoveryCheckpointStatus(aggregation);
                string targetIds = JoinValues(aggregation.TargetIds);
                string roleIds = JoinValues(aggregation.RoleIds);
                string contributorKinds = JoinValues(aggregation.ContributorKinds);

                string token =
                    $"{aggregation.ActivityId}|{aggregation.EntrySequence}|{aggregation.DiscoveryStarted}|{aggregation.DiscoveredCount}|{targetIds}|{roleIds}|{contributorKinds}|{aggregation.DiscoveryCompleted}|{aggregation.DiscoveryFailed}|{aggregation.SkippedNoContent}|{checkpointStatus}";

                if (_lastActivityObjectContributorDiscoveryCheckpointTokenByEntry.TryGetValue(pair.Key, out string lastToken) &&
                    string.Equals(lastToken, token, StringComparison.Ordinal))
                {
                    continue;
                }

                _lastActivityObjectContributorDiscoveryCheckpointTokenByEntry[pair.Key] = token;
                Debug.Log(
                    $"[OBS][SessionActivityPipeline][QACheckpoint] checkpoint='ActivityObjectContributorDiscovery' checkpointStatus='{checkpointStatus}' " +
                    $"activityId='{aggregation.ActivityId}' entrySequence='{aggregation.EntrySequence}' discoveryStarted='{aggregation.DiscoveryStarted.ToString().ToLowerInvariant()}' " +
                    $"discoveredCount='{aggregation.DiscoveredCount}' targetIds='{targetIds}' roleIds='{roleIds}' contributorKinds='{contributorKinds}' " +
                    $"discoveryCompleted='{aggregation.DiscoveryCompleted.ToString().ToLowerInvariant()}' discoveryFailed='{aggregation.DiscoveryFailed.ToString().ToLowerInvariant()}' " +
                    $"skippedNoContent='{aggregation.SkippedNoContent.ToString().ToLowerInvariant()}'");
            }
        }

        private static string ResolveActivityObjectContributorDiscoveryCheckpointStatus(ActivityObjectContributorDiscoveryCheckpointAggregation aggregation)
        {
            if (aggregation.DiscoveryFailed)
            {
                return "Failed";
            }

            if (string.Equals(aggregation.ActivityId, "activity_01", StringComparison.Ordinal))
            {
                return aggregation.DiscoveryCompleted && aggregation.DiscoveredCount >= 1
                    ? "Passed"
                    : "Waiting";
            }

            if (string.Equals(aggregation.ActivityId, "activity_02", StringComparison.Ordinal))
            {
                return aggregation.SkippedNoContent
                    ? "Passed"
                    : "Waiting";
            }

            if (aggregation.DiscoveryCompleted && aggregation.DiscoveredCount >= 1)
            {
                return "Passed";
            }

            if (aggregation.SkippedNoContent)
            {
                return "Passed";
            }

            return "Waiting";
        }

        private static string JoinValues(HashSet<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "<none>";
            }

            return string.Join(",", values);
        }

        private void TryEmitActivityObjectResetCheckpoint()
        {
            SessionActivityRuntimeState state = host.State;
            if (state == null || state.Facts == null || state.Facts.Count == 0)
            {
                return;
            }

            Dictionary<string, ActivityObjectResetCheckpointAggregation> byEntry = new(StringComparer.Ordinal);
            for (int i = 0; i < state.Facts.Count; i++)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Kind != SessionActivityFactKind.ObjectResetStarted &&
                    fact.Kind != SessionActivityFactKind.ObjectResetCommandIssued &&
                    fact.Kind != SessionActivityFactKind.ObjectResetApplied &&
                    fact.Kind != SessionActivityFactKind.ObjectResetSkippedOptional &&
                    fact.Kind != SessionActivityFactKind.ObjectResetFailed &&
                    fact.Kind != SessionActivityFactKind.ObjectResetCompleted)
                {
                    continue;
                }

                string key = $"{fact.Identity.ActivityId}|{fact.Identity.EntrySequence}";
                if (!byEntry.TryGetValue(key, out ActivityObjectResetCheckpointAggregation aggregation))
                {
                    aggregation = new ActivityObjectResetCheckpointAggregation(
                        fact.Identity.ActivityId,
                        fact.Identity.EntrySequence);
                }

                if (fact.Kind == SessionActivityFactKind.ObjectResetStarted)
                {
                    aggregation.ResetStarted = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ObjectResetCommandIssued)
                {
                    aggregation.CommandCount += 1;
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                    }

                    string resetGroup = ExtractToken(fact.Message, "resetGroup");
                    if (!string.Equals(resetGroup, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.ResetGroups.Add(resetGroup);
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ObjectResetApplied)
                {
                    aggregation.AppliedCount += 1;
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                    }

                    string resetGroup = ExtractToken(fact.Message, "resetGroup");
                    if (!string.Equals(resetGroup, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.ResetGroups.Add(resetGroup);
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ObjectResetSkippedOptional)
                {
                    aggregation.SkippedCount += 1;
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                    }

                    string resetGroup = ExtractToken(fact.Message, "resetGroup");
                    if (!string.Equals(resetGroup, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.ResetGroups.Add(resetGroup);
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ObjectResetFailed)
                {
                    aggregation.FailedCount += 1;
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                    }

                    string resetGroup = ExtractToken(fact.Message, "resetGroup");
                    if (!string.Equals(resetGroup, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.ResetGroups.Add(resetGroup);
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ObjectResetCompleted)
                {
                    aggregation.ResetCompleted = true;
                    aggregation.CompletionKind = ExtractToken(fact.Message, "completionKind");
                    aggregation.CompletionReason = ExtractToken(fact.Message, "completionReason");
                }

                byEntry[key] = aggregation;
            }

            foreach (KeyValuePair<string, ActivityObjectResetCheckpointAggregation> pair in byEntry)
            {
                ActivityObjectResetCheckpointAggregation aggregation = pair.Value;
                string targetIds = JoinValues(aggregation.TargetIds);
                string resetGroups = JoinValues(aggregation.ResetGroups);
                string checkpointStatus = ResolveActivityObjectResetCheckpointStatus(aggregation);
                string completionKind = string.IsNullOrWhiteSpace(aggregation.CompletionKind) ? "<none>" : aggregation.CompletionKind;
                string completionReason = string.IsNullOrWhiteSpace(aggregation.CompletionReason) ? "<none>" : aggregation.CompletionReason;

                string token =
                    $"{aggregation.ActivityId}|{aggregation.EntrySequence}|{aggregation.ResetStarted}|{aggregation.CommandCount}|{aggregation.AppliedCount}|{aggregation.SkippedCount}|{aggregation.FailedCount}|{targetIds}|{resetGroups}|{aggregation.ResetCompleted}|{checkpointStatus}|{completionKind}|{completionReason}";

                if (_lastActivityObjectResetCheckpointTokenByEntry.TryGetValue(pair.Key, out string lastToken) &&
                    string.Equals(lastToken, token, StringComparison.Ordinal))
                {
                    continue;
                }

                _lastActivityObjectResetCheckpointTokenByEntry[pair.Key] = token;
                Debug.Log(
                    $"[OBS][SessionActivityPipeline][QACheckpoint] checkpoint='ActivityObjectReset' checkpointStatus='{checkpointStatus}' " +
                    $"activityId='{aggregation.ActivityId}' entrySequence='{aggregation.EntrySequence}' resetStarted='{aggregation.ResetStarted.ToString().ToLowerInvariant()}' " +
                    $"commandCount='{aggregation.CommandCount}' appliedCount='{aggregation.AppliedCount}' skippedCount='{aggregation.SkippedCount}' failedCount='{aggregation.FailedCount}' " +
                    $"targetIds='{targetIds}' resetGroups='{resetGroups}' resetCompleted='{aggregation.ResetCompleted.ToString().ToLowerInvariant()}' " +
                    $"completionKind='{completionKind}' completionReason='{completionReason}'");
            }
        }

        private bool CanResetCurrentPlayerActor()
        {
            if (!IsHostStateAvailable())
            {
                return false;
            }

            return !host.State.CurrentPendingOperation.IsValid &&
                   host.State.CurrentIdentity.IsValid &&
                   host.State.CurrentStage == SessionActivityStage.ActivityRunning;
        }

        private static string ResolveActivityObjectResetCheckpointStatus(ActivityObjectResetCheckpointAggregation aggregation)
        {
            if (aggregation.FailedCount > 0)
            {
                return "Failed";
            }

            if (aggregation.ResetCompleted)
            {
                if (aggregation.AppliedCount > 0)
                {
                    return "PassedApplied";
                }

                if (string.Equals(aggregation.CompletionKind, "SkippedOptional", StringComparison.Ordinal))
                {
                    return "SkippedOptional";
                }

                if (string.Equals(aggregation.CompletionKind, "InventoryInvalidOrStale", StringComparison.Ordinal))
                {
                    return "SkippedInventoryInvalidOrStale";
                }

                if (string.Equals(aggregation.CompletionKind, "NoCommands", StringComparison.Ordinal) &&
                    aggregation.CommandCount == 0 &&
                    aggregation.FailedCount == 0)
                {
                    return "PassedNoCommands";
                }

                if (string.Equals(aggregation.CompletionKind, "NoApplicableGroups", StringComparison.Ordinal))
                {
                    return "SkippedNoApplicableGroups";
                }

                return "Passed";
            }

            return "Waiting";
        }

        private void TryEmitActivityObjectReleaseCheckpoint()
        {
            SessionActivityRuntimeState state = host.State;
            if (state == null || state.Facts == null || state.Facts.Count == 0)
            {
                return;
            }

            Dictionary<string, ActivityObjectReleaseCheckpointAggregation> byEntry = new(StringComparer.Ordinal);
            for (int i = 0; i < state.Facts.Count; i++)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Kind != SessionActivityFactKind.ObjectReleaseStarted &&
                    fact.Kind != SessionActivityFactKind.ObjectReleaseCommandIssued &&
                    fact.Kind != SessionActivityFactKind.ObjectReleaseApplied &&
                    fact.Kind != SessionActivityFactKind.ObjectReleaseSkippedOptional &&
                    fact.Kind != SessionActivityFactKind.ObjectReleaseFailed &&
                    fact.Kind != SessionActivityFactKind.ObjectReleaseCompleted)
                {
                    continue;
                }

                string key = $"{fact.Identity.ActivityId}|{fact.Identity.EntrySequence}";
                if (!byEntry.TryGetValue(key, out ActivityObjectReleaseCheckpointAggregation aggregation))
                {
                    aggregation = new ActivityObjectReleaseCheckpointAggregation(
                        fact.Identity.ActivityId,
                        fact.Identity.EntrySequence);
                }

                if (fact.Kind == SessionActivityFactKind.ObjectReleaseStarted)
                {
                    aggregation.ReleaseStarted = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ObjectReleaseCommandIssued)
                {
                    aggregation.CommandCount += 1;
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                    }

                    string releaseKind = ExtractToken(fact.Message, "releaseKind");
                    if (!string.Equals(releaseKind, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.ReleaseKinds.Add(releaseKind);
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ObjectReleaseApplied)
                {
                    aggregation.AppliedCount += 1;
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                    }

                    string releaseKind = ExtractToken(fact.Message, "releaseKind");
                    if (!string.Equals(releaseKind, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.ReleaseKinds.Add(releaseKind);
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ObjectReleaseSkippedOptional)
                {
                    aggregation.SkippedCount += 1;
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                    }

                    string releaseKind = ExtractToken(fact.Message, "releaseKind");
                    if (!string.Equals(releaseKind, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.ReleaseKinds.Add(releaseKind);
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ObjectReleaseFailed)
                {
                    aggregation.FailedCount += 1;
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                    }

                    string releaseKind = ExtractToken(fact.Message, "releaseKind");
                    if (!string.Equals(releaseKind, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.ReleaseKinds.Add(releaseKind);
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ObjectReleaseCompleted)
                {
                    aggregation.ReleaseCompleted = true;
                }

                byEntry[key] = aggregation;
            }

            foreach (KeyValuePair<string, ActivityObjectReleaseCheckpointAggregation> pair in byEntry)
            {
                ActivityObjectReleaseCheckpointAggregation aggregation = pair.Value;
                string targetIds = JoinValues(aggregation.TargetIds);
                string releaseKinds = JoinValues(aggregation.ReleaseKinds);
                string checkpointStatus = ResolveActivityObjectReleaseCheckpointStatus(aggregation);

                string token =
                    $"{aggregation.ActivityId}|{aggregation.EntrySequence}|{aggregation.ReleaseStarted}|{aggregation.CommandCount}|{aggregation.AppliedCount}|{aggregation.SkippedCount}|{aggregation.FailedCount}|{targetIds}|{releaseKinds}|{aggregation.ReleaseCompleted}|{checkpointStatus}";

                if (_lastActivityObjectReleaseCheckpointTokenByEntry.TryGetValue(pair.Key, out string lastToken) &&
                    string.Equals(lastToken, token, StringComparison.Ordinal))
                {
                    continue;
                }

                _lastActivityObjectReleaseCheckpointTokenByEntry[pair.Key] = token;
                Debug.Log(
                    $"[OBS][SessionActivityPipeline][QACheckpoint] checkpoint='ActivityObjectRelease' checkpointStatus='{checkpointStatus}' " +
                    $"activityId='{aggregation.ActivityId}' entrySequence='{aggregation.EntrySequence}' releaseStarted='{aggregation.ReleaseStarted.ToString().ToLowerInvariant()}' " +
                    $"commandCount='{aggregation.CommandCount}' appliedCount='{aggregation.AppliedCount}' skippedCount='{aggregation.SkippedCount}' failedCount='{aggregation.FailedCount}' " +
                    $"targetIds='{targetIds}' releaseKinds='{releaseKinds}' releaseCompleted='{aggregation.ReleaseCompleted.ToString().ToLowerInvariant()}'");
            }
        }

        private void TryEmitActivityObjectSnapshotCaptureCheckpoint()
        {
            SessionActivityRuntimeState state = host.State;
            if (state == null || state.Facts == null || state.Facts.Count == 0)
            {
                return;
            }

            Dictionary<string, ActivityObjectSnapshotCaptureCheckpointAggregation> byEntry = new(StringComparer.Ordinal);
            for (int i = 0; i < state.Facts.Count; i++)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotCaptureStarted &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotCaptured &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotCaptureSkippedNoProviders &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotCaptureFailed &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotCaptureCompleted)
                {
                    continue;
                }

                string key = $"{fact.Identity.ActivityId}|{fact.Identity.EntrySequence}";
                if (!byEntry.TryGetValue(key, out ActivityObjectSnapshotCaptureCheckpointAggregation aggregation))
                {
                    aggregation = new ActivityObjectSnapshotCaptureCheckpointAggregation(
                        fact.Identity.ActivityId,
                        fact.Identity.EntrySequence);
                }

                if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotCaptureStarted)
                {
                    aggregation.CaptureStarted = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotCaptured)
                {
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                    }

                    string hasTransformPayload = ExtractToken(fact.Message, "hasTransformPayload");
                    if (string.Equals(hasTransformPayload, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        aggregation.HasTransformPayload = true;
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotCaptureFailed)
                {
                    aggregation.CaptureFailed = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotCaptureCompleted)
                {
                    aggregation.CaptureCompleted = true;
                    aggregation.CapturedCount = Math.Max(aggregation.CapturedCount, ParseIntToken(fact.Message, "capturedCount"));
                    aggregation.FailedCount = Math.Max(aggregation.FailedCount, ParseIntToken(fact.Message, "failedCount"));
                    aggregation.RecordCount = Math.Max(aggregation.RecordCount, ParseIntToken(fact.Message, "recordCount"));

                    string envelopeSchemaId = ExtractToken(fact.Message, "envelopeSchemaId");
                    if (!string.IsNullOrWhiteSpace(envelopeSchemaId) && !string.Equals(envelopeSchemaId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.EnvelopeSchemaId = envelopeSchemaId;
                    }

                    string ownerKinds = ExtractToken(fact.Message, "ownerKinds");
                    if (!string.IsNullOrWhiteSpace(ownerKinds) && !string.Equals(ownerKinds, "<none>", StringComparison.Ordinal))
                    {
                        string[] splitOwnerKinds = ownerKinds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (int idx = 0; idx < splitOwnerKinds.Length; idx++)
                        {
                            string current = splitOwnerKinds[idx].Trim();
                            if (!string.IsNullOrWhiteSpace(current))
                            {
                                aggregation.OwnerKinds.Add(current);
                            }
                        }
                    }

                    string ids = ExtractToken(fact.Message, "targetIds");
                    if (!string.IsNullOrWhiteSpace(ids) && !string.Equals(ids, "<none>", StringComparison.Ordinal))
                    {
                        string[] splitTargetIds = ids.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (int idx = 0; idx < splitTargetIds.Length; idx++)
                        {
                            string current = splitTargetIds[idx].Trim();
                            if (!string.IsNullOrWhiteSpace(current))
                            {
                                aggregation.TargetIds.Add(current);
                            }
                        }
                    }

                    string hasTransformPayload = ExtractToken(fact.Message, "hasTransformPayload");
                    if (string.Equals(hasTransformPayload, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        aggregation.HasTransformPayload = true;
                    }
                }

                byEntry[key] = aggregation;
            }

            foreach (KeyValuePair<string, ActivityObjectSnapshotCaptureCheckpointAggregation> pair in byEntry)
            {
                ActivityObjectSnapshotCaptureCheckpointAggregation aggregation = pair.Value;
                string targetIds = JoinValues(aggregation.TargetIds);
                string ownerKinds = JoinValues(aggregation.OwnerKinds);
                string checkpointStatus = ResolveActivityObjectSnapshotCaptureCheckpointStatus(aggregation);

                string token =
                    $"{aggregation.ActivityId}|{aggregation.EntrySequence}|{aggregation.CaptureStarted}|{aggregation.CapturedCount}|{aggregation.RecordCount}|{aggregation.FailedCount}|{targetIds}|{ownerKinds}|{aggregation.EnvelopeSchemaId}|{aggregation.HasTransformPayload}|{aggregation.CaptureCompleted}|{aggregation.CaptureFailed}|{checkpointStatus}";

                if (_lastActivityObjectSnapshotCaptureCheckpointTokenByEntry.TryGetValue(pair.Key, out string lastToken) &&
                    string.Equals(lastToken, token, StringComparison.Ordinal))
                {
                    continue;
                }

                _lastActivityObjectSnapshotCaptureCheckpointTokenByEntry[pair.Key] = token;
                Debug.Log(
                    $"[OBS][SessionActivityPipeline][QACheckpoint] checkpoint='CapabilitySnapshotEnvelopeCapture' checkpointStatus='{checkpointStatus}' " +
                    $"activityId='{aggregation.ActivityId}' entrySequence='{aggregation.EntrySequence}' captureStarted='{aggregation.CaptureStarted.ToString().ToLowerInvariant()}' " +
                    $"capturedCount='{aggregation.CapturedCount}' recordCount='{aggregation.RecordCount}' failedCount='{aggregation.FailedCount}' " +
                    $"targetIds='{targetIds}' envelopeSchemaId='{aggregation.EnvelopeSchemaId}' ownerKinds='{ownerKinds}' hasTransformPayload='{aggregation.HasTransformPayload.ToString().ToLowerInvariant()}' " +
                    $"canonicalPayload='CapabilitySnapshotEnvelope' captureCompleted='{aggregation.CaptureCompleted.ToString().ToLowerInvariant()}' captureFailed='{aggregation.CaptureFailed.ToString().ToLowerInvariant()}'");
            }
        }

        private static string ResolveActivityObjectSnapshotCaptureCheckpointStatus(ActivityObjectSnapshotCaptureCheckpointAggregation aggregation)
        {
            if (aggregation.CaptureFailed || aggregation.FailedCount > 0)
            {
                return "Failed";
            }

            if (aggregation.CaptureCompleted && aggregation.RecordCount > 0)
            {
                return "Passed";
            }

            if (aggregation.CaptureCompleted)
            {
                return "PassedNoRecords";
            }

            return "Waiting";
        }

        private void TryEmitActivityObjectSnapshotRestoreCheckpoint()
        {
            SessionActivityRuntimeState state = host.State;
            if (state == null || state.Facts == null || state.Facts.Count == 0)
            {
                return;
            }

            Dictionary<string, ActivityObjectSnapshotRestoreCheckpointAggregation> byEntry = new(StringComparer.Ordinal);
            for (int i = 0; i < state.Facts.Count; i++)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotRestoreStarted &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotCaptured &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotRestoreApplied &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoPayload &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoMatchingTarget &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotRestoreSkippedNoEndpointOptional &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted)
                {
                    continue;
                }

                string key = $"{fact.Identity.ActivityId}|{fact.Identity.EntrySequence}";
                if (!byEntry.TryGetValue(key, out ActivityObjectSnapshotRestoreCheckpointAggregation aggregation))
                {
                    aggregation = new ActivityObjectSnapshotRestoreCheckpointAggregation(
                        fact.Identity.ActivityId,
                        fact.Identity.EntrySequence);
                }

                if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotRestoreStarted)
                {
                    aggregation.RestoreStarted = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotCaptured)
                {
                    string capturedTargetId = ExtractToken(fact.Message, "targetId");
                    string capturedTargetPath = ExtractToken(fact.Message, "targetTransformPath");
                    if (!string.Equals(capturedTargetId, "<none>", StringComparison.Ordinal) &&
                        !string.Equals(capturedTargetPath, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.CaptureTargetTransformPathByTargetId[capturedTargetId] = capturedTargetPath;
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotRestoreApplied)
                {
                    aggregation.RestoredCount += 1;
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                        aggregation.AppliedTargetIds.Add(targetId);
                    }

                    aggregation.PayloadPosition = ExtractToken(fact.Message, "payloadPosition");
                    aggregation.BeforePosition = ExtractToken(fact.Message, "beforePosition");
                    aggregation.AfterPosition = ExtractToken(fact.Message, "afterPosition");
                    aggregation.CoordinateSpace = ExtractToken(fact.Message, "coordinateSpace");
                    aggregation.RestoreVerified = string.Equals(ExtractToken(fact.Message, "restoreVerified"), "true", StringComparison.OrdinalIgnoreCase);
                    string restoreTargetPath = ExtractToken(fact.Message, "targetTransformPath");
                    if (!string.Equals(restoreTargetPath, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.RestoreTargetTransformPathByTargetId[targetId] = restoreTargetPath;
                        aggregation.RestoreTargetTransformPath = restoreTargetPath;
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotRestoreFailed)
                {
                    aggregation.RestoreFailed = true;
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.FailedTargetIds.Add(targetId);
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotRestoreCompleted)
                {
                    aggregation.RestoreCompleted = true;
                    aggregation.PayloadAvailable = string.Equals(ExtractToken(fact.Message, "payloadAvailable"), "true", StringComparison.OrdinalIgnoreCase);
                    aggregation.RecordCount = ParseIntToken(fact.Message, "recordCount");
                    aggregation.MatchedTargetCount = ParseIntToken(fact.Message, "matchedTargetCount");
                    aggregation.RestoredCount = Math.Max(aggregation.RestoredCount, ParseIntToken(fact.Message, "restoredCount"));
                    aggregation.RestoreFailed = aggregation.RestoreFailed || string.Equals(ExtractToken(fact.Message, "restoreFailed"), "true", StringComparison.OrdinalIgnoreCase);
                    string restoreVerifiedToken = ExtractToken(fact.Message, "restoreVerified");
                    if (!string.Equals(restoreVerifiedToken, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.RestoreVerified = string.Equals(restoreVerifiedToken, "true", StringComparison.OrdinalIgnoreCase);
                    }

                    string coordinateSpace = ExtractToken(fact.Message, "coordinateSpace");
                    if (!string.Equals(coordinateSpace, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.CoordinateSpace = coordinateSpace;
                    }

                    string appliedIds = ExtractToken(fact.Message, "appliedTargetIds");
                    if (!string.IsNullOrWhiteSpace(appliedIds) && !string.Equals(appliedIds, "<none>", StringComparison.Ordinal))
                    {
                        string[] splitApplied = appliedIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (int idx = 0; idx < splitApplied.Length; idx++)
                        {
                            string current = splitApplied[idx].Trim();
                            if (!string.IsNullOrWhiteSpace(current))
                            {
                                aggregation.AppliedTargetIds.Add(current);
                            }
                        }
                    }

                    string failedIds = ExtractToken(fact.Message, "failedTargetIds");
                    if (!string.IsNullOrWhiteSpace(failedIds) && !string.Equals(failedIds, "<none>", StringComparison.Ordinal))
                    {
                        string[] splitFailed = failedIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (int idx = 0; idx < splitFailed.Length; idx++)
                        {
                            string current = splitFailed[idx].Trim();
                            if (!string.IsNullOrWhiteSpace(current))
                            {
                                aggregation.FailedTargetIds.Add(current);
                            }
                        }
                    }

                    string ids = ExtractToken(fact.Message, "targetIds");
                    if (!string.IsNullOrWhiteSpace(ids) && !string.Equals(ids, "<none>", StringComparison.Ordinal))
                    {
                        string[] split = ids.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        for (int idx = 0; idx < split.Length; idx++)
                        {
                            string current = split[idx].Trim();
                            if (!string.IsNullOrWhiteSpace(current))
                            {
                                aggregation.TargetIds.Add(current);
                            }
                        }
                    }
                }

                byEntry[key] = aggregation;
            }

            foreach (KeyValuePair<string, ActivityObjectSnapshotRestoreCheckpointAggregation> pair in byEntry)
            {
                ActivityObjectSnapshotRestoreCheckpointAggregation aggregation = pair.Value;
                foreach (KeyValuePair<string, string> capturePair in aggregation.CaptureTargetTransformPathByTargetId)
                {
                    if (!aggregation.RestoreTargetTransformPathByTargetId.TryGetValue(capturePair.Key, out string restoredPath))
                    {
                        continue;
                    }

                    if (!string.Equals(capturePair.Value, restoredPath, StringComparison.Ordinal))
                    {
                        aggregation.SnapshotRestoreTargetTransformMismatch = true;
                        aggregation.MismatchReason = "snapshot_restore_target_transform_mismatch";
                        if (string.Equals(aggregation.CaptureTargetTransformPath, "<none>", StringComparison.Ordinal))
                        {
                            aggregation.CaptureTargetTransformPath = capturePair.Value;
                        }

                        if (string.Equals(aggregation.RestoreTargetTransformPath, "<none>", StringComparison.Ordinal))
                        {
                            aggregation.RestoreTargetTransformPath = restoredPath;
                        }
                    }
                }

                string targetIds = JoinValues(aggregation.TargetIds);
                string appliedTargetIds = JoinValues(aggregation.AppliedTargetIds);
                string failedTargetIds = JoinValues(aggregation.FailedTargetIds);
                string checkpointStatus = ResolveActivityObjectSnapshotRestoreCheckpointStatus(aggregation);

                string token =
                    $"{aggregation.ActivityId}|{aggregation.EntrySequence}|{aggregation.PayloadAvailable}|{aggregation.RecordCount}|{aggregation.MatchedTargetCount}|{aggregation.RestoredCount}|{targetIds}|{appliedTargetIds}|{failedTargetIds}|{aggregation.CoordinateSpace}|{aggregation.PayloadPosition}|{aggregation.BeforePosition}|{aggregation.AfterPosition}|{aggregation.RestoreVerified}|{aggregation.CaptureTargetTransformPath}|{aggregation.RestoreTargetTransformPath}|{aggregation.SnapshotRestoreTargetTransformMismatch}|{aggregation.MismatchReason}|{aggregation.RestoreCompleted}|{aggregation.RestoreFailed}|{checkpointStatus}";

                if (_lastActivityObjectSnapshotRestoreCheckpointTokenByEntry.TryGetValue(pair.Key, out string lastToken) &&
                    string.Equals(lastToken, token, StringComparison.Ordinal))
                {
                    continue;
                }

                _lastActivityObjectSnapshotRestoreCheckpointTokenByEntry[pair.Key] = token;
                Debug.Log(
                    $"[OBS][SessionActivityPipeline][QACheckpoint] checkpoint='ActivityObjectSnapshotRestore' checkpointStatus='{checkpointStatus}' " +
                    $"activityId='{aggregation.ActivityId}' entrySequence='{aggregation.EntrySequence}' payloadAvailable='{aggregation.PayloadAvailable.ToString().ToLowerInvariant()}' " +
                    $"recordCount='{aggregation.RecordCount}' matchedTargetCount='{aggregation.MatchedTargetCount}' restoredCount='{aggregation.RestoredCount}' " +
                    $"targetIds='{targetIds}' appliedTargetIds='{appliedTargetIds}' failedTargetIds='{failedTargetIds}' coordinateSpace='{aggregation.CoordinateSpace}' " +
                    $"captureTargetTransformPath='{aggregation.CaptureTargetTransformPath}' restoreTargetTransformPath='{aggregation.RestoreTargetTransformPath}' " +
                    $"snapshotRestoreTargetTransformMismatch='{aggregation.SnapshotRestoreTargetTransformMismatch.ToString().ToLowerInvariant()}' mismatchReason='{aggregation.MismatchReason}' " +
                    $"payloadPosition='{aggregation.PayloadPosition}' beforePosition='{aggregation.BeforePosition}' afterPosition='{aggregation.AfterPosition}' restoreVerified='{aggregation.RestoreVerified.ToString().ToLowerInvariant()}' " +
                    $"restoreCompleted='{aggregation.RestoreCompleted.ToString().ToLowerInvariant()}' restoreFailed='{aggregation.RestoreFailed.ToString().ToLowerInvariant()}'");
            }
        }

        private static string ResolveActivityObjectSnapshotRestoreCheckpointStatus(ActivityObjectSnapshotRestoreCheckpointAggregation aggregation)
        {
            if (aggregation.RestoredCount > 0 && !aggregation.RestoreVerified)
            {
                return "Failed";
            }

            if (aggregation.SnapshotRestoreTargetTransformMismatch)
            {
                return "Failed";
            }

            if (aggregation.RestoreFailed)
            {
                return "Failed";
            }

            if (aggregation.RestoreCompleted)
            {
                // Restore was completed, but check if it was actually applied or just skipped due to missing payload
                if (!aggregation.PayloadAvailable)
                {
                    // No payload was available - checkpoint should be Skipped/NotApplicable, not Passed
                    return "Skipped";
                }

                // Payload was available - check if restore was actually applied
                if (aggregation.MatchedTargetCount == 0)
                {
                    // Payload had no matching targets for this entry - checkpoint skipped
                    return "Skipped";
                }

                // Restore was applied - mark as passed only if verified
                if (aggregation.RestoredCount == aggregation.MatchedTargetCount && aggregation.RestoreVerified)
                {
                    return "Passed";
                }

                // Some targets had restore applied, but not all matched ones or not verified
                if (aggregation.RestoredCount > 0)
                {
                    return "Passed";
                }

                // No actual restore applied, even though payload and targets matched
                return "Skipped";
            }

            return "Waiting";
        }

        private static string ResolveActivityObjectReleaseCheckpointStatus(ActivityObjectReleaseCheckpointAggregation aggregation)
        {
            if (aggregation.FailedCount > 0)
            {
                return "Failed";
            }

            if (aggregation.ReleaseCompleted)
            {
                return "Passed";
            }

            return "Waiting";
        }

        private void TryEmitActivityObjectSnapshotContractValidationCheckpoint()
        {
            SessionActivityRuntimeState state = host.State;
            if (state == null || state.Facts == null || state.Facts.Count == 0)
            {
                return;
            }

            Dictionary<string, ActivityObjectSnapshotContractValidationCheckpointAggregation> byEntry = new(StringComparer.Ordinal);
            for (int i = 0; i < state.Facts.Count; i++)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotContractValidationStarted &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotContractValidated &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotContractSkippedOptional &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotContractFailed &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectSnapshotContractValidationCompleted)
                {
                    continue;
                }

                string key = $"{fact.Identity.ActivityId}|{fact.Identity.EntrySequence}";
                if (!byEntry.TryGetValue(key, out ActivityObjectSnapshotContractValidationCheckpointAggregation aggregation))
                {
                    aggregation = new ActivityObjectSnapshotContractValidationCheckpointAggregation(
                        fact.Identity.ActivityId,
                        fact.Identity.EntrySequence);
                }

                if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotContractValidationStarted)
                {
                    aggregation.ValidationStarted = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotContractValidated)
                {
                    aggregation.ValidatedCount += 1;
                    aggregation.TargetIds.Add(ExtractToken(fact.Message, "targetId"));
                    aggregation.ProviderPaths.Add(ExtractToken(fact.Message, "providerPath"));
                    aggregation.RestoreEndpointPaths.Add(ExtractToken(fact.Message, "restoreEndpointPath"));
                    aggregation.TargetTransformPaths.Add(ExtractToken(fact.Message, "targetTransformPath"));
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotContractSkippedOptional)
                {
                    aggregation.SkippedCount += 1;
                    aggregation.TargetIds.Add(ExtractToken(fact.Message, "targetId"));
                    aggregation.ProviderPaths.Add(ExtractToken(fact.Message, "providerPath"));
                    aggregation.RestoreEndpointPaths.Add(ExtractToken(fact.Message, "restoreEndpointPath"));
                    aggregation.TargetTransformPaths.Add(ExtractToken(fact.Message, "targetTransformPath"));
                    string reason = ExtractToken(fact.Message, "reason");
                    if (!string.IsNullOrWhiteSpace(reason) &&
                        !string.Equals(reason, "<none>", StringComparison.Ordinal) &&
                        !string.Equals(reason, "snapshot_capability_not_declared_optional", StringComparison.Ordinal))
                    {
                        aggregation.MismatchReason = reason;
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotContractFailed)
                {
                    aggregation.FailedCount += 1;
                    aggregation.TargetIds.Add(ExtractToken(fact.Message, "targetId"));
                    aggregation.ProviderPaths.Add(ExtractToken(fact.Message, "providerPath"));
                    aggregation.RestoreEndpointPaths.Add(ExtractToken(fact.Message, "restoreEndpointPath"));
                    aggregation.TargetTransformPaths.Add(ExtractToken(fact.Message, "providerTargetTransformPath"));
                    aggregation.TargetTransformPaths.Add(ExtractToken(fact.Message, "restoreTargetTransformPath"));
                    string reason = ExtractToken(fact.Message, "reason");
                    aggregation.MismatchReason = string.IsNullOrWhiteSpace(reason) ? "contract_failed" : reason;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectSnapshotContractValidationCompleted)
                {
                    aggregation.ValidationCompleted = true;
                    aggregation.ValidatedCount = Math.Max(aggregation.ValidatedCount, ParseIntToken(fact.Message, "validatedCount"));
                    aggregation.SkippedCount = Math.Max(aggregation.SkippedCount, ParseIntToken(fact.Message, "skippedCount"));
                    aggregation.FailedCount = Math.Max(aggregation.FailedCount, ParseIntToken(fact.Message, "failedCount"));
                    AddCsvTokens(aggregation.TargetIds, ExtractToken(fact.Message, "targetIds"));
                    AddCsvTokens(aggregation.ProviderPaths, ExtractToken(fact.Message, "providerPaths"));
                    AddCsvTokens(aggregation.RestoreEndpointPaths, ExtractToken(fact.Message, "restoreEndpointPaths"));
                    AddCsvTokens(aggregation.TargetTransformPaths, ExtractToken(fact.Message, "targetTransformPaths"));
                    string mismatchReason = ExtractToken(fact.Message, "mismatchReason");
                    if (!string.IsNullOrWhiteSpace(mismatchReason) && !string.Equals(mismatchReason, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.MismatchReason = mismatchReason;
                    }
                }

                byEntry[key] = aggregation;
            }

            foreach (KeyValuePair<string, ActivityObjectSnapshotContractValidationCheckpointAggregation> pair in byEntry)
            {
                ActivityObjectSnapshotContractValidationCheckpointAggregation aggregation = pair.Value;
                string targetIds = JoinValues(aggregation.TargetIds);
                string providerPaths = JoinValues(aggregation.ProviderPaths);
                string restoreEndpointPaths = JoinValues(aggregation.RestoreEndpointPaths);
                string targetTransformPaths = JoinValues(aggregation.TargetTransformPaths);
                string checkpointStatus = ResolveActivityObjectSnapshotContractValidationCheckpointStatus(aggregation);

                string token = $"{aggregation.ActivityId}|{aggregation.EntrySequence}|{aggregation.ValidationStarted}|{aggregation.ValidatedCount}|{aggregation.SkippedCount}|{aggregation.FailedCount}|{targetIds}|{providerPaths}|{restoreEndpointPaths}|{targetTransformPaths}|{aggregation.MismatchReason}|{aggregation.ValidationCompleted}|{checkpointStatus}";
                if (_lastActivityObjectSnapshotContractValidationCheckpointTokenByEntry.TryGetValue(pair.Key, out string lastToken) &&
                    string.Equals(lastToken, token, StringComparison.Ordinal))
                {
                    continue;
                }

                _lastActivityObjectSnapshotContractValidationCheckpointTokenByEntry[pair.Key] = token;
                Debug.Log(
                    $"[OBS][SessionActivityPipeline][QACheckpoint] checkpoint='ActivityObjectSnapshotContractValidation' checkpointStatus='{checkpointStatus}' " +
                    $"activityId='{aggregation.ActivityId}' entrySequence='{aggregation.EntrySequence}' validationStarted='{aggregation.ValidationStarted.ToString().ToLowerInvariant()}' " +
                    $"validatedCount='{aggregation.ValidatedCount}' skippedCount='{aggregation.SkippedCount}' failedCount='{aggregation.FailedCount}' " +
                    $"targetIds='{targetIds}' providerPaths='{providerPaths}' restoreEndpointPaths='{restoreEndpointPaths}' targetTransformPaths='{targetTransformPaths}' " +
                    $"mismatchReason='{aggregation.MismatchReason}' validationCompleted='{aggregation.ValidationCompleted.ToString().ToLowerInvariant()}'");
            }
        }

        private static string ResolveActivityObjectSnapshotContractValidationCheckpointStatus(ActivityObjectSnapshotContractValidationCheckpointAggregation aggregation)
        {
            if (aggregation.FailedCount > 0)
            {
                return "Failed";
            }

            if (aggregation.ValidationCompleted &&
                string.Equals(aggregation.MismatchReason, "<none>", StringComparison.Ordinal))
            {
                return "Passed";
            }

            return "Waiting";
        }

        private void TryEmitActivityObjectContributorUnregisterCheckpoint()
        {
            SessionActivityRuntimeState state = host.State;
            if (state == null || state.Facts == null || state.Facts.Count == 0)
            {
                return;
            }

            Dictionary<string, ActivityObjectContributorUnregisterCheckpointAggregation> byEntry = new(StringComparer.Ordinal);
            for (int i = 0; i < state.Facts.Count; i++)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Kind != SessionActivityFactKind.ActivityObjectContributorUnregisterStarted &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectContributorUnregistered &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectContributorUnregisterSkippedNoContributors &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectContributorUnregisterCompleted &&
                    fact.Kind != SessionActivityFactKind.ActivityObjectContributorUnregisterFailed)
                {
                    continue;
                }

                string key = $"{fact.Identity.ActivityId}|{fact.Identity.EntrySequence}";
                if (!byEntry.TryGetValue(key, out ActivityObjectContributorUnregisterCheckpointAggregation aggregation))
                {
                    aggregation = new ActivityObjectContributorUnregisterCheckpointAggregation(
                        fact.Identity.ActivityId,
                        fact.Identity.EntrySequence);
                }

                if (fact.Kind == SessionActivityFactKind.ActivityObjectContributorUnregisterStarted)
                {
                    aggregation.UnregisterStarted = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectContributorUnregistered)
                {
                    aggregation.UnregisteredCount += 1;
                    string targetId = ExtractToken(fact.Message, "targetId");
                    if (!string.Equals(targetId, "<none>", StringComparison.Ordinal))
                    {
                        aggregation.TargetIds.Add(targetId);
                    }
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectContributorUnregisterSkippedNoContributors)
                {
                    aggregation.SkippedNoContributors = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectContributorUnregisterCompleted)
                {
                    aggregation.UnregisterCompleted = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityObjectContributorUnregisterFailed)
                {
                    aggregation.UnregisterFailed = true;
                }

                byEntry[key] = aggregation;
            }

            foreach (KeyValuePair<string, ActivityObjectContributorUnregisterCheckpointAggregation> pair in byEntry)
            {
                ActivityObjectContributorUnregisterCheckpointAggregation aggregation = pair.Value;
                string targetIds = JoinValues(aggregation.TargetIds);
                string checkpointStatus = ResolveActivityObjectContributorUnregisterCheckpointStatus(aggregation);

                string token =
                    $"{aggregation.ActivityId}|{aggregation.EntrySequence}|{aggregation.UnregisterStarted}|{aggregation.UnregisteredCount}|{aggregation.SkippedNoContributors}|{aggregation.UnregisterCompleted}|{aggregation.UnregisterFailed}|{targetIds}|{checkpointStatus}";

                if (_lastActivityObjectContributorUnregisterCheckpointTokenByEntry.TryGetValue(pair.Key, out string lastToken) &&
                    string.Equals(lastToken, token, StringComparison.Ordinal))
                {
                    continue;
                }

                _lastActivityObjectContributorUnregisterCheckpointTokenByEntry[pair.Key] = token;
                Debug.Log(
                    $"[OBS][SessionActivityPipeline][QACheckpoint] checkpoint='ActivityObjectContributorUnregister' checkpointStatus='{checkpointStatus}' " +
                    $"activityId='{aggregation.ActivityId}' entrySequence='{aggregation.EntrySequence}' unregisterStarted='{aggregation.UnregisterStarted.ToString().ToLowerInvariant()}' " +
                    $"unregisteredCount='{aggregation.UnregisteredCount}' skippedNoContributors='{aggregation.SkippedNoContributors.ToString().ToLowerInvariant()}' " +
                    $"unregisterCompleted='{aggregation.UnregisterCompleted.ToString().ToLowerInvariant()}' unregisterFailed='{aggregation.UnregisterFailed.ToString().ToLowerInvariant()}' " +
                    $"targetIds='{targetIds}'");
            }
        }

        private static string ResolveActivityObjectContributorUnregisterCheckpointStatus(ActivityObjectContributorUnregisterCheckpointAggregation aggregation)
        {
            if (aggregation.UnregisterFailed)
            {
                return "Failed";
            }

            if (aggregation.UnregisterCompleted)
            {
                return "Passed";
            }

            return "Waiting";
        }

        private struct ActivityObjectContributorDiscoveryCheckpointAggregation
        {
            public ActivityObjectContributorDiscoveryCheckpointAggregation(string activityId, int entrySequence)
            {
                ActivityId = activityId;
                EntrySequence = entrySequence;
                DiscoveryStarted = false;
                DiscoveredCount = 0;
                DiscoveryCompleted = false;
                DiscoveryFailed = false;
                SkippedNoContent = false;
                TargetIds = new HashSet<string>(StringComparer.Ordinal);
                RoleIds = new HashSet<string>(StringComparer.Ordinal);
                ContributorKinds = new HashSet<string>(StringComparer.Ordinal);
            }

            public string ActivityId;
            public int EntrySequence;
            public bool DiscoveryStarted;
            public int DiscoveredCount;
            public bool DiscoveryCompleted;
            public bool DiscoveryFailed;
            public bool SkippedNoContent;
            public HashSet<string> TargetIds;
            public HashSet<string> RoleIds;
            public HashSet<string> ContributorKinds;
        }

        private struct ActivityObjectResetCheckpointAggregation
        {
            public ActivityObjectResetCheckpointAggregation(string activityId, int entrySequence)
            {
                ActivityId = activityId;
                EntrySequence = entrySequence;
                ResetStarted = false;
                CommandCount = 0;
                AppliedCount = 0;
                SkippedCount = 0;
                FailedCount = 0;
                ResetCompleted = false;
                CompletionKind = string.Empty;
                CompletionReason = string.Empty;
                TargetIds = new HashSet<string>(StringComparer.Ordinal);
                ResetGroups = new HashSet<string>(StringComparer.Ordinal);
            }

            public string ActivityId;
            public int EntrySequence;
            public bool ResetStarted;
            public int CommandCount;
            public int AppliedCount;
            public int SkippedCount;
            public int FailedCount;
            public bool ResetCompleted;
            public string CompletionKind;
            public string CompletionReason;
            public HashSet<string> TargetIds;
            public HashSet<string> ResetGroups;
        }

        private struct ActivityObjectReleaseCheckpointAggregation
        {
            public ActivityObjectReleaseCheckpointAggregation(string activityId, int entrySequence)
            {
                ActivityId = activityId;
                EntrySequence = entrySequence;
                ReleaseStarted = false;
                CommandCount = 0;
                AppliedCount = 0;
                SkippedCount = 0;
                FailedCount = 0;
                ReleaseCompleted = false;
                TargetIds = new HashSet<string>(StringComparer.Ordinal);
                ReleaseKinds = new HashSet<string>(StringComparer.Ordinal);
            }

            public string ActivityId;
            public int EntrySequence;
            public bool ReleaseStarted;
            public int CommandCount;
            public int AppliedCount;
            public int SkippedCount;
            public int FailedCount;
            public bool ReleaseCompleted;
            public HashSet<string> TargetIds;
            public HashSet<string> ReleaseKinds;
        }

        private struct ActivityObjectSnapshotCaptureCheckpointAggregation
        {
            public ActivityObjectSnapshotCaptureCheckpointAggregation(string activityId, int entrySequence)
            {
                ActivityId = activityId;
                EntrySequence = entrySequence;
                CaptureStarted = false;
                CapturedCount = 0;
                RecordCount = 0;
                FailedCount = 0;
                TargetIds = new HashSet<string>(StringComparer.Ordinal);
                OwnerKinds = new HashSet<string>(StringComparer.Ordinal);
                EnvelopeSchemaId = "<none>";
                HasTransformPayload = false;
                CaptureCompleted = false;
                CaptureFailed = false;
            }

            public string ActivityId;
            public int EntrySequence;
            public bool CaptureStarted;
            public int CapturedCount;
            public int RecordCount;
            public int FailedCount;
            public HashSet<string> TargetIds;
            public HashSet<string> OwnerKinds;
            public string EnvelopeSchemaId;
            public bool HasTransformPayload;
            public bool CaptureCompleted;
            public bool CaptureFailed;
        }

        private struct ActivityObjectSnapshotContractValidationCheckpointAggregation
        {
            public ActivityObjectSnapshotContractValidationCheckpointAggregation(string activityId, int entrySequence)
            {
                ActivityId = activityId;
                EntrySequence = entrySequence;
                ValidationStarted = false;
                ValidatedCount = 0;
                SkippedCount = 0;
                FailedCount = 0;
                TargetIds = new HashSet<string>(StringComparer.Ordinal);
                ProviderPaths = new HashSet<string>(StringComparer.Ordinal);
                RestoreEndpointPaths = new HashSet<string>(StringComparer.Ordinal);
                TargetTransformPaths = new HashSet<string>(StringComparer.Ordinal);
                MismatchReason = "<none>";
                ValidationCompleted = false;
            }

            public string ActivityId;
            public int EntrySequence;
            public bool ValidationStarted;
            public int ValidatedCount;
            public int SkippedCount;
            public int FailedCount;
            public HashSet<string> TargetIds;
            public HashSet<string> ProviderPaths;
            public HashSet<string> RestoreEndpointPaths;
            public HashSet<string> TargetTransformPaths;
            public string MismatchReason;
            public bool ValidationCompleted;
        }

        private struct ActivityObjectSnapshotRestoreCheckpointAggregation
        {
            public ActivityObjectSnapshotRestoreCheckpointAggregation(string activityId, int entrySequence)
            {
                ActivityId = activityId;
                EntrySequence = entrySequence;
                PayloadAvailable = false;
                RecordCount = 0;
                MatchedTargetCount = 0;
                RestoredCount = 0;
                TargetIds = new HashSet<string>(StringComparer.Ordinal);
                AppliedTargetIds = new HashSet<string>(StringComparer.Ordinal);
                FailedTargetIds = new HashSet<string>(StringComparer.Ordinal);
                CoordinateSpace = "world_transform";
                PayloadPosition = "<none>";
                BeforePosition = "<none>";
                AfterPosition = "<none>";
                CaptureTargetTransformPath = "<none>";
                RestoreTargetTransformPath = "<none>";
                SnapshotRestoreTargetTransformMismatch = false;
                MismatchReason = "<none>";
                CaptureTargetTransformPathByTargetId = new Dictionary<string, string>(StringComparer.Ordinal);
                RestoreTargetTransformPathByTargetId = new Dictionary<string, string>(StringComparer.Ordinal);
                RestoreVerified = false;
                RestoreStarted = false;
                RestoreCompleted = false;
                RestoreFailed = false;
            }

            public string ActivityId;
            public int EntrySequence;
            public bool PayloadAvailable;
            public int RecordCount;
            public int MatchedTargetCount;
            public int RestoredCount;
            public HashSet<string> TargetIds;
            public HashSet<string> AppliedTargetIds;
            public HashSet<string> FailedTargetIds;
            public string CoordinateSpace;
            public string PayloadPosition;
            public string BeforePosition;
            public string AfterPosition;
            public string CaptureTargetTransformPath;
            public string RestoreTargetTransformPath;
            public bool SnapshotRestoreTargetTransformMismatch;
            public string MismatchReason;
            public Dictionary<string, string> CaptureTargetTransformPathByTargetId;
            public Dictionary<string, string> RestoreTargetTransformPathByTargetId;
            public bool RestoreVerified;
            public bool RestoreStarted;
            public bool RestoreCompleted;
            public bool RestoreFailed;
        }

        private struct ActivityObjectContributorUnregisterCheckpointAggregation
        {
            public ActivityObjectContributorUnregisterCheckpointAggregation(string activityId, int entrySequence)
            {
                ActivityId = activityId;
                EntrySequence = entrySequence;
                UnregisterStarted = false;
                UnregisteredCount = 0;
                SkippedNoContributors = false;
                UnregisterCompleted = false;
                UnregisterFailed = false;
                TargetIds = new HashSet<string>(StringComparer.Ordinal);
            }

            public string ActivityId;
            public int EntrySequence;
            public bool UnregisterStarted;
            public int UnregisteredCount;
            public bool SkippedNoContributors;
            public bool UnregisterCompleted;
            public bool UnregisterFailed;
            public HashSet<string> TargetIds;
        }

        private bool TryRunSmokeStep(string smokeName, string stepName, SessionActivityStage? expectedStage, Action action)
        {
            SessionActivityRuntimeState state = host.State;
            if (state.CurrentPendingOperation.IsValid)
            {
                LogSmokeStepBlocked(smokeName, stepName, expectedStage, "pending_operation_active");
                return false;
            }

            if (expectedStage.HasValue && state.CurrentStage != expectedStage.Value)
            {
                LogSmokeStepBlocked(smokeName, stepName, expectedStage, "unexpected_stage");
                return false;
            }

            action();
            return true;
        }

        private void LogSmokeStepBlocked(string smokeName, string stepName, SessionActivityStage? expectedStage, string reason)
        {
            SessionActivityRuntimeState state = host.State;
            string expectedStageValue = expectedStage.HasValue ? expectedStage.Value.ToString() : "<any>";
            Debug.Log($"[OBS][SessionActivityPipeline][QA] SmokeStepBlocked smokeName='{smokeName}' stepName='{stepName}' expectedStage='{expectedStageValue}' actualStage='{state.CurrentStage}' activityId='{state.CurrentDefinition.ActivityId}' entrySequence='{state.CurrentEntrySequence}' reason='{reason}'");
        }

        private void TryEmitActivity01ToActivity02Checkpoint()
        {
            SessionActivityRuntimeState state = host.State;
            const string checkpointName = "Activity01ToActivity02";
            int latestFromEntrySequence = ResolveLatestEntrySequenceForActivity("activity_01");
            if (latestFromEntrySequence <= 0)
            {
                return;
            }

            if (_activeCheckpointFromEntrySequence <= 0 || latestFromEntrySequence > _activeCheckpointFromEntrySequence)
            {
                _activeCheckpointFromEntrySequence = latestFromEntrySequence;
                _activeCheckpointToEntrySequence = 0;
                _activeCheckpointObservedActivity02Running = false;
            }

            if (_activeCheckpointFromEntrySequence == _frozenPassedCheckpointFromEntrySequence &&
                _activeCheckpointToEntrySequence == _frozenPassedCheckpointToEntrySequence)
            {
                return;
            }

            int fromEntrySequence = _activeCheckpointFromEntrySequence;
            if (_activeCheckpointToEntrySequence <= 0)
            {
                _activeCheckpointToEntrySequence = ResolveFirstEntrySequenceForActivityAfter("activity_02", fromEntrySequence);
            }

            int toEntrySequence = _activeCheckpointToEntrySequence;
            if (toEntrySequence > 0 &&
                string.Equals(state.CurrentDefinition.ActivityId, "activity_02", StringComparison.Ordinal) &&
                state.CurrentEntrySequence == toEntrySequence &&
                state.CurrentStage == SessionActivityStage.ActivityRunning)
            {
                _activeCheckpointObservedActivity02Running = true;
            }

            bool releaseStarted = false;
            bool releaseCompleted = false;
            string releaseSceneName = "<none>";
            string releaseStatus = "<none>";
            bool activity02ReachedRunning = _activeCheckpointObservedActivity02Running;
            bool activity02SkipNoRequirementsObserved = false;
            bool participantCommandsForActivity02Observed = false;

            for (int i = 0; i < state.Facts.Count; i++)
            {
                SessionActivityFact fact = state.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Identity.EntrySequence == fromEntrySequence)
                {
                    if (fact.Kind == SessionActivityFactKind.ActivityContentReleaseStarted)
                    {
                        releaseStarted = true;
                    }
                    else if (fact.Kind == SessionActivityFactKind.ActivityContentReleaseCompleted)
                    {
                        releaseCompleted = true;
                    }
                    else if (fact.Kind == SessionActivityFactKind.ActivityContentSceneUnloaded)
                    {
                        releaseSceneName = ExtractToken(fact.Message, "sceneName");
                        releaseStatus = ExtractToken(fact.Message, "releaseStatus");
                    }
                }

                if (!string.Equals(fact.Identity.ActivityId, "activity_02", StringComparison.Ordinal))
                {
                    continue;
                }

                if (toEntrySequence <= 0)
                {
                    continue;
                }

                if (fact.Identity.EntrySequence != toEntrySequence)
                {
                    continue;
                }

                if (fact.Kind == SessionActivityFactKind.ActivityRunningEntered && _activeCheckpointObservedActivity02Running)
                {
                    activity02ReachedRunning = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivitySetupInventorySkippedNoRequirements ||
                         fact.Kind == SessionActivityFactKind.ActivityParticipantBindingSkippedNoRequirements)
                {
                    activity02SkipNoRequirementsObserved = true;
                }
                else if (fact.Kind == SessionActivityFactKind.ActivityParticipantCommandPlanReady ||
                         fact.Kind == SessionActivityFactKind.ActivityParticipantBindCommandIssued ||
                         fact.Kind == SessionActivityFactKind.ActivityParticipantMaterializationCommandIssued ||
                         fact.Kind == SessionActivityFactKind.ActivityParticipantPlacementCommandIssued ||
                         fact.Kind == SessionActivityFactKind.ActivityParticipantResetCommandIssued)
                {
                    participantCommandsForActivity02Observed = true;
                }
            }

            bool releaseSceneIsLoadedAfterRelease = false;
            bool shouldEvaluateSceneLoadedForActiveScope = releaseCompleted &&
                                                           toEntrySequence > 0 &&
                                                           !_activeCheckpointObservedActivity02Running &&
                                                           state.CurrentEntrySequence <= toEntrySequence;
            if (shouldEvaluateSceneLoadedForActiveScope)
            {
                releaseSceneIsLoadedAfterRelease = ResolveSceneLoaded("ActivityScene01");
            }

            bool complete = releaseStarted &&
                            releaseCompleted &&
                            string.Equals(releaseSceneName, "ActivityScene01", StringComparison.Ordinal) &&
                            string.Equals(releaseStatus, "Unloaded", StringComparison.Ordinal) &&
                            !releaseSceneIsLoadedAfterRelease &&
                            activity02ReachedRunning &&
                            activity02SkipNoRequirementsObserved &&
                            !participantCommandsForActivity02Observed;

            string checkpointStatus = complete ? "Passed" : "Waiting";
            string failedCriterion = "<none>";
            if (releaseCompleted &&
                !string.Equals(releaseSceneName, "<none>", StringComparison.Ordinal) &&
                !string.Equals(releaseSceneName, "ActivityScene01", StringComparison.Ordinal))
            {
                checkpointStatus = "Failed";
                failedCriterion = "releaseSceneName";
            }
            else if (releaseCompleted && releaseSceneIsLoadedAfterRelease)
            {
                checkpointStatus = "Failed";
                failedCriterion = "releaseSceneIsLoadedAfterRelease";
            }
            else if (releaseCompleted &&
                     !string.Equals(releaseStatus, "<none>", StringComparison.Ordinal) &&
                     !string.Equals(releaseStatus, "Unloaded", StringComparison.Ordinal))
            {
                checkpointStatus = "Failed";
                failedCriterion = "releaseStatus";
            }
            else if (participantCommandsForActivity02Observed)
            {
                checkpointStatus = "Failed";
                failedCriterion = "participantCommandsForActivity02Observed";
            }
            else if (activity02ReachedRunning && !activity02SkipNoRequirementsObserved)
            {
                checkpointStatus = "Failed";
                failedCriterion = "activity02SkipNoRequirementsObserved";
            }

            if (string.Equals(checkpointStatus, "Passed", StringComparison.Ordinal))
            {
                _frozenPassedCheckpointFromEntrySequence = fromEntrySequence;
                _frozenPassedCheckpointToEntrySequence = toEntrySequence;
            }

            string token =
                $"{fromEntrySequence}|{toEntrySequence}|{checkpointStatus}|{failedCriterion}|{releaseStarted}|{releaseCompleted}|{releaseSceneName}|{releaseStatus}|{releaseSceneIsLoadedAfterRelease}|{activity02ReachedRunning}|{activity02SkipNoRequirementsObserved}|{participantCommandsForActivity02Observed}";
            if (string.Equals(token, _lastActivity01ToActivity02CheckpointToken, StringComparison.Ordinal))
            {
                return;
            }

            _lastActivity01ToActivity02CheckpointToken = token;
            Debug.Log(
                $"[OBS][SessionActivityPipeline][QACheckpoint] checkpoint='{checkpointName}' checkpointStatus='{checkpointStatus}' failedCriterion='{failedCriterion}' " +
                $"fromActivity='activity_01' toActivity='activity_02' fromEntrySequence='{fromEntrySequence}' toEntrySequence='{toEntrySequence}' " +
                $"releaseStarted='{releaseStarted.ToString().ToLowerInvariant()}' releaseCompleted='{releaseCompleted.ToString().ToLowerInvariant()}' " +
                $"releaseSceneName='{releaseSceneName}' releaseStatus='{releaseStatus}' releaseSceneIsLoadedAfterRelease='{releaseSceneIsLoadedAfterRelease.ToString().ToLowerInvariant()}' " +
                $"activity02ReachedRunning='{activity02ReachedRunning.ToString().ToLowerInvariant()}' activity02SkipNoRequirementsObserved='{activity02SkipNoRequirementsObserved.ToString().ToLowerInvariant()}' " +
                $"participantCommandsForActivity02Observed='{participantCommandsForActivity02Observed.ToString().ToLowerInvariant()}'");
        }

        private int ResolveLatestEntrySequenceForActivity(string activityId)
        {
            int latest = 0;
            for (int i = 0; i < host.State.Facts.Count; i++)
            {
                SessionActivityFact fact = host.State.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (!string.Equals(fact.Identity.ActivityId, activityId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (fact.Kind == SessionActivityFactKind.DeactivationWindowReady ||
                    fact.Kind == SessionActivityFactKind.ActivityContentReleaseStarted ||
                    fact.Kind == SessionActivityFactKind.ActivityContentReleaseCompleted ||
                    fact.Kind == SessionActivityFactKind.ContinueAccepted ||
                    fact.Kind == SessionActivityFactKind.ActivityRunningEntered)
                {
                    latest = Math.Max(latest, fact.Identity.EntrySequence);
                }
            }

            return latest;
        }

        private bool TryResolveLatestRestartAccepted(out string fromActivity, out int fromEntrySequence, out int toEntrySequence)
        {
            fromActivity = string.Empty;
            fromEntrySequence = 0;
            toEntrySequence = 0;

            for (int i = host.State.Facts.Count - 1; i >= 0; i--)
            {
                SessionActivityFact fact = host.State.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Kind != SessionActivityFactKind.ActivityRestartAccepted)
                {
                    continue;
                }

                fromActivity = fact.Identity.ActivityId;
                fromEntrySequence = fact.Identity.EntrySequence;
                string parsed = ExtractToken(fact.Message, "nextEntrySequence");
                if (!int.TryParse(parsed, out toEntrySequence) || toEntrySequence <= 0)
                {
                    toEntrySequence = fromEntrySequence + 1;
                }

                return true;
            }

            return false;
        }

        private bool TryResolveLatestRouteExitContext(out string activityId, out int entrySequence)
        {
            activityId = string.Empty;
            entrySequence = 0;

            for (int i = host.State.Facts.Count - 1; i >= 0; i--)
            {
                SessionActivityFact fact = host.State.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (fact.Kind == SessionActivityFactKind.ActivityRouteExitRequested ||
                    fact.Kind == SessionActivityFactKind.ActivityRouteExitCompleted)
                {
                    activityId = fact.Identity.ActivityId;
                    entrySequence = fact.Identity.EntrySequence;
                    return true;
                }
            }

            if (host.State.CurrentStage == SessionActivityStage.ClosedForRouteExit &&
                host.State.CurrentIdentity.IsValid)
            {
                activityId = host.State.CurrentIdentity.ActivityId;
                entrySequence = host.State.CurrentIdentity.EntrySequence;
                return true;
            }

            return false;
        }

        private bool ContainsTraceToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            for (int i = 0; i < host.State.Trace.Count; i++)
            {
                string line = host.State.Trace[i];
                if (line != null && line.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private int ResolveFirstEntrySequenceForActivityAfter(string activityId, int minimumExclusiveEntrySequence)
        {
            int candidate = 0;
            for (int i = 0; i < host.State.Facts.Count; i++)
            {
                SessionActivityFact fact = host.State.Facts[i];
                if (!fact.IsValid || !fact.Identity.IsValid)
                {
                    continue;
                }

                if (!string.Equals(fact.Identity.ActivityId, activityId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (fact.Identity.EntrySequence <= minimumExclusiveEntrySequence)
                {
                    continue;
                }

                if (candidate == 0 || fact.Identity.EntrySequence < candidate)
                {
                    candidate = fact.Identity.EntrySequence;
                }
            }

            return candidate;
        }

        private static string ExtractToken(string message, string key)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(key))
            {
                return "<none>";
            }

            string token = key + "='";
            int start = message.IndexOf(token, StringComparison.Ordinal);
            if (start < 0)
            {
                return "<none>";
            }

            start += token.Length;
            int end = message.IndexOf('\'', start);
            if (end <= start)
            {
                return "<none>";
            }

            return message.Substring(start, end - start);
        }

        private static int ParseIntToken(string message, string key)
        {
            string token = ExtractToken(message, key);
            if (string.IsNullOrWhiteSpace(token) || string.Equals(token, "<none>", StringComparison.Ordinal))
            {
                return 0;
            }

            return int.TryParse(token, out int parsed) ? parsed : 0;
        }

        private static void AddCsvTokens(HashSet<string> target, string csv)
        {
            if (target == null || string.IsNullOrWhiteSpace(csv) || string.Equals(csv, "<none>", StringComparison.Ordinal))
            {
                return;
            }

            string[] split = csv.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < split.Length; i++)
            {
                string current = split[i].Trim();
                if (!string.IsNullOrWhiteSpace(current))
                {
                    target.Add(current);
                }
            }
        }

        private static bool ResolveSceneLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName.Trim());
            return scene.IsValid() && scene.isLoaded;
        }

        private static bool ResolveSceneLoadedOrActive(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            string normalizedSceneName = sceneName.Trim();
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && string.Equals(activeScene.name, normalizedSceneName, StringComparison.Ordinal))
            {
                return true;
            }

            return ResolveSceneLoaded(normalizedSceneName);
        }

        private ActivityTransitionContinuePolicy ResolveCurrentContinuePolicy()
        {
            SessionActivityDefinition current = host.State.CurrentDefinition;
            return current.IsValid
                ? current.NextActivityTransitionContinuePolicy
                : ActivityTransitionContinuePolicy.Unknown;
        }

        private string FormatActivitySetupInventory()
        {
            if (host == null || host.Pipeline == null)
            {
                return "<none>";
            }

            ActivitySetupInventory inventory = host.Pipeline.EntryPipeline.GetCurrentActivitySetupInventory();
            if (!inventory.IsValid || string.IsNullOrWhiteSpace(inventory.InventoryId))
            {
                return "<none>";
            }

            return $"inventoryId='{inventory.InventoryId}', totalRequirements='{inventory.TotalRequirementCount}'";
        }
    }
}
