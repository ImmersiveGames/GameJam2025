using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteActivitySaveSaveOnExitResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalRouteActivitySaveSaveOnExitCommand
    {
        public OperationalRouteActivitySaveSaveOnExitCommand(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRouteCommand routeCommand,
            RouteActivitySavePlan routeActivitySavePlan,
            string source,
            string reason)
        {
            RuntimeModeConfig = runtimeModeConfig;
            RouteCommand = routeCommand;
            RouteActivitySavePlan = routeActivitySavePlan;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public RuntimeModeConfig RuntimeModeConfig { get; }
        public SessionOperationalRouteCommand RouteCommand { get; }
        public RouteActivitySavePlan RouteActivitySavePlan { get; }
        public string Source { get; }
        public string Reason { get; }

        public string PreviousRouteIdentity => RouteActivitySavePlan.SaveOnExit.PreviousRouteIdentity;
        public string PreviousActivityIdentity => RouteActivitySavePlan.SaveOnExit.PreviousActivityIdentity;

        public bool IsValid =>
            RuntimeModeConfig != null &&
            RouteCommand.IsValid &&
            RouteActivitySavePlan.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalRouteActivitySaveSaveOnExitResult
    {
        public OperationalRouteActivitySaveSaveOnExitResult(
            OperationalRouteActivitySaveSaveOnExitResultKind kind,
            string reason,
            string detail)
        {
            Kind = kind;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalRouteActivitySaveSaveOnExitResultKind Kind { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalRouteActivitySaveSaveOnExitResultKind.Completed;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalRouteActivitySaveQaSaveCommand
    {
        public OperationalRouteActivitySaveQaSaveCommand(
            RuntimeModeConfig runtimeModeConfig,
            string sessionStateId,
            string requestedActivityIdentity,
            string source,
            string reason)
        {
            RuntimeModeConfig = runtimeModeConfig;
            SessionStateId = Normalize(sessionStateId);
            SaveOwnerActivityIdentity = Normalize(sessionStateId);
            RequestedActivityIdentity = Normalize(requestedActivityIdentity);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public RuntimeModeConfig RuntimeModeConfig { get; }
        public string SessionStateId { get; }
        public string SaveOwnerActivityIdentity { get; }
        public string RequestedActivityIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            RuntimeModeConfig != null &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalRouteActivitySaveSaveOnExitStage
    {
        private readonly ISessionOperationalActivitySaveAdapter _activitySaveAdapter;
        private readonly IProgressionSlotContextResolver _progressionSlotContextResolver;
        private readonly Func<ISessionActivitySnapshotPayloadProvider> _activitySnapshotPayloadProviderResolver;
        private readonly string _routeActivitySnapshotSchemaId;

        public OperationalRouteActivitySaveSaveOnExitStage(
            ISessionOperationalActivitySaveAdapter activitySaveAdapter,
            IProgressionSlotContextResolver progressionSlotContextResolver,
            Func<ISessionActivitySnapshotPayloadProvider> activitySnapshotPayloadProviderResolver,
            string routeActivitySnapshotSchemaId)
        {
            _activitySaveAdapter = activitySaveAdapter ?? throw new ArgumentNullException(nameof(activitySaveAdapter));
            _progressionSlotContextResolver = progressionSlotContextResolver ?? throw new ArgumentNullException(nameof(progressionSlotContextResolver));
            _activitySnapshotPayloadProviderResolver = activitySnapshotPayloadProviderResolver ?? throw new ArgumentNullException(nameof(activitySnapshotPayloadProviderResolver));
            _routeActivitySnapshotSchemaId = string.IsNullOrWhiteSpace(routeActivitySnapshotSchemaId)
                ? throw new ArgumentException("routeActivitySnapshotSchemaId is required.", nameof(routeActivitySnapshotSchemaId))
                : routeActivitySnapshotSchemaId.Trim();
        }

        public OperationalRouteActivitySaveSaveOnExitResult Execute(OperationalRouteActivitySaveSaveOnExitCommand command)
        {
            if (!command.IsValid)
            {
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    "invalid_command",
                    "OperationalRouteActivitySaveSaveOnExitCommand invalido.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;
            RouteActivitySaveOnExitPlan saveOnExitPlan = command.RouteActivitySavePlan.SaveOnExit;
            RouteActivitySaveContributorScopePolicy contributorScopePolicy = saveOnExitPlan.PreviousRouteContributorScopePolicy;
            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveOnExitStageStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' contributorScopePolicy='{contributorScopePolicy}' saveOnExitSkipKind='{saveOnExitPlan.SkipKind}' saveOnExitSkipReason='{RouteActivitySaveSkipKindMapper.ToCode(saveOnExitPlan.SkipKind)}' saveOnExitSkipDetail='{Normalize(saveOnExitPlan.SkipDetail)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            ExecuteSaveOnExitOrFail(command);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveOnExitStageCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' contributorScopePolicy='{contributorScopePolicy}' saveOnExitSkipKind='{saveOnExitPlan.SkipKind}' saveOnExitSkipReason='{RouteActivitySaveSkipKindMapper.ToCode(saveOnExitPlan.SkipKind)}' saveOnExitSkipDetail='{Normalize(saveOnExitPlan.SkipDetail)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new OperationalRouteActivitySaveSaveOnExitResult(
                OperationalRouteActivitySaveSaveOnExitResultKind.Completed,
                "completed",
                "route_activity_save_save_on_exit_completed");
        }

        public OperationalRouteActivitySaveSaveOnExitResult ExecuteQaSaveCurrentSnapshot(OperationalRouteActivitySaveQaSaveCommand command)
        {
            if (!command.IsValid)
            {
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    "invalid_qa_command",
                    "OperationalRouteActivitySaveQaSaveCommand invalido.");
            }

            const string qaRouteIdentity = "qa.route_activity_save";
            const string qaRouteOperationId = "qa.route_activity_save|manual";
            const string qaTransitionId = "qa.route_activity_save|manual|snapshot";
            const int qaRouteSequence = 1;

            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave][QA] RouteActivitySaveQaSaveStarted sessionStateId='{Normalize(command.SessionStateId)}' saveOwnerActivityIdentity='{Normalize(command.SaveOwnerActivityIdentity)}' requestedActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            ISessionActivitySnapshotPayloadProvider provider = _activitySnapshotPayloadProviderResolver();
            if (provider == null)
            {
                DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveQaSave' checkpointStatus='Failed' activityIdentity='{Normalize(command.SaveOwnerActivityIdentity)}' saveOwnerActivityIdentity='{Normalize(command.SaveOwnerActivityIdentity)}' payloadActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' requestedActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' payloadResolved='false' payloadKind='<none>' recordCount='0' contributorResolutionKind='no_session_save_contributors' failureReason='no_session_save_contributors' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    "no_session_save_contributors",
                    "ISessionActivitySnapshotPayloadProvider ausente para QA save.");
            }

            if (!provider.TryGetSnapshotPayloadForSaveOnExit(
                    command.SessionStateId,
                    out SessionActivitySnapshotPayload payload,
                    out string failureReason) ||
                !payload.IsValid)
            {
                string normalizedFailure = Normalize(failureReason);
                if (string.IsNullOrWhiteSpace(normalizedFailure))
                {
                    normalizedFailure = "snapshot_payload_expected_but_missing";
                }

                if (string.Equals(normalizedFailure, "no_activity_content_contributors", StringComparison.Ordinal))
                {
                    DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                        $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveQaSave' checkpointStatus='SkippedNoContent' activityIdentity='{Normalize(command.SaveOwnerActivityIdentity)}' saveOwnerActivityIdentity='{Normalize(command.SaveOwnerActivityIdentity)}' payloadActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' requestedActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' payloadResolved='false' payloadKind='<none>' recordCount='0' contributorResolutionKind='{normalizedFailure}' skipReason='{normalizedFailure}' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Info);
                    return new OperationalRouteActivitySaveSaveOnExitResult(
                        OperationalRouteActivitySaveSaveOnExitResultKind.Completed,
                        "qa_save_skipped_no_content",
                        "snapshot payload skipped because current activity has no content contributors.");
                }

                DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveQaSave' checkpointStatus='Failed' activityIdentity='{Normalize(command.SaveOwnerActivityIdentity)}' saveOwnerActivityIdentity='{Normalize(command.SaveOwnerActivityIdentity)}' payloadActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' requestedActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' payloadResolved='false' payloadKind='<none>' recordCount='0' contributorResolutionKind='{normalizedFailure}' failureReason='{normalizedFailure}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    normalizedFailure,
                    "snapshot payload ausente/invalido para QA save.");
            }

            string actualActivityIdentity = Normalize(payload.ActivityId);
            string saveOwnerActivityIdentity = Normalize(command.SaveOwnerActivityIdentity);
            if (string.IsNullOrWhiteSpace(saveOwnerActivityIdentity))
            {
                saveOwnerActivityIdentity = Normalize(command.SessionStateId);
            }

            if (!string.IsNullOrWhiteSpace(command.RequestedActivityIdentity) &&
                !string.Equals(command.RequestedActivityIdentity, actualActivityIdentity, StringComparison.Ordinal))
            {
                DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveQaSave' checkpointStatus='Failed' activityIdentity='{saveOwnerActivityIdentity}' saveOwnerActivityIdentity='{saveOwnerActivityIdentity}' payloadActivityIdentity='{actualActivityIdentity}' requestedActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' payloadResolved='true' payloadKind='{ResolveSnapshotPayloadKind(payload)}' recordCount='{ResolveSnapshotPayloadRecordCount(payload)}' contributorResolutionKind='snapshot_identity_mismatch' failureReason='snapshot_identity_mismatch' sourceActivityId='{actualActivityIdentity}' sourceEntrySequence='{payload.EntrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    "snapshot_identity_mismatch",
                    "requested activity identity differs from captured payload activity identity.");
            }

            string serializedPayload = SerializeSnapshotPayload(payload);
            if (string.IsNullOrWhiteSpace(serializedPayload))
            {
                DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveQaSave' checkpointStatus='Failed' activityIdentity='{saveOwnerActivityIdentity}' saveOwnerActivityIdentity='{saveOwnerActivityIdentity}' payloadActivityIdentity='{actualActivityIdentity}' requestedActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' payloadResolved='true' payloadKind='{ResolveSnapshotPayloadKind(payload)}' recordCount='{ResolveSnapshotPayloadRecordCount(payload)}' contributorResolutionKind='snapshot_payload_serialization_failed' failureReason='snapshot_payload_serialization_failed' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    "snapshot_payload_serialization_failed",
                    "snapshot payload serialization failed for QA save.");
            }

            ProgressionSlotContext slotContext = ResolveProgressionSlotContextOrFail(
                _progressionSlotContextResolver,
                qaRouteIdentity,
                qaRouteOperationId,
                qaTransitionId,
                qaRouteSequence,
                command.Source,
                command.Reason);

            RouteActivitySaveSaveResult saveResult = _activitySaveAdapter.SaveActivityOnExit(
                command.RuntimeModeConfig,
                slotContext,
                saveOwnerActivityIdentity,
                serializedPayload);

            string payloadKind = ResolveSnapshotPayloadKind(payload);
            int recordCount = ResolveSnapshotPayloadRecordCount(payload);
            string targetIds = ResolveSnapshotPayloadTargetIdsLabel(payload);
            string contributorResolutionKind = payload.HasCapabilitySnapshotEnvelope
                ? "capability_snapshot_envelope_resolved"
                : "snapshot_payload_resolved";

            if (!saveResult.IsSaved)
            {
                DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveQaSave' checkpointStatus='Failed' activityIdentity='{saveOwnerActivityIdentity}' saveOwnerActivityIdentity='{saveOwnerActivityIdentity}' payloadActivityIdentity='{actualActivityIdentity}' requestedActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' payloadResolved='true' payloadKind='{payloadKind}' recordCount='{recordCount}' targetIds='{Normalize(targetIds)}' contributorResolutionKind='{contributorResolutionKind}' failureReason='{Normalize(saveResult.Detail)}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    string.IsNullOrWhiteSpace(saveResult.Detail) ? "save_not_completed" : Normalize(saveResult.Detail),
                    saveResult.Detail);
            }

            ObserveCurrentSnapshotPointerAfterSave(
                qaRouteIdentity,
                qaRouteOperationId,
                qaTransitionId,
                qaRouteSequence,
                saveOwnerActivityIdentity,
                slotContext,
                payloadKind,
                recordCount,
                "qa_save",
                command.Source,
                command.Reason);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave][QA] RouteActivitySaveQaSaveCompleted activityIdentity='{saveOwnerActivityIdentity}' saveOwnerActivityIdentity='{saveOwnerActivityIdentity}' payloadActivityIdentity='{actualActivityIdentity}' requestedActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' payloadKind='{payloadKind}' recordCount='{recordCount}' targetIds='{Normalize(targetIds)}' contributorResolutionKind='{contributorResolutionKind}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' detail='{Normalize(saveResult.Detail)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveQaSave' checkpointStatus='Passed' activityIdentity='{saveOwnerActivityIdentity}' saveOwnerActivityIdentity='{saveOwnerActivityIdentity}' payloadActivityIdentity='{actualActivityIdentity}' requestedActivityIdentity='{Normalize(command.RequestedActivityIdentity)}' payloadResolved='true' payloadKind='{payloadKind}' recordCount='{recordCount}' targetIds='{Normalize(targetIds)}' contributorResolutionKind='{contributorResolutionKind}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' payloadSize='{serializedPayload.Length}' detail='{Normalize(saveResult.Detail)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new OperationalRouteActivitySaveSaveOnExitResult(
                OperationalRouteActivitySaveSaveOnExitResultKind.Completed,
                "qa_save_completed",
                saveResult.Detail);
        }

        private void ExecuteSaveOnExitOrFail(OperationalRouteActivitySaveSaveOnExitCommand command)
        {
            RouteActivitySaveOnExitPlan saveOnExitPlan = command.RouteActivitySavePlan.SaveOnExit;
            RouteActivitySaveContributorScopePolicy contributorScopePolicy = saveOnExitPlan.PreviousRouteContributorScopePolicy;
            string currentRouteIdentity = saveOnExitPlan.CurrentRouteIdentity;
            string currentRouteOperationId = saveOnExitPlan.CurrentRouteOperationId;
            string currentTransitionId = saveOnExitPlan.CurrentTransitionId;
            int currentRouteSequence = saveOnExitPlan.CurrentRouteSequence;

            if (!saveOnExitPlan.ShouldSave)
            {
                LogRouteActivitySaveSaveSkipped(
                    saveOnExitPlan,
                    saveOnExitPlan.PreviousRouteContributorScopePolicy,
                    string.Empty,
                    saveOnExitPlan.SkipKind,
                    saveOnExitPlan.SkipDetail,
                    command.Source,
                    command.Reason);
                return;
            }

            string previousActivityIdentity = saveOnExitPlan.PreviousActivityIdentity;
            string previousActivitySaveKey = saveOnExitPlan.PreviousActivitySaveKey;

            if (!TryResolvePreviousActivitySnapshotPayload(command, contributorScopePolicy, out string activitySnapshotPayload, out RouteActivitySnapshotPayloadResolution payloadResolution))
            {
                bool isCaptureFailed = payloadResolution.FailureKind == RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed;
                bool isNoContent = payloadResolution.FailureKind == RouteActivitySaveSnapshotFailureKind.NoActivityContentContributors;
                RouteActivitySaveSkipKind skipKind = ResolveSnapshotPayloadSkipReason(payloadResolution.FailureKind);
                string checkpointStatus = isCaptureFailed ? "Failed" : isNoContent ? "SkippedNoContent" : "Waiting";
                DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotPayload' checkpointStatus='{checkpointStatus}' activityIdentity='{Normalize(previousActivityIdentity)}' contributorScopePolicy='{contributorScopePolicy}' contributorResolutionKind='{Normalize(payloadResolution.ContributorResolutionKind)}' sourceActivityId='<none>' sourceEntrySequence='0' payloadResolved='false' recordCount='0' payloadKind='<none>' targetIds='<none>' payloadSize='0' failureReason='{Normalize(payloadResolution.FailureReason)}'.",
                    DebugUtility.Colors.Info);
                if (isCaptureFailed)
                {
                    LogRouteActivitySaveCaptureFailed(
                        saveOnExitPlan,
                        contributorScopePolicy,
                        payloadResolution.ContributorResolutionKind,
                        skipKind,
                        Normalize(payloadResolution.FailureReason),
                        command.Source,
                        command.Reason);
                }
                else
                {
                    LogRouteActivitySaveSaveSkipped(
                        saveOnExitPlan,
                        contributorScopePolicy,
                        payloadResolution.ContributorResolutionKind,
                        skipKind,
                        Normalize(payloadResolution.FailureReason),
                        command.Source,
                        command.Reason);
                }
                return;
            }

            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySnapshotPayloadResolved previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' activityIdentity='{Normalize(previousActivityIdentity)}' contributorScopePolicy='{contributorScopePolicy}' contributorResolutionKind='{Normalize(payloadResolution.ContributorResolutionKind)}' sourceActivityId='{Normalize(payloadResolution.SourceActivityId)}' sourceEntrySequence='{payloadResolution.SourceEntrySequence}' recordCount='{payloadResolution.RecordCount}' payloadKind='{ResolvePayloadKindFromSerialized(activitySnapshotPayload)}' targetIds='{Normalize(payloadResolution.TargetIds)}' schemaId='{Normalize(payloadResolution.SchemaId)}' payloadSize='{payloadResolution.PayloadSize}' payloadResolutionReason='snapshot_payload_resolved'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotPayload' checkpointStatus='Passed' activityIdentity='{Normalize(previousActivityIdentity)}' contributorScopePolicy='{contributorScopePolicy}' contributorResolutionKind='{Normalize(payloadResolution.ContributorResolutionKind)}' sourceActivityId='{Normalize(payloadResolution.SourceActivityId)}' sourceEntrySequence='{payloadResolution.SourceEntrySequence}' payloadResolved='true' recordCount='{payloadResolution.RecordCount}' payloadKind='{ResolvePayloadKindFromSerialized(activitySnapshotPayload)}' targetIds='{Normalize(payloadResolution.TargetIds)}' payloadSize='{payloadResolution.PayloadSize}'.",
                DebugUtility.Colors.Info);

            if (string.IsNullOrWhiteSpace(activitySnapshotPayload))
            {
                LogRouteActivitySaveSaveSkipped(
                    saveOnExitPlan,
                    contributorScopePolicy,
                    string.Empty,
                    RouteActivitySaveSkipKind.NoSaveContributors,
                    "no_save_contributors",
                    command.Source,
                    command.Reason);
                return;
            }

            ProgressionSlotContext slotContext = ResolveProgressionSlotContextOrFail(
                _progressionSlotContextResolver,
                currentRouteIdentity,
                currentRouteOperationId,
                currentTransitionId,
                currentRouteSequence,
                command.Source,
                command.Reason);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveStarted previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' previousActivityIdentity='{Normalize(previousActivityIdentity)}' previousActivitySaveKey='{previousActivitySaveKey}' currentRouteIdentity='{Normalize(currentRouteIdentity)}' currentRouteOperationId='{Normalize(currentRouteOperationId)}' currentTransitionId='{Normalize(currentTransitionId)}' routeSequence='{currentRouteSequence}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            RouteActivitySaveSaveResult saveResult = _activitySaveAdapter.SaveActivityOnExit(
                command.RuntimeModeConfig,
                slotContext,
                previousActivityIdentity,
                activitySnapshotPayload);

            if (saveResult.IsSkipped)
            {
                LogRouteActivitySaveSaveSkipped(
                    saveOnExitPlan,
                    contributorScopePolicy,
                    string.Empty,
                    saveResult.SkipKind,
                    saveResult.Detail,
                    command.Source,
                    command.Reason);
                return;
            }

            if (!saveResult.IsSaved)
            {
                string message =
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] save-on-exit retornou estado invalido previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' currentRouteIdentity='{currentRouteIdentity}' currentRouteOperationId='{currentRouteOperationId}' currentTransitionId='{currentTransitionId}'.";
                DebugUtility.LogError<OperationalRouteActivitySaveSaveOnExitStage>(message);
                throw new InvalidOperationException(message);
            }

            ObserveCurrentSnapshotPointerAfterSave(
                currentRouteIdentity,
                currentRouteOperationId,
                currentTransitionId,
                currentRouteSequence,
                previousActivityIdentity,
                slotContext,
                ResolvePayloadKindFromSerialized(activitySnapshotPayload),
                payloadResolution.RecordCount,
                "save_on_exit",
                command.Source,
                command.Reason);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveCompleted previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' previousActivityIdentity='{Normalize(previousActivityIdentity)}' previousActivitySaveKey='{previousActivitySaveKey}' currentRouteIdentity='{Normalize(currentRouteIdentity)}' currentRouteOperationId='{Normalize(currentRouteOperationId)}' currentTransitionId='{Normalize(currentTransitionId)}' routeSequence='{currentRouteSequence}' detail='{Normalize(saveResult.Detail)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
        }

        private void ObserveCurrentSnapshotPointerAfterSave(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string activityIdentity,
            ProgressionSlotContext expectedSlotContext,
            string payloadKind,
            int recordCount,
            string operationKind,
            string source,
            string reason)
        {
            ProgressionSlotContext observedSlotContext = ResolveProgressionSlotContextOrFail(
                _progressionSlotContextResolver,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            string expectedSnapshotId = expectedSlotContext != null && expectedSlotContext.SnapshotId.IsValid
                ? expectedSlotContext.SnapshotId.Value
                : string.Empty;
            string observedSnapshotId = observedSlotContext != null && observedSlotContext.SnapshotId.IsValid
                ? observedSlotContext.SnapshotId.Value
                : string.Empty;
            string expectedSlotId = expectedSlotContext != null && expectedSlotContext.SlotId.IsValid
                ? expectedSlotContext.SlotId.Value
                : string.Empty;
            string observedSlotId = observedSlotContext != null && observedSlotContext.SlotId.IsValid
                ? observedSlotContext.SlotId.Value
                : string.Empty;
            bool snapshotIdMatches = !string.IsNullOrWhiteSpace(expectedSnapshotId) &&
                                     string.Equals(expectedSnapshotId, observedSnapshotId, StringComparison.Ordinal);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveCurrentSnapshotPointerObserved operationKind='{Normalize(operationKind)}' activityIdentity='{Normalize(activityIdentity)}' routeIdentity='{Normalize(routeIdentity)}' routeOperationId='{Normalize(routeOperationId)}' transitionId='{Normalize(transitionId)}' routeSequence='{routeSequence}' expectedSlotId='{Normalize(expectedSlotId)}' observedSlotId='{Normalize(observedSlotId)}' expectedSnapshotId='{Normalize(expectedSnapshotId)}' observedSnapshotId='{Normalize(observedSnapshotId)}' snapshotIdMatches='{snapshotIdMatches.ToString().ToLowerInvariant()}' payloadKind='{Normalize(payloadKind)}' recordCount='{recordCount}' pointerOwner='ProgressionSlotContextResolver' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                snapshotIdMatches ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
        }

        private static void LogRouteActivitySaveSaveSkipped(
            RouteActivitySaveOnExitPlan saveOnExitPlan,
            RouteActivitySaveContributorScopePolicy contributorScopePolicy,
            string contributorResolutionKind,
            RouteActivitySaveSkipKind skipKind,
            string detail,
            string source,
            string reason)
        {
            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveSkipped previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' previousActivityIdentity='{Normalize(saveOnExitPlan.PreviousActivityIdentity)}' previousActivitySaveKey='{Normalize(saveOnExitPlan.PreviousActivitySaveKey)}' contributorScopePolicy='{contributorScopePolicy}' contributorResolutionKind='{Normalize(contributorResolutionKind)}' currentRouteIdentity='{Normalize(saveOnExitPlan.CurrentRouteIdentity)}' currentRouteOperationId='{Normalize(saveOnExitPlan.CurrentRouteOperationId)}' currentTransitionId='{Normalize(saveOnExitPlan.CurrentTransitionId)}' routeSequence='{saveOnExitPlan.CurrentRouteSequence}' skipKind='{skipKind}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(skipKind)}' detail='{Normalize(detail)}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogRouteActivitySaveCaptureFailed(
            RouteActivitySaveOnExitPlan saveOnExitPlan,
            RouteActivitySaveContributorScopePolicy contributorScopePolicy,
            string contributorResolutionKind,
            RouteActivitySaveSkipKind failureKind,
            string detail,
            string source,
            string reason)
        {
            DebugUtility.LogError(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveFailed previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' previousActivityIdentity='{Normalize(saveOnExitPlan.PreviousActivityIdentity)}' previousActivitySaveKey='{Normalize(saveOnExitPlan.PreviousActivitySaveKey)}' contributorScopePolicy='{contributorScopePolicy}' contributorResolutionKind='{Normalize(contributorResolutionKind)}' currentRouteIdentity='{Normalize(saveOnExitPlan.CurrentRouteIdentity)}' currentRouteOperationId='{Normalize(saveOnExitPlan.CurrentRouteOperationId)}' currentTransitionId='{Normalize(saveOnExitPlan.CurrentTransitionId)}' routeSequence='{saveOnExitPlan.CurrentRouteSequence}' failureKind='{failureKind}' failureReason='{RouteActivitySaveSkipKindMapper.ToCode(failureKind)}' detail='{Normalize(detail)}' source='{source}' reason='{reason}'.");
        }

        private bool TryResolvePreviousActivitySnapshotPayload(
            OperationalRouteActivitySaveSaveOnExitCommand command,
            RouteActivitySaveContributorScopePolicy contributorScopePolicy,
            out string activitySnapshotPayload,
            out RouteActivitySnapshotPayloadResolution resolution)
        {
            activitySnapshotPayload = string.Empty;
            resolution = default;
            RouteActivitySaveOnExitPlan saveOnExitPlan = command.RouteActivitySavePlan.SaveOnExit;

            if (!saveOnExitPlan.HasPreviousRoute)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    recordCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.NoPreviousRoute,
                    failureReason: "no_previous_route",
                    contributorResolutionKind: "no_previous_route");
                return false;
            }

            string sessionStateId = Normalize(saveOnExitPlan.PreviousActivityIdentity);
            if (string.IsNullOrWhiteSpace(sessionStateId))
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    recordCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.NoSessionActivity,
                    failureReason: "no_session_activity",
                    contributorResolutionKind: "no_session_activity");
                return false;
            }

            if (contributorScopePolicy == RouteActivitySaveContributorScopePolicy.CurrentRouteSaveContributors)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    recordCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.NoRouteSaveContributors,
                    failureReason: "no_route_save_contributors",
                    contributorResolutionKind: "no_route_save_contributors");
                return false;
            }

            ISessionActivitySnapshotPayloadProvider provider = _activitySnapshotPayloadProviderResolver();
            if (provider == null)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    recordCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.NoSessionSaveContributors,
                    failureReason: "no_session_save_contributors",
                    contributorResolutionKind: "no_session_save_contributors");
                return false;
            }

            bool resolved = provider.TryGetSnapshotPayloadForSaveOnExit(
                sessionStateId,
                out SessionActivitySnapshotPayload payload,
                out string failureReason);

            if (!resolved)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    recordCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: ResolveFailureKindFromFailureReason(failureReason),
                    failureReason: Normalize(failureReason),
                    contributorResolutionKind: Normalize(failureReason));
                return false;
            }

            if (!payload.IsValid)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    recordCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed,
                    failureReason: string.IsNullOrWhiteSpace(failureReason) ? "snapshot_capture_failed" : Normalize(failureReason),
                    contributorResolutionKind: "snapshot_capture_failed");
                return false;
            }

            activitySnapshotPayload = SerializeSnapshotPayload(payload);
            if (string.IsNullOrWhiteSpace(activitySnapshotPayload))
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    payload.SchemaId,
                    payload.ActivityId,
                    payload.EntrySequence,
                    ResolveSnapshotPayloadRecordCount(payload),
                    ResolveSnapshotPayloadTargetIdsLabel(payload),
                    0,
                    RouteActivitySaveSnapshotFailureKind.SnapshotPayloadInvalid,
                    "snapshot_payload_serialization_failed",
                    "snapshot_payload_serialization_failed");
                return false;
            }

            int resolvedRecordCount = ResolveSnapshotPayloadRecordCount(payload);
            string resolvedTargetIds = ResolveSnapshotPayloadTargetIdsLabel(payload);
            resolution = new RouteActivitySnapshotPayloadResolution(
                payload.SchemaId,
                payload.ActivityId,
                payload.EntrySequence,
                resolvedRecordCount,
                resolvedTargetIds,
                activitySnapshotPayload.Length,
                RouteActivitySaveSnapshotFailureKind.SnapshotPayloadResolved,
                payload.HasCapabilitySnapshotEnvelope ? "capability_snapshot_envelope_resolved" : "snapshot_payload_resolved",
                payload.HasCapabilitySnapshotEnvelope ? "capability_snapshot_envelope_resolved" : "snapshot_payload_resolved");
            return true;
        }

        private static RouteActivitySaveSkipKind ResolveSnapshotPayloadSkipReason(RouteActivitySaveSnapshotFailureKind failureKind)
        {
            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotProviderUnavailable)
            {
                return RouteActivitySaveSkipKind.NoSessionSaveContributors;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed)
            {
                return RouteActivitySaveSkipKind.SnapshotCaptureFailed;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotPayloadInvalid)
            {
                return RouteActivitySaveSkipKind.SnapshotPayloadInvalid;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotPayloadResolved)
            {
                return RouteActivitySaveSkipKind.SnapshotPayloadResolved;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.NoSessionActivity)
            {
                return RouteActivitySaveSkipKind.NoSessionActivity;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.NoActivityContentContributors)
            {
                return RouteActivitySaveSkipKind.NoActivityContentContributors;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.NoRouteSaveContributors)
            {
                return RouteActivitySaveSkipKind.NoRouteSaveContributors;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.NoSessionSaveContributors)
            {
                return RouteActivitySaveSkipKind.NoSessionSaveContributors;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.NoSaveContributors)
            {
                return RouteActivitySaveSkipKind.NoSaveContributors;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotPayloadExpectedButMissing)
            {
                return RouteActivitySaveSkipKind.SnapshotPayloadExpectedButMissing;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotPayloadMissing)
            {
                return RouteActivitySaveSkipKind.SnapshotPayloadExpectedButMissing;
            }

            return RouteActivitySaveSkipKind.SnapshotPayloadExpectedButMissing;
        }

        private static RouteActivitySaveSnapshotFailureKind ResolveFailureKindFromFailureReason(string failureReason)
        {
            string normalized = Normalize(failureReason);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return RouteActivitySaveSnapshotFailureKind.SnapshotPayloadExpectedButMissing;
            }

            if (string.Equals(normalized, "no_session_activity", StringComparison.Ordinal))
            {
                return RouteActivitySaveSnapshotFailureKind.NoSessionActivity;
            }

            if (string.Equals(normalized, "no_activity_content_contributors", StringComparison.Ordinal))
            {
                return RouteActivitySaveSnapshotFailureKind.NoActivityContentContributors;
            }

            if (string.Equals(normalized, "no_route_save_contributors", StringComparison.Ordinal))
            {
                return RouteActivitySaveSnapshotFailureKind.NoRouteSaveContributors;
            }

            if (string.Equals(normalized, "no_session_save_contributors", StringComparison.Ordinal))
            {
                return RouteActivitySaveSnapshotFailureKind.NoSessionSaveContributors;
            }

            if (string.Equals(normalized, "no_save_contributors", StringComparison.Ordinal))
            {
                return RouteActivitySaveSnapshotFailureKind.NoSaveContributors;
            }

            if (string.Equals(normalized, "snapshot_payload_expected_but_missing", StringComparison.Ordinal))
            {
                return RouteActivitySaveSnapshotFailureKind.SnapshotPayloadExpectedButMissing;
            }

            if (string.Equals(normalized, "snapshot_payload_missing", StringComparison.Ordinal))
            {
                return RouteActivitySaveSnapshotFailureKind.SnapshotPayloadExpectedButMissing;
            }

            if (string.Equals(normalized, "snapshot_capture_failed", StringComparison.Ordinal) ||
                normalized.StartsWith("snapshot_capture_failed:", StringComparison.Ordinal))
            {
                return RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed;
            }

            if (string.Equals(normalized, "snapshot_payload_invalid", StringComparison.Ordinal))
            {
                return RouteActivitySaveSnapshotFailureKind.SnapshotPayloadInvalid;
            }

            if (string.Equals(normalized, "no_snapshot_provider", StringComparison.Ordinal))
            {
                return RouteActivitySaveSnapshotFailureKind.NoSessionSaveContributors;
            }

            return RouteActivitySaveSnapshotFailureKind.UnknownFailure;
        }

        private static ProgressionSlotContext ResolveProgressionSlotContextOrFail(
            IProgressionSlotContextResolver resolver,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            bool resolved = resolver.TryResolveForRouteActivitySave(
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason,
                out ProgressionSlotContext slotContext,
                out string failureReason);

            if (!resolved || slotContext == null || !slotContext.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] ProgressionSlotContext obrigatorio ausente/invalido routeIdentity='{Normalize(routeIdentity)}' routeOperationId='{Normalize(routeOperationId)}' transitionId='{Normalize(transitionId)}' routeSequence='{routeSequence}' failureReason='{Normalize(failureReason)}'.");
            }

            return slotContext;
        }

        private static string ResolvePayloadKindFromSerialized(string payload)
        {
            string normalized = Normalize(payload);
            if (normalized.IndexOf("\"canonicalPayload\":\"CapabilitySnapshotEnvelope\"", StringComparison.Ordinal) >= 0 ||
                normalized.IndexOf("\"capabilitySnapshotEnvelope\"", StringComparison.Ordinal) >= 0)
            {
                return "CapabilitySnapshotEnvelope";
            }

            return "Unknown";
        }

        private static string SerializeSnapshotPayload(SessionActivitySnapshotPayload payload)
        {
            return payload.HasCapabilitySnapshotEnvelope
                ? SerializeCapabilitySnapshotEnvelopePayload(payload)
                : string.Empty;
        }

        private static string SerializeCapabilitySnapshotEnvelopePayload(SessionActivitySnapshotPayload payload)
        {
            ActivityCapabilitySnapshotEnvelope envelope = payload.CapabilitySnapshotEnvelope;
            StringBuilder builder = new(1024);
            builder.Append('{');
            AppendJsonField(builder, "schemaId", payload.SchemaId);
            builder.Append(',');
            AppendJsonField(builder, "sessionStateId", payload.SessionStateId);
            builder.Append(',');
            AppendJsonField(builder, "activityId", payload.ActivityId);
            builder.Append(',');
            AppendJsonField(builder, "entrySequence", payload.EntrySequence.ToString(CultureInfo.InvariantCulture), isNumber: true);
            builder.Append(',');
            AppendJsonField(builder, "canonicalPayload", "CapabilitySnapshotEnvelope");
            builder.Append(',');
            builder.Append("\"capabilitySnapshotEnvelope\":{");
            AppendJsonField(builder, "schemaId", envelope.SchemaId);
            builder.Append(',');
            AppendJsonField(builder, "pipelineId", envelope.PipelineId);
            builder.Append(',');
            AppendJsonField(builder, "sessionStateId", envelope.SessionStateId);
            builder.Append(',');
            AppendJsonField(builder, "activityId", envelope.ActivityId);
            builder.Append(',');
            AppendJsonField(builder, "activityOrdinal", envelope.ActivityOrdinal.ToString(CultureInfo.InvariantCulture), isNumber: true);
            builder.Append(',');
            AppendJsonField(builder, "entrySequence", envelope.EntrySequence.ToString(CultureInfo.InvariantCulture), isNumber: true);
            builder.Append(',');
            AppendJsonField(builder, "source", envelope.Source);
            builder.Append(',');
            AppendJsonField(builder, "reason", envelope.Reason);
            builder.Append(',');
            builder.Append("\"records\":[");

            for (int index = 0; index < envelope.Records.Count; index++)
            {
                ActivityCapabilitySnapshotRecord record = envelope.Records[index];
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append('{');
                AppendJsonField(builder, "ownerKind", record.OwnerKind.ToString());
                builder.Append(',');
                AppendJsonField(builder, "ownerId", record.OwnerId);
                builder.Append(',');
                AppendJsonField(builder, "contentProfileId", record.ContentProfileId);
                builder.Append(',');
                AppendJsonField(builder, "capabilityId", record.CapabilityId);
                builder.Append(',');
                AppendJsonField(builder, "capabilityKind", record.CapabilityKind);
                builder.Append(',');
                AppendJsonField(builder, "payloadSchemaId", record.PayloadSchemaId);
                builder.Append(',');
                AppendJsonField(builder, "payloadSchemaVersion", record.PayloadSchemaVersion.ToString(CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "payloadFormat", record.PayloadFormat.ToString());
                builder.Append(',');
                AppendJsonField(builder, "payload", record.Payload);
                builder.Append(',');
                AppendJsonField(builder, "source", record.Source);
                builder.Append(',');
                AppendJsonField(builder, "reason", record.Reason);
                builder.Append('}');
            }

            builder.Append("]}}");
            return builder.ToString();
        }

        private static void AppendJsonField(StringBuilder builder, string key, string value, bool isNumber = false)
        {
            builder.Append('"').Append(key).Append("\":");
            if (isNumber)
            {
                builder.Append(string.IsNullOrWhiteSpace(value) ? "0" : value);
                return;
            }

            builder.Append('"').Append(EscapeJson(value)).Append('"');
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }


        private static string ResolveSnapshotPayloadKind(SessionActivitySnapshotPayload payload)
        {
            if (payload.HasCapabilitySnapshotEnvelope)
            {
                return "CapabilitySnapshotEnvelope";
            }

            return "<none>";
        }

        private static int ResolveSnapshotPayloadRecordCount(SessionActivitySnapshotPayload payload)
        {
            if (payload.HasCapabilitySnapshotEnvelope && payload.CapabilitySnapshotEnvelope.Records != null)
            {
                return payload.CapabilitySnapshotEnvelope.Records.Count;
            }

            return 0;
        }

        private static string ResolveSnapshotPayloadTargetIdsLabel(SessionActivitySnapshotPayload payload)
        {
            if (payload.HasCapabilitySnapshotEnvelope)
            {
                return BuildEnvelopeOwnerIdsLabel(payload.CapabilitySnapshotEnvelope.Records);
            }

            return "<none>";
        }

        private static string BuildEnvelopeOwnerIdsLabel(IReadOnlyList<ActivityCapabilitySnapshotRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                return "<none>";
            }

            HashSet<string> ownerIds = new(StringComparer.Ordinal);
            for (int index = 0; index < records.Count; index++)
            {
                string ownerId = Normalize(records[index].OwnerId);
                if (!string.IsNullOrWhiteSpace(ownerId))
                {
                    ownerIds.Add(ownerId);
                }
            }

            return ownerIds.Count == 0 ? "<none>" : string.Join(",", ownerIds);
        }


        private readonly struct RouteActivitySnapshotPayloadResolution
        {
            public RouteActivitySnapshotPayloadResolution(
                string schemaId,
                string sourceActivityId,
                int sourceEntrySequence,
                int recordCount,
                string targetIds,
                int payloadSize,
                RouteActivitySaveSnapshotFailureKind failureKind,
                string failureReason,
                string contributorResolutionKind)
            {
                SchemaId = Normalize(schemaId);
                SourceActivityId = Normalize(sourceActivityId);
                SourceEntrySequence = sourceEntrySequence < 0 ? 0 : sourceEntrySequence;
                RecordCount = recordCount < 0 ? 0 : recordCount;
                TargetIds = Normalize(targetIds);
                PayloadSize = payloadSize < 0 ? 0 : payloadSize;
                FailureKind = failureKind;
                FailureReason = Normalize(failureReason);
                ContributorResolutionKind = Normalize(contributorResolutionKind);
            }

            public string SchemaId { get; }
            public string SourceActivityId { get; }
            public int SourceEntrySequence { get; }
            public int RecordCount { get; }
            public string TargetIds { get; }
            public int PayloadSize { get; }
            public RouteActivitySaveSnapshotFailureKind FailureKind { get; }
            public string FailureReason { get; }
            public string ContributorResolutionKind { get; }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
