using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public interface IRouteActivityLoadedSnapshotPayloadStore
    {
        void ClearPendingLoadedSnapshotPayload();
        void SetPendingLoadedSnapshotPayload(string activityIdentity, LoadedRouteActivitySnapshotPayload payload, int payloadSize);
    }

    public enum OperationalRouteActivitySaveLoadOnEnterResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalRouteActivitySaveLoadOnEnterCommand
    {
        public OperationalRouteActivitySaveLoadOnEnterCommand(
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

        public bool IsValid =>
            RuntimeModeConfig != null &&
            RouteCommand.IsValid &&
            RouteActivitySavePlan.IsValid;
}

    public readonly struct OperationalRouteActivitySaveLoadOnEnterResult
    {
        public OperationalRouteActivitySaveLoadOnEnterResult(
            OperationalRouteActivitySaveLoadOnEnterResultKind kind,
            string reason,
            string detail,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext = default)
        {
            Kind = kind;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
            LoadedSnapshotPayloadContext = loadedSnapshotPayloadContext;
        }

        public OperationalRouteActivitySaveLoadOnEnterResultKind Kind { get; }
        public string Reason { get; }
        public string Detail { get; }
        public ActivityEntryObjectSnapshotRestorePayloadContext LoadedSnapshotPayloadContext { get; }
        public bool HasLoadedSnapshotPayloadContext => LoadedSnapshotPayloadContext.IsValid;
        public bool IsCompleted => Kind == OperationalRouteActivitySaveLoadOnEnterResultKind.Completed;
}

    public sealed class OperationalRouteActivitySaveLoadOnEnterStage
    {
        private readonly OperationalFactRecorder _factRecorder;
        private readonly ISessionOperationalActivitySaveAdapter _activitySaveAdapter;
        private readonly IProgressionSlotContextResolver _progressionSlotContextResolver;
        private readonly IRouteActivityLoadedSnapshotPayloadStore _loadedSnapshotStore;
        private readonly string _routeActivitySnapshotSchemaId;

        public OperationalRouteActivitySaveLoadOnEnterStage(
            OperationalFactRecorder factRecorder,
            ISessionOperationalActivitySaveAdapter activitySaveAdapter,
            IProgressionSlotContextResolver progressionSlotContextResolver,
            IRouteActivityLoadedSnapshotPayloadStore loadedSnapshotStore,
            string routeActivitySnapshotSchemaId)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
            _activitySaveAdapter = activitySaveAdapter ?? throw new ArgumentNullException(nameof(activitySaveAdapter));
            _progressionSlotContextResolver = progressionSlotContextResolver ?? throw new ArgumentNullException(nameof(progressionSlotContextResolver));
            _loadedSnapshotStore = loadedSnapshotStore ?? throw new ArgumentNullException(nameof(loadedSnapshotStore));
            if (string.IsNullOrWhiteSpace(routeActivitySnapshotSchemaId))
            {
                throw new ArgumentException("routeActivitySnapshotSchemaId is required.", nameof(routeActivitySnapshotSchemaId));
            }

            _routeActivitySnapshotSchemaId = routeActivitySnapshotSchemaId.TrimToEmpty();
        }

        public OperationalRouteActivitySaveLoadOnEnterResult Execute(OperationalRouteActivitySaveLoadOnEnterCommand command)
        {
            if (!command.IsValid)
            {
                return new OperationalRouteActivitySaveLoadOnEnterResult(
                    OperationalRouteActivitySaveLoadOnEnterResultKind.Failed,
                    "invalid_command",
                    "OperationalRouteActivitySaveLoadOnEnterCommand invalido.");
            }

            var routeCommand = command.RouteCommand;
            _factRecorder.TryRecordOperationStage(SessionOperationalStage.RoutePhysicalApplyObserved, command.Source, command.Reason, "route_activity_save_load_on_enter_started");
            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"RouteActivitySaveLoadOnEnterStageStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            var loadedSnapshotPayloadContext = ExecuteLoadOnEnterOrFail(command);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"RouteActivitySaveLoadOnEnterStageCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}' loadedSnapshotPayload='{(loadedSnapshotPayloadContext.HasPayload ? "present" : "absent")}' loadedSnapshotPayloadRecordCount='{(loadedSnapshotPayloadContext.HasPayload ? loadedSnapshotPayloadContext.Payload.RecordCount : 0)}' loadedSnapshotPayloadSourceActivityId='{(loadedSnapshotPayloadContext.HasPayload ? loadedSnapshotPayloadContext.Payload.ActivityId.TrimToEmpty() : "<none>")}' loadedSnapshotPayloadSourceEntrySequence='{(loadedSnapshotPayloadContext.HasPayload ? loadedSnapshotPayloadContext.Payload.SourceEntrySequence : 0)}'.",
                DebugUtility.Colors.Success);

            return new OperationalRouteActivitySaveLoadOnEnterResult(
                OperationalRouteActivitySaveLoadOnEnterResultKind.Completed,
                "completed",
                "route_activity_save_load_on_enter_completed",
                loadedSnapshotPayloadContext);
        }

        private ActivityEntryObjectSnapshotRestorePayloadContext ExecuteLoadOnEnterOrFail(OperationalRouteActivitySaveLoadOnEnterCommand command)
        {
            var loadOnEnterPlan = command.RouteActivitySavePlan.LoadOnEnter;
            string activityIdentity = loadOnEnterPlan.ActivityIdentity;
            string routeIdentity = loadOnEnterPlan.RouteIdentity;
            string routeOperationId = loadOnEnterPlan.RouteOperationId;
            string transitionId = loadOnEnterPlan.TransitionId;
            int routeSequence = loadOnEnterPlan.RouteSequence;

            if (!loadOnEnterPlan.ShouldLoad)
            {
                DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                    $"RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{activityIdentity.TrimToEmpty()}' skipKind='{loadOnEnterPlan.SkipKind}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(loadOnEnterPlan.SkipKind)}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return default;
            }

            _loadedSnapshotStore.ClearPendingLoadedSnapshotPayload();
            var slotContext = ResolveProgressionSlotContextOrFail(
                _progressionSlotContextResolver,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                command.Source,
                command.Reason);

            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"RouteActivitySaveLoadPointerResolved routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{activityIdentity.TrimToEmpty()}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' pointerOwner='ProgressionSlotContextResolver' currentSnapshotRequired='false' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"RouteActivitySaveLoadStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{activityIdentity.TrimToEmpty()}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            var result = _activitySaveAdapter.LoadActivitySaveOnEnter(
                command.RuntimeModeConfig,
                command.RouteCommand,
                slotContext,
                activityIdentity);

            if (result.IsSkipped)
            {
                if (result.FailureKind == RouteActivitySaveSnapshotFailureKind.SnapshotPayloadMissing)
                {
                    DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                        $"checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Waiting' activityIdentity='{activityIdentity.TrimToEmpty()}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' recordCount='0' ownerIds='<none>' payloadSize='0' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' pointerOwner='ProgressionSlotContextResolver' detail='{result.Detail.TrimToEmpty()}'.",
                        DebugUtility.Colors.Info);
                }

                DebugUtility.LogVerbose(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                    $"RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{activityIdentity.TrimToEmpty()}' skipKind='{result.SkipKind}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(result.SkipKind)}' detail='{result.Detail.TrimToEmpty()}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return default;
            }

            if (!result.IsLoaded)
            {
                string message = $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] load-on-enter retornou estado invalido routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.";
                DebugUtility.LogError<OperationalRouteActivitySaveLoadOnEnterStage>(message);
                throw new InvalidOperationException(message);
            }

            var readResult =
                RouteActivitySnapshotPayloadReader.Read(result.ActivitySnapshotPayload, _routeActivitySnapshotSchemaId);
            if (!readResult.Succeeded)
            {
                DebugUtility.LogError(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                    $"checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Failed' activityIdentity='{activityIdentity.TrimToEmpty()}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' recordCount='0' ownerIds='<none>' payloadSize='0' failureKind='{readResult.FailureKind}' failureReason='{readResult.FailureReason.TrimToEmpty()}' detail='{readResult.Detail.TrimToEmpty()}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] payload invalido no load-on-enter routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{activityIdentity.TrimToEmpty()}' failureKind='{readResult.FailureKind}' failureReason='{readResult.FailureReason.TrimToEmpty()}'.");
            }

            var loadedPayload = readResult.Payload;
            _loadedSnapshotStore.SetPendingLoadedSnapshotPayload(
                activityIdentity.TrimToEmpty(),
                loadedPayload,
                result.ActivitySnapshotPayload.Length);
            string loadedOwnerIds = BuildLoadedSnapshotOwnerIds(loadedPayload.CapabilitySnapshotEnvelope.Records);
            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"RouteActivitySnapshotPayloadLoaded activityIdentity='{activityIdentity.TrimToEmpty()}' sourceActivityId='{loadedPayload.ActivityId.TrimToEmpty()}' sourceEntrySequence='{loadedPayload.SourceEntrySequence}' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' ownerIds='{loadedOwnerIds}' schemaId='{loadedPayload.SchemaId.TrimToEmpty()}' payloadSize='{result.ActivitySnapshotPayload.Length}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Passed' activityIdentity='{activityIdentity.TrimToEmpty()}' payloadLoaded='true' sourceActivityId='{loadedPayload.ActivityId.TrimToEmpty()}' sourceEntrySequence='{loadedPayload.SourceEntrySequence}' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' ownerIds='{loadedOwnerIds}' payloadSize='{result.ActivitySnapshotPayload.Length}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"RouteActivitySaveLoadCompleted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{activityIdentity.TrimToEmpty()}' detail='{result.Detail.TrimToEmpty()}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new ActivityEntryObjectSnapshotRestorePayloadContext(loadedPayload);
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

        private static string BuildLoadedSnapshotOwnerIds(IReadOnlyList<ActivityCapabilitySnapshotRecord> records)
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
}
}
