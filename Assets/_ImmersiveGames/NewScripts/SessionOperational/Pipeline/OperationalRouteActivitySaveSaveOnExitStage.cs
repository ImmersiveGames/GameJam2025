using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteActivitySaveSaveOnExitResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2
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
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
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
    }

    public readonly struct OperationalRouteActivitySaveSaveOnExitResult
    {
        public OperationalRouteActivitySaveSaveOnExitResult(
            OperationalRouteActivitySaveSaveOnExitResultKind kind,
            string reason,
            string detail)
        {
            Kind = kind;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public OperationalRouteActivitySaveSaveOnExitResultKind Kind { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalRouteActivitySaveSaveOnExitResultKind.Completed;
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
            SessionStateId = sessionStateId.TrimToEmpty();
            SaveOwnerActivityIdentity = sessionStateId.TrimToEmpty();
            RequestedActivityIdentity = requestedActivityIdentity.TrimToEmpty();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
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
    }

    public sealed class OperationalRouteActivitySaveSaveOnExitStage
    {
        private readonly OperationalFactRecorder _factRecorder;
        private readonly ISessionOperationalActivitySaveAdapter _activitySaveAdapter;
        private readonly IProgressionSlotContextResolver _progressionSlotContextResolver;
        private readonly Func<ISessionActivitySnapshotPayloadProvider> _activitySnapshotPayloadProviderResolver;
        private readonly string _routeActivitySnapshotSchemaId;

        public OperationalRouteActivitySaveSaveOnExitStage(
            OperationalFactRecorder factRecorder,
            ISessionOperationalActivitySaveAdapter activitySaveAdapter,
            IProgressionSlotContextResolver progressionSlotContextResolver,
            Func<ISessionActivitySnapshotPayloadProvider> activitySnapshotPayloadProviderResolver,
            string routeActivitySnapshotSchemaId)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
            _activitySaveAdapter = activitySaveAdapter ?? throw new ArgumentNullException(nameof(activitySaveAdapter));
            _progressionSlotContextResolver = progressionSlotContextResolver ?? throw new ArgumentNullException(nameof(progressionSlotContextResolver));
            _activitySnapshotPayloadProviderResolver = activitySnapshotPayloadProviderResolver ?? throw new ArgumentNullException(nameof(activitySnapshotPayloadProviderResolver));
            if (string.IsNullOrWhiteSpace(routeActivitySnapshotSchemaId))
            {
                throw new ArgumentException("routeActivitySnapshotSchemaId is required.", nameof(routeActivitySnapshotSchemaId));
            }

            _routeActivitySnapshotSchemaId = routeActivitySnapshotSchemaId.TrimToEmpty();
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

            var routeCommand = command.RouteCommand;
            var saveOnExitPlan = command.RouteActivitySavePlan.SaveOnExit;
            var contributorScopePolicy = saveOnExitPlan.PreviousRouteContributorScopePolicy;
            _factRecorder.TryRecordOperationStage(SessionOperationalStage.RoutePhysicalApplyObserved, command.Source, command.Reason, "route_activity_save_save_on_exit_started");
            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"RouteActivitySaveSaveOnExitStageStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' contributorScopePolicy='{contributorScopePolicy}' saveOnExitSkipKind='{saveOnExitPlan.SkipKind}' saveOnExitSkipReason='{RouteActivitySaveSkipKindMapper.ToCode(saveOnExitPlan.SkipKind)}' saveOnExitSkipDetail='{saveOnExitPlan.SkipDetail.TrimToEmpty()}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            ExecuteSaveOnExitOrFail(command);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"RouteActivitySaveSaveOnExitStageCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' contributorScopePolicy='{contributorScopePolicy}' saveOnExitSkipKind='{saveOnExitPlan.SkipKind}' saveOnExitSkipReason='{RouteActivitySaveSkipKindMapper.ToCode(saveOnExitPlan.SkipKind)}' saveOnExitSkipDetail='{saveOnExitPlan.SkipDetail.TrimToEmpty()}' source='{command.Source}' reason='{command.Reason}'.",
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

            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"RouteActivitySaveQaSaveStarted sessionStateId='{command.SessionStateId.TrimToEmpty()}' saveOwnerActivityIdentity='{command.SaveOwnerActivityIdentity.TrimToEmpty()}' requestedActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            var provider = _activitySnapshotPayloadProviderResolver();
            if (provider == null)
            {
                DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"checkpoint='RouteActivitySaveQaSave' checkpointStatus='Failed' activityIdentity='{command.SaveOwnerActivityIdentity.TrimToEmpty()}' saveOwnerActivityIdentity='{command.SaveOwnerActivityIdentity.TrimToEmpty()}' payloadActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' requestedActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' payloadResolved='false' payloadKind='<none>' recordCount='0' contributorResolutionKind='no_session_save_contributors' failureReason='no_session_save_contributors' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    "no_session_save_contributors",
                    "ISessionActivitySnapshotPayloadProvider ausente para QA save.");
            }

            if (!provider.TryGetSnapshotPayloadForSaveOnExit(
                    command.SessionStateId,
                    out var payload,
                    out string failureReason) ||
                !payload.IsValid)
            {
                string normalizedFailure = failureReason.TrimToEmpty();
                if (string.IsNullOrWhiteSpace(normalizedFailure))
                {
                    normalizedFailure = "snapshot_payload_expected_but_missing";
                }

                if (string.Equals(normalizedFailure, "no_activity_content_contributors", StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                        $"checkpoint='RouteActivitySaveQaSave' checkpointStatus='SkippedNoContent' activityIdentity='{command.SaveOwnerActivityIdentity.TrimToEmpty()}' saveOwnerActivityIdentity='{command.SaveOwnerActivityIdentity.TrimToEmpty()}' payloadActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' requestedActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' payloadResolved='false' payloadKind='<none>' recordCount='0' contributorResolutionKind='{normalizedFailure}' skipReason='{normalizedFailure}' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Info);
                    return new OperationalRouteActivitySaveSaveOnExitResult(
                        OperationalRouteActivitySaveSaveOnExitResultKind.Completed,
                        "qa_save_skipped_no_content",
                        "snapshot payload skipped because current activity has no content contributors.");
                }

                DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"checkpoint='RouteActivitySaveQaSave' checkpointStatus='Failed' activityIdentity='{command.SaveOwnerActivityIdentity.TrimToEmpty()}' saveOwnerActivityIdentity='{command.SaveOwnerActivityIdentity.TrimToEmpty()}' payloadActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' requestedActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' payloadResolved='false' payloadKind='<none>' recordCount='0' contributorResolutionKind='{normalizedFailure}' failureReason='{normalizedFailure}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    normalizedFailure,
                    "snapshot payload ausente/invalido para QA save.");
            }

            string actualActivityIdentity = payload.ActivityId.TrimToEmpty();
            string saveOwnerActivityIdentity = command.SaveOwnerActivityIdentity.TrimToEmpty();
            if (string.IsNullOrWhiteSpace(saveOwnerActivityIdentity))
            {
                saveOwnerActivityIdentity = command.SessionStateId.TrimToEmpty();
            }

            if (!string.IsNullOrWhiteSpace(command.RequestedActivityIdentity) &&
                !string.Equals(command.RequestedActivityIdentity, actualActivityIdentity, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"checkpoint='RouteActivitySaveQaSave' checkpointStatus='Failed' activityIdentity='{saveOwnerActivityIdentity}' saveOwnerActivityIdentity='{saveOwnerActivityIdentity}' payloadActivityIdentity='{actualActivityIdentity}' requestedActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' payloadResolved='true' payloadKind='{ResolveSnapshotPayloadKind(payload)}' recordCount='{ResolveSnapshotPayloadRecordCount(payload)}' contributorResolutionKind='snapshot_identity_mismatch' failureReason='snapshot_identity_mismatch' sourceActivityId='{actualActivityIdentity}' sourceEntrySequence='{payload.EntrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    "snapshot_identity_mismatch",
                    "requested activity identity differs from captured payload activity identity.");
            }

            string serializedPayload = SerializeSnapshotPayload(payload);
            if (string.IsNullOrWhiteSpace(serializedPayload))
            {
                DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"checkpoint='RouteActivitySaveQaSave' checkpointStatus='Failed' activityIdentity='{saveOwnerActivityIdentity}' saveOwnerActivityIdentity='{saveOwnerActivityIdentity}' payloadActivityIdentity='{actualActivityIdentity}' requestedActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' payloadResolved='true' payloadKind='{ResolveSnapshotPayloadKind(payload)}' recordCount='{ResolveSnapshotPayloadRecordCount(payload)}' contributorResolutionKind='snapshot_payload_serialization_failed' failureReason='snapshot_payload_serialization_failed' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    "snapshot_payload_serialization_failed",
                    "snapshot payload serialization failed for QA save.");
            }

            var slotContext = ResolveProgressionSlotContextOrFail(
                _progressionSlotContextResolver,
                qaRouteIdentity,
                qaRouteOperationId,
                qaTransitionId,
                qaRouteSequence,
                command.Source,
                command.Reason);

            var saveResult = _activitySaveAdapter.SaveActivityOnExit(
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
                DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"checkpoint='RouteActivitySaveQaSave' checkpointStatus='Failed' activityIdentity='{saveOwnerActivityIdentity}' saveOwnerActivityIdentity='{saveOwnerActivityIdentity}' payloadActivityIdentity='{actualActivityIdentity}' requestedActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' payloadResolved='true' payloadKind='{payloadKind}' recordCount='{recordCount}' targetIds='{targetIds.TrimToEmpty()}' contributorResolutionKind='{contributorResolutionKind}' failureReason='{saveResult.Detail.TrimToEmpty()}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Warning);
                return new OperationalRouteActivitySaveSaveOnExitResult(
                    OperationalRouteActivitySaveSaveOnExitResultKind.Failed,
                    string.IsNullOrWhiteSpace(saveResult.Detail) ? "save_not_completed" : saveResult.Detail.TrimToEmpty(),
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

            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"RouteActivitySaveQaSaveCompleted activityIdentity='{saveOwnerActivityIdentity}' saveOwnerActivityIdentity='{saveOwnerActivityIdentity}' payloadActivityIdentity='{actualActivityIdentity}' requestedActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' payloadKind='{payloadKind}' recordCount='{recordCount}' targetIds='{targetIds.TrimToEmpty()}' contributorResolutionKind='{contributorResolutionKind}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' detail='{saveResult.Detail.TrimToEmpty()}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"checkpoint='RouteActivitySaveQaSave' checkpointStatus='Passed' activityIdentity='{saveOwnerActivityIdentity}' saveOwnerActivityIdentity='{saveOwnerActivityIdentity}' payloadActivityIdentity='{actualActivityIdentity}' requestedActivityIdentity='{command.RequestedActivityIdentity.TrimToEmpty()}' payloadResolved='true' payloadKind='{payloadKind}' recordCount='{recordCount}' targetIds='{targetIds.TrimToEmpty()}' contributorResolutionKind='{contributorResolutionKind}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' payloadSize='{serializedPayload.Length}' detail='{saveResult.Detail.TrimToEmpty()}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new OperationalRouteActivitySaveSaveOnExitResult(
                OperationalRouteActivitySaveSaveOnExitResultKind.Completed,
                "qa_save_completed",
                saveResult.Detail);
        }

        private void ExecuteSaveOnExitOrFail(OperationalRouteActivitySaveSaveOnExitCommand command)
        {
            var saveOnExitPlan = command.RouteActivitySavePlan.SaveOnExit;
            var contributorScopePolicy = saveOnExitPlan.PreviousRouteContributorScopePolicy;
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

            if (!TryResolvePreviousActivitySnapshotPayload(command, contributorScopePolicy, out string activitySnapshotPayload, out var payloadResolution))
            {
                bool isCaptureFailed = payloadResolution.FailureKind == RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed;
                bool isNoContent = payloadResolution.FailureKind == RouteActivitySaveSnapshotFailureKind.NoActivityContentContributors;
                var skipKind = ResolveSnapshotPayloadSkipReason(payloadResolution.FailureKind);
                string checkpointStatus = isCaptureFailed ? "Failed" : isNoContent ? "SkippedNoContent" : "Waiting";
                DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"checkpoint='RouteActivitySaveSnapshotPayload' checkpointStatus='{checkpointStatus}' activityIdentity='{previousActivityIdentity.TrimToEmpty()}' contributorScopePolicy='{contributorScopePolicy}' contributorResolutionKind='{payloadResolution.ContributorResolutionKind.TrimToEmpty()}' sourceActivityId='<none>' sourceEntrySequence='0' payloadResolved='false' recordCount='0' payloadKind='<none>' targetIds='<none>' payloadSize='0' failureReason='{payloadResolution.FailureReason.TrimToEmpty()}'.",
                    DebugUtility.Colors.Info);
                if (isCaptureFailed)
                {
                    LogRouteActivitySaveCaptureFailed(
                        saveOnExitPlan,
                        contributorScopePolicy,
                        payloadResolution.ContributorResolutionKind,
                        skipKind,
                        payloadResolution.FailureReason.TrimToEmpty(),
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
                        payloadResolution.FailureReason.TrimToEmpty(),
                        command.Source,
                        command.Reason);
                }
                return;
            }

            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"RouteActivitySnapshotPayloadResolved previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' activityIdentity='{previousActivityIdentity.TrimToEmpty()}' contributorScopePolicy='{contributorScopePolicy}' contributorResolutionKind='{payloadResolution.ContributorResolutionKind.TrimToEmpty()}' sourceActivityId='{payloadResolution.SourceActivityId.TrimToEmpty()}' sourceEntrySequence='{payloadResolution.SourceEntrySequence}' recordCount='{payloadResolution.RecordCount}' payloadKind='{ResolvePayloadKindFromSerialized(activitySnapshotPayload)}' targetIds='{payloadResolution.TargetIds.TrimToEmpty()}' schemaId='{payloadResolution.SchemaId.TrimToEmpty()}' payloadSize='{payloadResolution.PayloadSize}' payloadResolutionReason='snapshot_payload_resolved'.",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"checkpoint='RouteActivitySaveSnapshotPayload' checkpointStatus='Passed' activityIdentity='{previousActivityIdentity.TrimToEmpty()}' contributorScopePolicy='{contributorScopePolicy}' contributorResolutionKind='{payloadResolution.ContributorResolutionKind.TrimToEmpty()}' sourceActivityId='{payloadResolution.SourceActivityId.TrimToEmpty()}' sourceEntrySequence='{payloadResolution.SourceEntrySequence}' payloadResolved='true' recordCount='{payloadResolution.RecordCount}' payloadKind='{ResolvePayloadKindFromSerialized(activitySnapshotPayload)}' targetIds='{payloadResolution.TargetIds.TrimToEmpty()}' payloadSize='{payloadResolution.PayloadSize}'.",
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

            var slotContext = ResolveProgressionSlotContextOrFail(
                _progressionSlotContextResolver,
                currentRouteIdentity,
                currentRouteOperationId,
                currentTransitionId,
                currentRouteSequence,
                command.Source,
                command.Reason);

            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"RouteActivitySaveSaveStarted previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' previousActivityIdentity='{previousActivityIdentity.TrimToEmpty()}' previousActivitySaveKey='{previousActivitySaveKey}' currentRouteIdentity='{currentRouteIdentity.TrimToEmpty()}' currentRouteOperationId='{currentRouteOperationId.TrimToEmpty()}' currentTransitionId='{currentTransitionId.TrimToEmpty()}' routeSequence='{currentRouteSequence}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            var saveResult = _activitySaveAdapter.SaveActivityOnExit(
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
                $"RouteActivitySaveSaveCompleted previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' previousActivityIdentity='{previousActivityIdentity.TrimToEmpty()}' previousActivitySaveKey='{previousActivitySaveKey}' currentRouteIdentity='{currentRouteIdentity.TrimToEmpty()}' currentRouteOperationId='{currentRouteOperationId.TrimToEmpty()}' currentTransitionId='{currentTransitionId.TrimToEmpty()}' routeSequence='{currentRouteSequence}' detail='{saveResult.Detail.TrimToEmpty()}' source='{command.Source}' reason='{command.Reason}'.",
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
            var observedSlotContext = ResolveProgressionSlotContextOrFail(
                _progressionSlotContextResolver,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            string expectedSnapshotId = expectedSlotContext is { SnapshotId: { IsValid: true } }
                ? expectedSlotContext.SnapshotId.Value
                : string.Empty;
            string observedSnapshotId = observedSlotContext is { SnapshotId: { IsValid: true } }
                ? observedSlotContext.SnapshotId.Value
                : string.Empty;
            string expectedSlotId = expectedSlotContext is { SlotId: { IsValid: true } }
                ? expectedSlotContext.SlotId.Value
                : string.Empty;
            string observedSlotId = observedSlotContext is { SlotId: { IsValid: true } }
                ? observedSlotContext.SlotId.Value
                : string.Empty;
            bool snapshotIdMatches = !string.IsNullOrWhiteSpace(expectedSnapshotId) &&
                string.Equals(expectedSnapshotId, observedSnapshotId, StringComparison.Ordinal);

            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"RouteActivitySaveCurrentSnapshotPointerObserved operationKind='{operationKind.TrimToEmpty()}' activityIdentity='{activityIdentity.TrimToEmpty()}' routeIdentity='{routeIdentity.TrimToEmpty()}' routeOperationId='{routeOperationId.TrimToEmpty()}' transitionId='{transitionId.TrimToEmpty()}' routeSequence='{routeSequence}' expectedSlotId='{expectedSlotId.TrimToEmpty()}' observedSlotId='{observedSlotId.TrimToEmpty()}' expectedSnapshotId='{expectedSnapshotId.TrimToEmpty()}' observedSnapshotId='{observedSnapshotId.TrimToEmpty()}' snapshotIdMatches='{snapshotIdMatches.ToString().ToLowerInvariant()}' payloadKind='{payloadKind.TrimToEmpty()}' recordCount='{recordCount}' pointerOwner='ProgressionSlotContextResolver' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
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
            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"RouteActivitySaveSaveSkipped previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' previousActivityIdentity='{saveOnExitPlan.PreviousActivityIdentity.TrimToEmpty()}' previousActivitySaveKey='{saveOnExitPlan.PreviousActivitySaveKey.TrimToEmpty()}' contributorScopePolicy='{contributorScopePolicy}' contributorResolutionKind='{contributorResolutionKind.TrimToEmpty()}' currentRouteIdentity='{saveOnExitPlan.CurrentRouteIdentity.TrimToEmpty()}' currentRouteOperationId='{saveOnExitPlan.CurrentRouteOperationId.TrimToEmpty()}' currentTransitionId='{saveOnExitPlan.CurrentTransitionId.TrimToEmpty()}' routeSequence='{saveOnExitPlan.CurrentRouteSequence}' skipKind='{skipKind}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(skipKind)}' detail='{detail.TrimToEmpty()}' source='{source}' reason='{reason}'.",
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
                $"RouteActivitySaveSaveFailed previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' previousActivityIdentity='{saveOnExitPlan.PreviousActivityIdentity.TrimToEmpty()}' previousActivitySaveKey='{saveOnExitPlan.PreviousActivitySaveKey.TrimToEmpty()}' contributorScopePolicy='{contributorScopePolicy}' contributorResolutionKind='{contributorResolutionKind.TrimToEmpty()}' currentRouteIdentity='{saveOnExitPlan.CurrentRouteIdentity.TrimToEmpty()}' currentRouteOperationId='{saveOnExitPlan.CurrentRouteOperationId.TrimToEmpty()}' currentTransitionId='{saveOnExitPlan.CurrentTransitionId.TrimToEmpty()}' routeSequence='{saveOnExitPlan.CurrentRouteSequence}' failureKind='{failureKind}' failureReason='{RouteActivitySaveSkipKindMapper.ToCode(failureKind)}' detail='{detail.TrimToEmpty()}' source='{source}' reason='{reason}'.");
        }

        private bool TryResolvePreviousActivitySnapshotPayload(
            OperationalRouteActivitySaveSaveOnExitCommand command,
            RouteActivitySaveContributorScopePolicy contributorScopePolicy,
            out string activitySnapshotPayload,
            out RouteActivitySnapshotPayloadResolution resolution)
        {
            activitySnapshotPayload = string.Empty;
            resolution = default;
            var saveOnExitPlan = command.RouteActivitySavePlan.SaveOnExit;

            if (!saveOnExitPlan.HasPreviousRoute)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    string.Empty,
                    0,
                    RouteActivitySaveSnapshotFailureKind.NoPreviousRoute,
                    "no_previous_route",
                    "no_previous_route");
                return false;
            }

            string sessionStateId = saveOnExitPlan.PreviousActivityIdentity.TrimToEmpty();
            if (string.IsNullOrWhiteSpace(sessionStateId))
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    string.Empty,
                    0,
                    RouteActivitySaveSnapshotFailureKind.NoSessionActivity,
                    "no_session_activity",
                    "no_session_activity");
                return false;
            }

            if (contributorScopePolicy == RouteActivitySaveContributorScopePolicy.CurrentRouteSaveContributors)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    string.Empty,
                    0,
                    RouteActivitySaveSnapshotFailureKind.NoRouteSaveContributors,
                    "no_route_save_contributors",
                    "no_route_save_contributors");
                return false;
            }

            var provider = _activitySnapshotPayloadProviderResolver();
            if (provider == null)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    string.Empty,
                    0,
                    RouteActivitySaveSnapshotFailureKind.NoSessionSaveContributors,
                    "no_session_save_contributors",
                    "no_session_save_contributors");
                return false;
            }

            bool resolved = provider.TryGetSnapshotPayloadForSaveOnExit(
                sessionStateId,
                out var payload,
                out string failureReason);

            if (!resolved)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    string.Empty,
                    0,
                    ResolveFailureKindFromFailureReason(failureReason),
                    failureReason.TrimToEmpty(),
                    failureReason.TrimToEmpty());
                return false;
            }

            if (!payload.IsValid)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    string.Empty,
                    0,
                    RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed,
                    string.IsNullOrWhiteSpace(failureReason) ? "snapshot_capture_failed" : failureReason.TrimToEmpty(),
                    "snapshot_capture_failed");
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
            string normalized = failureReason.TrimToEmpty();
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

            if (string.Equals(normalized, "exit_correlation_snapshot_payload_expected_but_missing", StringComparison.Ordinal))
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
                out var slotContext,
                out string failureReason);

            if (!resolved || slotContext == null || !slotContext.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] ProgressionSlotContext obrigatorio ausente/invalido routeIdentity='{routeIdentity.TrimToEmpty()}' routeOperationId='{routeOperationId.TrimToEmpty()}' transitionId='{transitionId.TrimToEmpty()}' routeSequence='{routeSequence}' failureReason='{failureReason.TrimToEmpty()}'.");
            }

            return slotContext;
        }

        private static string ResolvePayloadKindFromSerialized(string payload)
        {
            string normalized = payload.TrimToEmpty();
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
            var envelope = payload.CapabilitySnapshotEnvelope;
            StringBuilder builder = new(1024);
            builder.Append('{');
            AppendJsonField(builder, "schemaId", payload.SchemaId);
            builder.Append(',');
            AppendJsonField(builder, "sessionStateId", payload.SessionStateId);
            builder.Append(',');
            AppendJsonField(builder, "activityId", payload.ActivityId);
            builder.Append(',');
            AppendJsonField(builder, "entrySequence", payload.EntrySequence.ToString(CultureInfo.InvariantCulture), true);
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
            AppendJsonField(builder, "activityOrdinal", envelope.ActivityOrdinal.ToString(CultureInfo.InvariantCulture), true);
            builder.Append(',');
            AppendJsonField(builder, "entrySequence", envelope.EntrySequence.ToString(CultureInfo.InvariantCulture), true);
            builder.Append(',');
            AppendJsonField(builder, "source", envelope.Source);
            builder.Append(',');
            AppendJsonField(builder, "reason", envelope.Reason);
            builder.Append(',');
            builder.Append("\"records\":[");

            for (int index = 0; index < envelope.Records.Count; index++)
            {
                var record = envelope.Records[index];
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
                AppendJsonField(builder, "payloadSchemaVersion", record.PayloadSchemaVersion.ToString(CultureInfo.InvariantCulture), true);
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
            if (payload is { HasCapabilitySnapshotEnvelope: true, CapabilitySnapshotEnvelope: { Records: not null } })
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
                string ownerId = records[index].OwnerId.TrimToEmpty();
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
                SchemaId = schemaId.TrimToEmpty();
                SourceActivityId = sourceActivityId.TrimToEmpty();
                SourceEntrySequence = sourceEntrySequence < 0 ? 0 : sourceEntrySequence;
                RecordCount = recordCount < 0 ? 0 : recordCount;
                TargetIds = targetIds.TrimToEmpty();
                PayloadSize = payloadSize < 0 ? 0 : payloadSize;
                FailureKind = failureKind;
                FailureReason = failureReason.TrimToEmpty();
                ContributorResolutionKind = contributorResolutionKind.TrimToEmpty();
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
    }
}
