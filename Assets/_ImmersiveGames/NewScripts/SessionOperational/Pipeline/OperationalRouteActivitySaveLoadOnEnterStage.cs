using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

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
            Source = Normalize(source);
            Reason = Normalize(reason);
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
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
            Reason = Normalize(reason);
            Detail = Normalize(detail);
            LoadedSnapshotPayloadContext = loadedSnapshotPayloadContext;
        }

        public OperationalRouteActivitySaveLoadOnEnterResultKind Kind { get; }
        public string Reason { get; }
        public string Detail { get; }
        public ActivityEntryObjectSnapshotRestorePayloadContext LoadedSnapshotPayloadContext { get; }
        public bool HasLoadedSnapshotPayloadContext => LoadedSnapshotPayloadContext.IsValid;
        public bool IsCompleted => Kind == OperationalRouteActivitySaveLoadOnEnterResultKind.Completed;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalRouteActivitySaveLoadOnEnterStage
    {
        private readonly ISessionOperationalActivitySaveAdapter _activitySaveAdapter;
        private readonly IProgressionSlotContextResolver _progressionSlotContextResolver;
        private readonly IRouteActivityLoadedSnapshotPayloadStore _loadedSnapshotStore;
        private readonly string _routeActivitySnapshotSchemaId;

        public OperationalRouteActivitySaveLoadOnEnterStage(
            ISessionOperationalActivitySaveAdapter activitySaveAdapter,
            IProgressionSlotContextResolver progressionSlotContextResolver,
            IRouteActivityLoadedSnapshotPayloadStore loadedSnapshotStore,
            string routeActivitySnapshotSchemaId)
        {
            _activitySaveAdapter = activitySaveAdapter ?? throw new ArgumentNullException(nameof(activitySaveAdapter));
            _progressionSlotContextResolver = progressionSlotContextResolver ?? throw new ArgumentNullException(nameof(progressionSlotContextResolver));
            _loadedSnapshotStore = loadedSnapshotStore ?? throw new ArgumentNullException(nameof(loadedSnapshotStore));
            _routeActivitySnapshotSchemaId = string.IsNullOrWhiteSpace(routeActivitySnapshotSchemaId)
                ? throw new ArgumentException("routeActivitySnapshotSchemaId is required.", nameof(routeActivitySnapshotSchemaId))
                : routeActivitySnapshotSchemaId.Trim();
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
            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadOnEnterStageStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            var loadedSnapshotPayloadContext = ExecuteLoadOnEnterOrFail(command);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadOnEnterStageCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}' loadedSnapshotPayload='{(loadedSnapshotPayloadContext.HasPayload ? "present" : "absent")}' loadedSnapshotPayloadRecordCount='{(loadedSnapshotPayloadContext.HasPayload ? loadedSnapshotPayloadContext.Payload.RecordCount : 0)}' loadedSnapshotPayloadSourceActivityId='{(loadedSnapshotPayloadContext.HasPayload ? Normalize(loadedSnapshotPayloadContext.Payload.ActivityId) : "<none>")}' loadedSnapshotPayloadSourceEntrySequence='{(loadedSnapshotPayloadContext.HasPayload ? loadedSnapshotPayloadContext.Payload.SourceEntrySequence : 0)}'.",
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
                DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                    $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' skipKind='{loadOnEnterPlan.SkipKind}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(loadOnEnterPlan.SkipKind)}' source='{command.Source}' reason='{command.Reason}'.",
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

            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadPointerResolved routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' pointerOwner='ProgressionSlotContextResolver' currentSnapshotRequired='false' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' source='{command.Source}' reason='{command.Reason}'.",
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
                    DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                        $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Waiting' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' recordCount='0' ownerIds='<none>' payloadSize='0' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' pointerOwner='ProgressionSlotContextResolver' detail='{Normalize(result.Detail)}'.",
                        DebugUtility.Colors.Info);
                }

                DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                    $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' skipKind='{result.SkipKind}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(result.SkipKind)}' detail='{Normalize(result.Detail)}' source='{command.Source}' reason='{command.Reason}'.",
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
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Failed' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' recordCount='0' ownerIds='<none>' payloadSize='0' failureKind='{readResult.FailureKind}' failureReason='{Normalize(readResult.FailureReason)}' detail='{Normalize(readResult.Detail)}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] payload invalido no load-on-enter routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' failureKind='{readResult.FailureKind}' failureReason='{Normalize(readResult.FailureReason)}'.");
            }

            var loadedPayload = readResult.Payload;
            _loadedSnapshotStore.SetPendingLoadedSnapshotPayload(
                Normalize(activityIdentity),
                loadedPayload,
                result.ActivitySnapshotPayload.Length);
            string loadedOwnerIds = BuildLoadedSnapshotOwnerIds(loadedPayload.CapabilitySnapshotEnvelope.Records);
            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySnapshotPayloadLoaded activityIdentity='{Normalize(activityIdentity)}' sourceActivityId='{Normalize(loadedPayload.ActivityId)}' sourceEntrySequence='{loadedPayload.SourceEntrySequence}' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' ownerIds='{loadedOwnerIds}' schemaId='{Normalize(loadedPayload.SchemaId)}' payloadSize='{result.ActivitySnapshotPayload.Length}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Passed' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='true' sourceActivityId='{Normalize(loadedPayload.ActivityId)}' sourceEntrySequence='{loadedPayload.SourceEntrySequence}' payloadKind='{loadedPayload.PayloadKind}' canonicalPayload='CapabilitySnapshotEnvelope' recordCount='{loadedPayload.RecordCount}' ownerIds='{loadedOwnerIds}' payloadSize='{result.ActivitySnapshotPayload.Length}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadCompleted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' detail='{Normalize(result.Detail)}' source='{command.Source}' reason='{command.Reason}'.",
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
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] ProgressionSlotContext obrigatorio ausente/invalido routeIdentity='{Normalize(routeIdentity)}' routeOperationId='{Normalize(routeOperationId)}' transitionId='{Normalize(transitionId)}' routeSequence='{routeSequence}' failureReason='{Normalize(failureReason)}'.");
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
                string ownerId = Normalize(records[index].OwnerId);
                if (!string.IsNullOrWhiteSpace(ownerId))
                {
                    ownerIds.Add(ownerId);
                }
            }

            return ownerIds.Count == 0 ? "<none>" : string.Join(",", ownerIds);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
