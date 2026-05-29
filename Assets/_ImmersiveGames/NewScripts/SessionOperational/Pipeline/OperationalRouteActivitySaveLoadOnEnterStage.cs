using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public interface IRouteActivityLoadedSnapshotPayloadStore
    {
        void ClearPendingLoadedSnapshotPayload();
        void SetPendingLoadedSnapshotPayload(string activityIdentity, LoadedSessionActivitySnapshotPayload payload, int payloadSize);
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
            ISessionOperationalActivitySaveAdapter activitySaveAdapter,
            IProgressionSlotContextResolver progressionSlotContextResolver,
            ISaveStateService saveStateService,
            IRouteActivityLoadedSnapshotPayloadStore loadedSnapshotStore,
            string routeActivitySnapshotSchemaId,
            string source,
            string reason)
        {
            RuntimeModeConfig = runtimeModeConfig;
            RouteCommand = routeCommand;
            RouteActivitySavePlan = routeActivitySavePlan;
            ActivitySaveAdapter = activitySaveAdapter;
            ProgressionSlotContextResolver = progressionSlotContextResolver;
            SaveStateService = saveStateService;
            LoadedSnapshotStore = loadedSnapshotStore;
            RouteActivitySnapshotSchemaId = Normalize(routeActivitySnapshotSchemaId);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public RuntimeModeConfig RuntimeModeConfig { get; }
        public SessionOperationalRouteCommand RouteCommand { get; }
        public RouteActivitySavePlan RouteActivitySavePlan { get; }
        public ISessionOperationalActivitySaveAdapter ActivitySaveAdapter { get; }
        public IProgressionSlotContextResolver ProgressionSlotContextResolver { get; }
        public ISaveStateService SaveStateService { get; }
        public IRouteActivityLoadedSnapshotPayloadStore LoadedSnapshotStore { get; }
        public string RouteActivitySnapshotSchemaId { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            RuntimeModeConfig != null &&
            RouteCommand.IsValid &&
            RouteActivitySavePlan.IsValid &&
            ActivitySaveAdapter != null &&
            ProgressionSlotContextResolver != null &&
            LoadedSnapshotStore != null &&
            !string.IsNullOrWhiteSpace(RouteActivitySnapshotSchemaId);

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
            string detail)
        {
            Kind = kind;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalRouteActivitySaveLoadOnEnterResultKind Kind { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalRouteActivitySaveLoadOnEnterResultKind.Completed;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalRouteActivitySaveLoadOnEnterStage
    {
        public OperationalRouteActivitySaveLoadOnEnterResult Execute(OperationalRouteActivitySaveLoadOnEnterCommand command)
        {
            if (!command.IsValid)
            {
                return new OperationalRouteActivitySaveLoadOnEnterResult(
                    OperationalRouteActivitySaveLoadOnEnterResultKind.Failed,
                    "invalid_command",
                    "OperationalRouteActivitySaveLoadOnEnterCommand invalido.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;
            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadOnEnterStageStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            ExecuteLoadOnEnterOrFail(command);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadOnEnterStageCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new OperationalRouteActivitySaveLoadOnEnterResult(
                OperationalRouteActivitySaveLoadOnEnterResultKind.Completed,
                "completed",
                "route_activity_save_load_on_enter_completed");
        }

        private static void ExecuteLoadOnEnterOrFail(OperationalRouteActivitySaveLoadOnEnterCommand command)
        {
            RouteActivitySaveLoadPlan loadOnEnterPlan = command.RouteActivitySavePlan.LoadOnEnter;
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
                return;
            }

            command.LoadedSnapshotStore.ClearPendingLoadedSnapshotPayload();
            if (!TryResolveCurrentSnapshotIdForRouteActivityLoad(command.SaveStateService, out string currentSnapshotId, out string snapshotFailureReason))
            {
                DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Waiting' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' payloadObjectCount='0' targetIds='<none>' payloadSize='0'.",
                    DebugUtility.Colors.Info);
                DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                    $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' skipKind='{RouteActivitySaveSkipKind.NoCurrentSnapshot}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(RouteActivitySaveSkipKind.NoCurrentSnapshot)}' detail='snapshotPointerReason={Normalize(snapshotFailureReason)}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            ProgressionSlotContext slotContext = ResolveProgressionSlotContextOrFail(
                command.ProgressionSlotContextResolver,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                command.Source,
                command.Reason);
            if (!string.Equals(slotContext.SnapshotId.Value, currentSnapshotId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] ProgressionSlotContext snapshotId mismatch with CurrentSnapshotId routeIdentity='{Normalize(routeIdentity)}' routeOperationId='{Normalize(routeOperationId)}' transitionId='{Normalize(transitionId)}' routeSequence='{routeSequence}' slotSnapshotId='{Normalize(slotContext.SnapshotId.Value)}' currentSnapshotId='{Normalize(currentSnapshotId)}'.");
            }

            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            RouteActivitySaveLoadResult result = command.ActivitySaveAdapter.LoadActivitySaveOnEnter(
                command.RuntimeModeConfig,
                command.RouteCommand,
                slotContext,
                activityIdentity);

            if (result.IsSkipped)
            {
                if (result.FailureKind == RouteActivitySaveSnapshotFailureKind.SnapshotPayloadMissing)
                {
                    DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                        $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Waiting' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' payloadObjectCount='0' targetIds='<none>' payloadSize='0'.",
                        DebugUtility.Colors.Info);
                }

                DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                    $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' skipKind='{result.SkipKind}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(result.SkipKind)}' detail='{Normalize(result.Detail)}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!result.IsLoaded)
            {
                string message = $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] load-on-enter retornou estado invalido routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.";
                DebugUtility.LogError<OperationalRouteActivitySaveLoadOnEnterStage>(message);
                throw new InvalidOperationException(message);
            }

            LoadedSessionActivitySnapshotPayloadParseResult parseResult =
                LoadedSessionActivitySnapshotPayloadParser.Parse(result.ActivitySnapshotPayload, command.RouteActivitySnapshotSchemaId);
            if (!parseResult.Succeeded)
            {
                DebugUtility.LogError(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Failed' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' payloadObjectCount='0' targetIds='<none>' payloadSize='0' failureKind='{parseResult.FailureKind}' failureReason='{Normalize(parseResult.FailureReason)}' detail='{Normalize(parseResult.Detail)}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] payload invalido no load-on-enter routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' failureKind='{parseResult.FailureKind}' failureReason='{Normalize(parseResult.FailureReason)}'.");
            }

            LoadedSessionActivitySnapshotPayload loadedPayload = parseResult.Payload;
            command.LoadedSnapshotStore.SetPendingLoadedSnapshotPayload(
                Normalize(activityIdentity),
                loadedPayload,
                result.ActivitySnapshotPayload.Length);
            string loadedTargetIds = BuildLoadedSnapshotTargetIds(loadedPayload.Objects);
            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySnapshotPayloadLoaded activityIdentity='{Normalize(activityIdentity)}' sourceActivityId='{Normalize(loadedPayload.ActivityId)}' sourceEntrySequence='{loadedPayload.SourceEntrySequence}' payloadObjectCount='{loadedPayload.Objects.Count}' targetIds='{loadedTargetIds}' schemaId='{Normalize(loadedPayload.SchemaId)}' payloadSize='{result.ActivitySnapshotPayload.Length}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Passed' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='true' sourceActivityId='{Normalize(loadedPayload.ActivityId)}' sourceEntrySequence='{loadedPayload.SourceEntrySequence}' payloadObjectCount='{loadedPayload.Objects.Count}' targetIds='{loadedTargetIds}' payloadSize='{result.ActivitySnapshotPayload.Length}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(OperationalRouteActivitySaveLoadOnEnterStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadCompleted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' detail='{Normalize(result.Detail)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
        }

        private static bool TryResolveCurrentSnapshotIdForRouteActivityLoad(
            ISaveStateService saveStateService,
            out string currentSnapshotId,
            out string failureReason)
        {
            currentSnapshotId = string.Empty;
            if (saveStateService == null)
            {
                failureReason = "save_state_service_missing";
                return false;
            }

            if (!saveStateService.HasCurrent)
            {
                failureReason = "current_save_missing";
                return false;
            }

            SaveCurrentState currentState = saveStateService.CurrentState;
            if (currentState == null || !currentState.IsValid)
            {
                failureReason = "current_state_invalid";
                return false;
            }

            string snapshotPointer = Normalize(currentState.CurrentSnapshotId);
            if (string.IsNullOrWhiteSpace(snapshotPointer))
            {
                failureReason = "current_snapshot_missing";
                return false;
            }

            currentSnapshotId = snapshotPointer;
            failureReason = "resolved";
            return true;
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

        private static string BuildLoadedSnapshotTargetIds(IReadOnlyList<LoadedSessionActivitySnapshotPayloadObject> objects)
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

            return targetIds.Count == 0 ? "<none>" : string.Join(",", targetIds);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
