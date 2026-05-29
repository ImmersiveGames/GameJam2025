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
            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveOnExitStageStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            ExecuteSaveOnExitOrFail(command);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveOnExitStageCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new OperationalRouteActivitySaveSaveOnExitResult(
                OperationalRouteActivitySaveSaveOnExitResultKind.Completed,
                "completed",
                "route_activity_save_save_on_exit_completed");
        }

        private void ExecuteSaveOnExitOrFail(OperationalRouteActivitySaveSaveOnExitCommand command)
        {
            RouteActivitySaveOnExitPlan saveOnExitPlan = command.RouteActivitySavePlan.SaveOnExit;
            string currentRouteIdentity = saveOnExitPlan.CurrentRouteIdentity;
            string currentRouteOperationId = saveOnExitPlan.CurrentRouteOperationId;
            string currentTransitionId = saveOnExitPlan.CurrentTransitionId;
            int currentRouteSequence = saveOnExitPlan.CurrentRouteSequence;

            if (!saveOnExitPlan.ShouldSave)
            {
                LogRouteActivitySaveSaveSkipped(
                    saveOnExitPlan,
                    saveOnExitPlan.SkipKind,
                    saveOnExitPlan.SkipDetail,
                    command.Source,
                    command.Reason);
                return;
            }

            string previousActivityIdentity = saveOnExitPlan.PreviousActivityIdentity;
            string previousActivitySaveKey = saveOnExitPlan.PreviousActivitySaveKey;

            if (!TryResolvePreviousActivitySnapshotPayload(command, out string activitySnapshotPayload, out RouteActivitySnapshotPayloadResolution payloadResolution))
            {
                bool isCaptureFailed = payloadResolution.FailureKind == RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed;
                RouteActivitySaveSkipKind skipKind = ResolveSnapshotPayloadSkipReason(payloadResolution.FailureKind);
                string checkpointStatus = isCaptureFailed ? "Failed" : "Waiting";
                DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotPayload' checkpointStatus='{checkpointStatus}' activityIdentity='{Normalize(previousActivityIdentity)}' sourceActivityId='<none>' sourceEntrySequence='0' payloadResolved='false' payloadObjectCount='0' targetIds='<none>' payloadSize='0' failureReason='{Normalize(payloadResolution.FailureReason)}'.",
                    DebugUtility.Colors.Info);
                if (isCaptureFailed)
                {
                    LogRouteActivitySaveCaptureFailed(
                        saveOnExitPlan,
                        skipKind,
                        Normalize(payloadResolution.FailureReason),
                        command.Source,
                        command.Reason);
                }
                else
                {
                    LogRouteActivitySaveSaveSkipped(
                        saveOnExitPlan,
                        skipKind,
                        Normalize(payloadResolution.FailureReason),
                        command.Source,
                        command.Reason);
                }
                return;
            }

            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySnapshotPayloadResolved previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' activityIdentity='{Normalize(previousActivityIdentity)}' sourceActivityId='{Normalize(payloadResolution.SourceActivityId)}' sourceEntrySequence='{payloadResolution.SourceEntrySequence}' payloadObjectCount='{payloadResolution.PayloadObjectCount}' targetIds='{Normalize(payloadResolution.TargetIds)}' schemaId='{Normalize(payloadResolution.SchemaId)}' payloadSize='{payloadResolution.PayloadSize}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotPayload' checkpointStatus='Passed' activityIdentity='{Normalize(previousActivityIdentity)}' sourceActivityId='{Normalize(payloadResolution.SourceActivityId)}' sourceEntrySequence='{payloadResolution.SourceEntrySequence}' payloadResolved='true' payloadObjectCount='{payloadResolution.PayloadObjectCount}' targetIds='{Normalize(payloadResolution.TargetIds)}' payloadSize='{payloadResolution.PayloadSize}'.",
                DebugUtility.Colors.Info);

            if (string.IsNullOrWhiteSpace(activitySnapshotPayload))
            {
                LogRouteActivitySaveSaveSkipped(
                    saveOnExitPlan,
                    RouteActivitySaveSkipKind.NoSnapshotPayload,
                    "snapshot da activity da rota anterior ausente.",
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

            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveCompleted previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' previousActivityIdentity='{Normalize(previousActivityIdentity)}' previousActivitySaveKey='{previousActivitySaveKey}' currentRouteIdentity='{Normalize(currentRouteIdentity)}' currentRouteOperationId='{Normalize(currentRouteOperationId)}' currentTransitionId='{Normalize(currentTransitionId)}' routeSequence='{currentRouteSequence}' detail='{Normalize(saveResult.Detail)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
        }

        private static void LogRouteActivitySaveSaveSkipped(
            RouteActivitySaveOnExitPlan saveOnExitPlan,
            RouteActivitySaveSkipKind skipKind,
            string detail,
            string source,
            string reason)
        {
            DebugUtility.Log(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveSkipped previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' previousActivityIdentity='{Normalize(saveOnExitPlan.PreviousActivityIdentity)}' previousActivitySaveKey='{Normalize(saveOnExitPlan.PreviousActivitySaveKey)}' currentRouteIdentity='{Normalize(saveOnExitPlan.CurrentRouteIdentity)}' currentRouteOperationId='{Normalize(saveOnExitPlan.CurrentRouteOperationId)}' currentTransitionId='{Normalize(saveOnExitPlan.CurrentTransitionId)}' routeSequence='{saveOnExitPlan.CurrentRouteSequence}' skipKind='{skipKind}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(skipKind)}' detail='{Normalize(detail)}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogRouteActivitySaveCaptureFailed(
            RouteActivitySaveOnExitPlan saveOnExitPlan,
            RouteActivitySaveSkipKind failureKind,
            string detail,
            string source,
            string reason)
        {
            DebugUtility.LogError(typeof(OperationalRouteActivitySaveSaveOnExitStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveFailed previousRouteIdentity='{saveOnExitPlan.PreviousRouteIdentity}' previousRouteOperationId='{saveOnExitPlan.PreviousRouteOperationId}' previousRouteSequence='{saveOnExitPlan.PreviousRouteSequence}' previousActivityIdentity='{Normalize(saveOnExitPlan.PreviousActivityIdentity)}' previousActivitySaveKey='{Normalize(saveOnExitPlan.PreviousActivitySaveKey)}' currentRouteIdentity='{Normalize(saveOnExitPlan.CurrentRouteIdentity)}' currentRouteOperationId='{Normalize(saveOnExitPlan.CurrentRouteOperationId)}' currentTransitionId='{Normalize(saveOnExitPlan.CurrentTransitionId)}' routeSequence='{saveOnExitPlan.CurrentRouteSequence}' failureKind='{failureKind}' failureReason='{RouteActivitySaveSkipKindMapper.ToCode(failureKind)}' detail='{Normalize(detail)}' source='{source}' reason='{reason}'.");
        }

        private bool TryResolvePreviousActivitySnapshotPayload(
            OperationalRouteActivitySaveSaveOnExitCommand command,
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
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.NoPreviousRoute,
                    failureReason: "previous_route_invalid");
                return false;
            }

            string sessionStateId = Normalize(saveOnExitPlan.PreviousActivityIdentity);
            if (string.IsNullOrWhiteSpace(sessionStateId))
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.NoCurrentActivity,
                    failureReason: "session_state_id_missing");
                return false;
            }

            ISessionActivitySnapshotPayloadProvider provider = _activitySnapshotPayloadProviderResolver();
            if (provider == null)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.SnapshotProviderUnavailable,
                    failureReason: "no_snapshot_provider");
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
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.SnapshotPayloadMissing,
                    failureReason: string.IsNullOrWhiteSpace(failureReason) ? "no_snapshot_payload" : Normalize(failureReason));
                return false;
            }

            if (!payload.IsValid)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed,
                    failureReason: string.IsNullOrWhiteSpace(failureReason) ? "snapshot_capture_failed" : Normalize(failureReason));
                return false;
            }

            activitySnapshotPayload = SerializeSnapshotPayload(payload);
            if (string.IsNullOrWhiteSpace(activitySnapshotPayload))
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    payload.SchemaId,
                    payload.ActivityId,
                    payload.EntrySequence,
                    payload.Objects.Count,
                    BuildTargetIdsLabel(payload.Objects),
                    0,
                    RouteActivitySaveSnapshotFailureKind.SnapshotPayloadInvalid,
                    "snapshot_payload_serialization_failed");
                return false;
            }

            resolution = new RouteActivitySnapshotPayloadResolution(
                payload.SchemaId,
                payload.ActivityId,
                payload.EntrySequence,
                payload.Objects.Count,
                BuildTargetIdsLabel(payload.Objects),
                activitySnapshotPayload.Length,
                RouteActivitySaveSnapshotFailureKind.None,
                "resolved");
            return true;
        }

        private static RouteActivitySaveSkipKind ResolveSnapshotPayloadSkipReason(RouteActivitySaveSnapshotFailureKind failureKind)
        {
            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotProviderUnavailable)
            {
                return RouteActivitySaveSkipKind.NoSnapshotProvider;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed)
            {
                return RouteActivitySaveSkipKind.SnapshotCaptureFailed;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotPayloadInvalid)
            {
                return RouteActivitySaveSkipKind.SnapshotPayloadInvalid;
            }

            return RouteActivitySaveSkipKind.NoSnapshotPayload;
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

        private static string SerializeSnapshotPayload(SessionActivitySnapshotPayload payload)
        {
            StringBuilder builder = new(512);
            builder.Append('{');
            AppendJsonField(builder, "schemaId", payload.SchemaId);
            builder.Append(',');
            AppendJsonField(builder, "sessionStateId", payload.SessionStateId);
            builder.Append(',');
            AppendJsonField(builder, "activityId", payload.ActivityId);
            builder.Append(',');
            AppendJsonField(builder, "entrySequence", payload.EntrySequence.ToString(CultureInfo.InvariantCulture), isNumber: true);
            builder.Append(',');
            builder.Append("\"objects\":[");

            for (int index = 0; index < payload.Objects.Count; index++)
            {
                SessionActivitySnapshotPayloadObject obj = payload.Objects[index];
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append('{');
                AppendJsonField(builder, "targetId", obj.TargetId);
                builder.Append(',');
                builder.Append("\"position\":{");
                AppendJsonField(builder, "x", obj.PositionX.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "y", obj.PositionY.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "z", obj.PositionZ.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append("},");
                builder.Append("\"rotation\":{");
                AppendJsonField(builder, "x", obj.RotationX.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "y", obj.RotationY.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "z", obj.RotationZ.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "w", obj.RotationW.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append("},");
                builder.Append("\"scale\":{");
                AppendJsonField(builder, "x", obj.ScaleX.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "y", obj.ScaleY.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "z", obj.ScaleZ.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append("}");
                builder.Append('}');
            }

            builder.Append("]}");
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

        private static string BuildTargetIdsLabel(IReadOnlyList<SessionActivitySnapshotPayloadObject> objects)
        {
            if (objects == null || objects.Count == 0)
            {
                return "<none>";
            }

            HashSet<string> targetIds = new(StringComparer.Ordinal);
            for (int index = 0; index < objects.Count; index++)
            {
                string targetId = Normalize(objects[index].TargetId);
                if (!string.IsNullOrWhiteSpace(targetId))
                {
                    targetIds.Add(targetId);
                }
            }

            if (targetIds.Count == 0)
            {
                return "<none>";
            }

            return string.Join(",", targetIds);
        }

        private readonly struct RouteActivitySnapshotPayloadResolution
        {
            public RouteActivitySnapshotPayloadResolution(
                string schemaId,
                string sourceActivityId,
                int sourceEntrySequence,
                int payloadObjectCount,
                string targetIds,
                int payloadSize,
                RouteActivitySaveSnapshotFailureKind failureKind,
                string failureReason)
            {
                SchemaId = Normalize(schemaId);
                SourceActivityId = Normalize(sourceActivityId);
                SourceEntrySequence = sourceEntrySequence < 0 ? 0 : sourceEntrySequence;
                PayloadObjectCount = payloadObjectCount < 0 ? 0 : payloadObjectCount;
                TargetIds = Normalize(targetIds);
                PayloadSize = payloadSize < 0 ? 0 : payloadSize;
                FailureKind = failureKind;
                FailureReason = Normalize(failureReason);
            }

            public string SchemaId { get; }
            public string SourceActivityId { get; }
            public int SourceEntrySequence { get; }
            public int PayloadObjectCount { get; }
            public string TargetIds { get; }
            public int PayloadSize { get; }
            public RouteActivitySaveSnapshotFailureKind FailureKind { get; }
            public string FailureReason { get; }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
