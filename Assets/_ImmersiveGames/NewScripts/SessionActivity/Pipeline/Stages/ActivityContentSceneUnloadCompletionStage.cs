using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityContentSceneUnloadCompletionStageCommand
    {
        public ActivityContentSceneUnloadCompletionStageCommand(
            SessionActivityCommand command,
            SessionActivityPendingOperation operation,
            ActivityContentUnloadResultKind unloadKind)
        {
            Command = command;
            Operation = operation;
            UnloadKind = unloadKind;
        }

        public SessionActivityCommand Command { get; }
        public SessionActivityPendingOperation Operation { get; }
        public ActivityContentUnloadResultKind UnloadKind { get; }
        public string Source => Command.Source;
        public string Reason => Command.Reason;

        public bool IsValid =>
            Command.Identity.IsValid &&
            Operation is { IsValid: true, OperationKind: SessionActivityPendingOperationKind.ActivityContentSceneUnload } &&
            !string.IsNullOrWhiteSpace(Source);
    }

    internal static class ActivityContentSceneUnloadCompletionStage
    {
        private const string Owner = "ActivityContentSceneUnloadCompletionStage";

        public static void Execute(
            ActivityContentSceneUnloadCompletionStageCommand command,
            IActivityEntryRuntimeBridge endpoint,
            SessionActivityPipeline.PendingActivityContentReleaseContext context,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityContentSceneUnloadCompletionStageCommand is invalid.");
            }

            endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            if (context == null || !context.IsValid)
            {
                throw new InvalidOperationException("Pending activity content release context is invalid for scene unload completion.");
            }

            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            var definition = context.Definition;
            int entrySequence = context.EntrySequence;
            var operation = command.Operation;

            endpoint.ClearPendingOperation();

            var unloadedIdentity = endpoint.BuildIdentity(
                definition,
                SessionActivityStage.ActivityContentSceneUnloaded,
                entrySequence);
            endpoint.SetCurrentIdentity(unloadedIdentity, SessionActivityStage.ActivityContentSceneUnloaded);

            string releaseStatus = command.UnloadKind == ActivityContentUnloadResultKind.SkippedNoContent
                ? "SkippedNoContent"
                : "Unloaded";

            string factMessage =
                $"'{definition.ActivityId}' activity content scene unloaded operationId='{operation.OperationId}' sceneName='{operation.SceneName}' releaseStatus='{releaseStatus}'.";

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentSceneUnloaded,
                unloadedIdentity,
                command.Source,
                command.Reason,
                factMessage);
            endpoint.EmitSnapshot(
                snapshots,
                "activity_content_scene_unloaded",
                command.Source,
                command.Reason,
                factMessage);

            DebugUtility.LogVerbose(
                typeof(ActivityContentSceneUnloadCompletionStage),
                $"event='ActivityContentUnloadCompletionTechnicalCompleted' owner='{Owner}' pipelineId='{unloadedIdentity.PipelineId}' sessionStateId='{unloadedIdentity.SessionId}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' operationId='{operation.OperationId}' unloadKind='{command.UnloadKind}' source='{Normalize(command.Source)}' reason='{Normalize(command.Reason)}' pendingOperationCleared='true'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
