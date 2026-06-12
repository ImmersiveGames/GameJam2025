using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityContentSceneUnloadDispatchStageCommand
    {
        public ActivityContentSceneUnloadDispatchStageCommand(SessionActivityCommand command)
        {
            Command = command;
        }

        public SessionActivityCommand Command { get; }
        public string Source => Command.Source;
        public string Reason => Command.Reason;

        public bool IsValid =>
            Command.Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source);
    }

    internal readonly struct ActivityContentSceneUnloadDispatchStageResult
    {
        public ActivityContentSceneUnloadDispatchStageResult(
            bool dispatched,
            bool completedNoMoreScenes,
            SessionActivityPendingOperation pendingOperation,
            string reason)
        {
            Dispatched = dispatched;
            CompletedNoMoreScenes = completedNoMoreScenes;
            PendingOperation = pendingOperation;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public bool Dispatched { get; }
        public bool CompletedNoMoreScenes { get; }
        public SessionActivityPendingOperation PendingOperation { get; }
        public string Reason { get; }
        public bool IsValid => Dispatched || CompletedNoMoreScenes;
    }

    internal static class ActivityContentSceneUnloadDispatchStage
    {
        public static ActivityContentSceneUnloadDispatchStageResult Execute(
            ActivityContentSceneUnloadDispatchStageCommand command,
            IActivityEntryRuntimeBridge endpoint,
            ActivityContentRuntimeState contentRuntimeState,
            ActivityContentReleaseRuntimeState releaseRuntimeState,
            ISessionActivityPendingOperationRunner pendingOperationRunner,
            ISessionActivityPendingOperationCallback pendingOperationCallback,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityContentSceneUnloadDispatchStageCommand is invalid.");
            }

            endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            contentRuntimeState = contentRuntimeState ?? throw new ArgumentNullException(nameof(contentRuntimeState));
            releaseRuntimeState = releaseRuntimeState ?? throw new ArgumentNullException(nameof(releaseRuntimeState));
            pendingOperationRunner = pendingOperationRunner ?? throw new ArgumentNullException(nameof(pendingOperationRunner));
            pendingOperationCallback = pendingOperationCallback ?? throw new ArgumentNullException(nameof(pendingOperationCallback));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            var context = releaseRuntimeState.PendingReleaseContext;
            if (context == null || !context.IsValid)
            {
                throw new InvalidOperationException("Pending activity content release context is invalid for scene unload dispatch.");
            }

            var definition = context.Definition;
            int entrySequence = context.EntrySequence;
            var loadedSet = contentRuntimeState.CurrentLoadedSet;
            int nextSceneIndex = context.NextSceneIndex;

            if (nextSceneIndex >= loadedSet.Scenes.Count)
            {
                DebugUtility.Log(
                    typeof(ActivityContentSceneUnloadDispatchStage),
                    $"[OBS][ActivityContentSceneUnloadDispatchStage] event='ActivityContentSceneUnloadDispatchCompletedNoMoreScenes' owner='ActivityContentSceneUnloadDispatchStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' sceneCount='{loadedSet.Scenes.Count}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return new ActivityContentSceneUnloadDispatchStageResult(
                    dispatched: false,
                    completedNoMoreScenes: true,
                    pendingOperation: default,
                    reason: "no_more_scenes");
            }

            var record = loadedSet.Scenes[nextSceneIndex];
            if (!record.IsValid)
            {
                throw new InvalidOperationException(
                    $"Activity content loaded scene record is invalid at index='{nextSceneIndex}'.");
            }

            var sceneReference = record.SceneReference;
            if (!sceneReference.IsValid)
            {
                throw new InvalidOperationException(
                    $"Activity content scene runtime reference is invalid for scene='{record.SceneName}' index='{nextSceneIndex}'.");
            }

            var unloadCommand = new ActivityContentSceneUnloadCommand(
                Guid.NewGuid().ToString("N"),
                record.Identity,
                record.ContentProfileId,
                record.SceneOrdinal,
                sceneReference,
                record.Requiredness,
                command.Source,
                command.Reason,
                command.Source,
                command.Reason);
            if (!unloadCommand.IsValid)
            {
                throw new InvalidOperationException(
                    $"ActivityContentSceneUnloadCommand is invalid for scene='{record.SceneName}'.");
            }

            var pendingOperation = BuildActivityContentReleasePendingOperation(
                endpoint,
                definition,
                entrySequence,
                unloadCommand);
            if (!pendingOperation.IsValid)
            {
                throw new InvalidOperationException(
                    $"Activity content scene unload pending operation is invalid scene='{record.SceneName}'.");
            }

            endpoint.SetPendingOperation(pendingOperation);
            EmitActivityContentSceneUnloadCommandIssued(
                endpoint,
                definition,
                command.Command,
                facts,
                snapshots,
                entrySequence,
                pendingOperation);

            DebugUtility.Log(
                typeof(ActivityContentSceneUnloadDispatchStage),
                $"[OBS][ActivityContentSceneUnloadDispatchStage] event='ActivityContentSceneUnloadDispatched' owner='ActivityContentSceneUnloadDispatchStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' sceneIndex='{nextSceneIndex}' sceneName='{record.SceneName}' operationId='{pendingOperation.OperationId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            pendingOperationRunner.RunActivityContentReleaseOperation(
                pendingOperation,
                unloadCommand,
                pendingOperationCallback);
            return new ActivityContentSceneUnloadDispatchStageResult(
                dispatched: true,
                completedNoMoreScenes: false,
                pendingOperation: pendingOperation,
                reason: "dispatched");
        }

        private static SessionActivityPendingOperation BuildActivityContentReleasePendingOperation(
            IActivityEntryRuntimeBridge endpoint,
            SessionActivityDefinition definition,
            int entrySequence,
            ActivityContentSceneUnloadCommand command)
        {
            var identity = endpoint.BuildIdentity(
                definition,
                SessionActivityStage.ActivityContentSceneUnloading,
                entrySequence);
            if (!identity.IsValid)
            {
                throw new InvalidOperationException(
                    $"Activity content scene unload identity is invalid activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            return new SessionActivityPendingOperation(
                command.OperationId,
                identity.PipelineId,
                identity.SessionId,
                definition.ActivityId,
                definition.ActivityOrdinal,
                entrySequence,
                SessionActivityPendingWindowKind.None,
                SessionActivityPendingOperationKind.ActivityContentSceneUnload,
                command.SceneKey,
                command.SceneName,
                command.Source,
                command.Reason);
        }

        private static void EmitActivityContentSceneUnloadCommandIssued(
            IActivityEntryRuntimeBridge endpoint,
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence,
            SessionActivityPendingOperation operation)
        {
            var unloadingIdentity = endpoint.BuildIdentity(
                definition,
                SessionActivityStage.ActivityContentSceneUnloading,
                entrySequence);
            endpoint.SetCurrentIdentity(unloadingIdentity, SessionActivityStage.ActivityContentSceneUnloading);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentSceneUnloadCommandIssued,
                unloadingIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene unload command issued operationId='{operation.OperationId}' sceneName='{operation.SceneName}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_content_scene_unload_command_issued",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' activity content scene unload command issued operationId='{operation.OperationId}' sceneName='{operation.SceneName}'.");
        }
    }
}
