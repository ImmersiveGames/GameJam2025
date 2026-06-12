using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityContentReleaseFinalizationStageCommand
    {
        public ActivityContentReleaseFinalizationStageCommand(
            SessionActivityCommand command,
            int entrySequence,
            int loadedSceneCount,
            int releasedSceneCount,
            bool skippedNoContent,
            string completionKind,
            string status,
            string continuationKind)
        {
            Command = command;
            ActivityId = Normalize(command.Identity.ActivityId);
            ActivityOrdinal = command.Identity.ActivityOrdinal;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            LoadedSceneCount = loadedSceneCount < 0 ? 0 : loadedSceneCount;
            ReleasedSceneCount = releasedSceneCount < 0 ? 0 : releasedSceneCount;
            SkippedNoContent = skippedNoContent;
            CompletionKind = Normalize(completionKind);
            Status = Normalize(status);
            ContinuationKind = Normalize(continuationKind);
        }

        public SessionActivityCommand Command { get; }
        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public int LoadedSceneCount { get; }
        public int ReleasedSceneCount { get; }
        public bool SkippedNoContent { get; }
        public string CompletionKind { get; }
        public string Status { get; }
        public string ContinuationKind { get; }
        public string Source => Command.Source;
        public string Reason => Command.Reason;

        public bool IsValid =>
            Command.Identity.IsValid &&
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0 &&
            string.Equals(Command.Identity.ActivityId, ActivityId, StringComparison.Ordinal) &&
            Command.Identity.ActivityOrdinal == ActivityOrdinal &&
            EntrySequence > 0 &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(CompletionKind) &&
            !string.IsNullOrWhiteSpace(Status) &&
            !string.IsNullOrWhiteSpace(ContinuationKind);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    internal readonly struct ActivityContentReleaseFinalizationStageResult
    {
        public ActivityContentReleaseFinalizationStageResult(
            bool completed,
            SessionActivityIdentity identity,
            string continuationKind,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            ContinuationKind = string.IsNullOrWhiteSpace(continuationKind) ? string.Empty : continuationKind.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public string ContinuationKind { get; }
        public string Reason { get; }
        public bool IsValid => Completed && Identity.IsValid && !string.IsNullOrWhiteSpace(ContinuationKind);
    }

    internal static class ActivityContentReleaseFinalizationStage
    {
        public static ActivityContentReleaseFinalizationStageResult Execute(
            ActivityContentReleaseFinalizationStageCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            ActivityContentRuntimeState contentRuntimeState,
            ActivityContentReleaseRuntimeState releaseRuntimeState,
            ActivityObjectExitRuntimeState objectExitRuntimeState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityContentReleaseFinalizationStageCommand is invalid.");
            }

            endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            contentRuntimeState = contentRuntimeState ?? throw new ArgumentNullException(nameof(contentRuntimeState));
            releaseRuntimeState = releaseRuntimeState ?? throw new ArgumentNullException(nameof(releaseRuntimeState));
            objectExitRuntimeState = objectExitRuntimeState ?? throw new ArgumentNullException(nameof(objectExitRuntimeState));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            bool loadedSetPresentBefore = contentRuntimeState.HasCurrentLoadedSet;
            bool pendingContextPresentBefore = releaseRuntimeState.HasPendingReleaseContext;
            bool awaitingBefore = releaseRuntimeState.IsAwaitingContinuation;

            int entrySequence = command.EntrySequence;
            var completedIdentity = endpoint.BuildIdentity(
                definition,
                SessionActivityStage.ActivityContentReleaseCompleted,
                entrySequence);

            LogFinalizationEvent(
                "ActivityContentReleaseFinalizationStarted",
                command,
                loadedSetPresentBefore,
                pendingContextPresentBefore,
                awaitingBefore,
                loadedSetPresentAfter: loadedSetPresentBefore,
                pendingContextPresentAfter: pendingContextPresentBefore,
                awaitingAfter: awaitingBefore,
                DebugUtility.Colors.Info);

            LogFinalizationEvent(
                "ActivityContentReleaseFinalizationCleanupStarted",
                command,
                loadedSetPresentBefore,
                pendingContextPresentBefore,
                awaitingBefore,
                loadedSetPresentAfter: loadedSetPresentBefore,
                pendingContextPresentAfter: pendingContextPresentBefore,
                awaitingAfter: awaitingBefore,
                DebugUtility.Colors.Info);

            ActivityObjectContributorUnregisterStage.Execute(
                new ActivityObjectContributorUnregisterStageCommand(
                    command.Command.Identity,
                    command.Command,
                    entrySequence),
                definition,
                endpoint,
                objectExitRuntimeState,
                facts,
                snapshots);

            contentRuntimeState.ClearCurrentLoadedSet(
                command.ActivityId,
                entrySequence,
                "ActivityContentReleaseFinalizationStage",
                "activity_content_release_finalized");
            releaseRuntimeState.ClearPendingReleaseContext(command.ActivityId, entrySequence, "ActivityContentReleaseFinalizationStage", "activity_content_release_finalized");
            releaseRuntimeState.SetAwaitingContinuation(false, command.ActivityId, entrySequence, "ActivityContentReleaseFinalizationStage", "activity_content_release_finalized");
            bool loadedSetPresentAfter = contentRuntimeState.HasCurrentLoadedSet;
            bool pendingContextPresentAfter = releaseRuntimeState.HasPendingReleaseContext;
            bool awaitingAfter = releaseRuntimeState.IsAwaitingContinuation;

            LogFinalizationEvent(
                "ActivityContentReleaseFinalizationCleanupCompleted",
                command,
                loadedSetPresentBefore,
                pendingContextPresentBefore,
                awaitingBefore,
                loadedSetPresentAfter,
                pendingContextPresentAfter,
                awaitingAfter,
                DebugUtility.Colors.Success);

            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActivityContentReleaseCompleted);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActivityContentReleaseCompleted,
                completedIdentity,
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity content release completed scenes='{command.ReleasedSceneCount}' status='{command.Status}'.");
            LogFinalizationEvent(
                "ActivityContentReleaseCompleted",
                command,
                loadedSetPresentBefore,
                pendingContextPresentBefore,
                awaitingBefore,
                loadedSetPresentAfter,
                pendingContextPresentAfter,
                awaitingAfter,
                DebugUtility.Colors.Success);
            new ActivityEntryLogSink().LogPhaseBoundary(
                "SessionActivityDematerializationCompleted",
                completedIdentity,
                command.Source,
                command.Reason,
                completed: true,
                detail: $"phase='dematerialization' scenes='{command.ReleasedSceneCount}' status='{command.Status}' skippedNoContent='{ToLowerInvariant(command.SkippedNoContent)}'");
            endpoint.EmitSnapshot(
                snapshots,
                "activity_content_release_completed",
                command.Source,
                command.Reason,
                $"'{command.ActivityId}' activity content release completed scenes='{command.ReleasedSceneCount}' status='{command.Status}'.");

            LogFinalizationEvent(
                "ActivityContentReleaseFinalizationCompleted",
                command,
                loadedSetPresentBefore,
                pendingContextPresentBefore,
                awaitingBefore,
                loadedSetPresentAfter,
                pendingContextPresentAfter,
                awaitingAfter,
                DebugUtility.Colors.Success);

            return new ActivityContentReleaseFinalizationStageResult(
                completed: true,
                identity: completedIdentity,
                continuationKind: command.ContinuationKind,
                reason: "finalized");
        }

        private static void LogFinalizationEvent(
            string eventName,
            ActivityContentReleaseFinalizationStageCommand command,
            bool loadedSetPresentBefore,
            bool pendingContextPresentBefore,
            bool awaitingBefore,
            bool loadedSetPresentAfter,
            bool pendingContextPresentAfter,
            bool awaitingAfter,
            string color)
        {
            DebugUtility.Log(
                typeof(ActivityContentReleaseFinalizationStage),
                $"[OBS][ActivityContentReleaseFinalizationStage] event='{eventName}' owner='ActivityContentReleaseFinalizationStage' " +
                $"pipelineId='{command.Command.Identity.PipelineId}' sessionStateId='{command.Command.Identity.SessionId}' activityId='{command.ActivityId}' " +
                $"entrySequence='{command.EntrySequence}' stage='{SessionActivityStage.ActivityContentReleaseCompleted}' source='{command.Source}' reason='{command.Reason}' " +
                $"completionKind='{command.CompletionKind}' status='{command.Status}' loadedSceneCount='{command.LoadedSceneCount}' releasedSceneCount='{command.ReleasedSceneCount}' " +
                $"skippedNoContent='{ToLowerInvariant(command.SkippedNoContent)}' pendingReleaseContextPresentBefore='{ToLowerInvariant(pendingContextPresentBefore)}' " +
                $"pendingReleaseContextPresentAfter='{ToLowerInvariant(pendingContextPresentAfter)}' loadedSetPresentBefore='{ToLowerInvariant(loadedSetPresentBefore)}' " +
                $"loadedSetPresentAfter='{ToLowerInvariant(loadedSetPresentAfter)}' awaitingContinuationBefore='{ToLowerInvariant(awaitingBefore)}' " +
                $"awaitingContinuationAfter='{ToLowerInvariant(awaitingAfter)}' continuationKind='{command.ContinuationKind}'.",
                color);
        }

        private static string ToLowerInvariant(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
