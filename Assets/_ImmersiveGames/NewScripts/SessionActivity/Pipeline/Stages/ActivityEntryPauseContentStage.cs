using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryPauseContentStage
    {
        public const string Owner = "ActivityEntryPauseContentStage";
        public const string MacroLifecycleOwner = "SessionActivityPipeline";

        public static ActivityPauseContentBindingResult Bind(
            SessionActivityIdentity identity,
            ActivityPauseContentProfileAsset profileAsset,
            SessionActivityRoutePauseSurfaceContext routePauseSurfaceContext,
            ISessionActivityPauseContentAdapter adapter,
            ActivityPauseContentRuntimeState runtimeState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPauseContentStage requires a valid identity.");
            }

            adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            facts ??= new List<SessionActivityFact>();
            snapshots ??= new List<SessionActivitySnapshot>();

            DebugUtility.LogVerbose(typeof(ActivityEntryPauseContentStage),
                $"event='ActivityPauseContentResolveStarted' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' routePauseSurface='{(routePauseSurfaceContext.HasSurface ? "present" : "absent")}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            if (profileAsset == null)
            {
                var skipped = ActivityPauseContentBindingResult.SkippedNoContribution(identity, "activity_pause_content_skipped_no_contribution");
                runtimeState.StoreCurrentBinding(skipped, source, reason);
                DebugUtility.Log(typeof(ActivityEntryPauseContentStage),
                    $"event='ActivityPauseContentSkippedNoContribution' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return skipped;
            }

            if (!routePauseSurfaceContext.HasSurface || !routePauseSurfaceContext.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseContent] RoutePauseSurfaceContext required for activity pause content binding activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' profile='{profileAsset.name}'.");
            }

            var profile = profileAsset.ToProfile();
            if (!profile.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseContent] ActivityPauseContentProfile invalid activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' profile='{profileAsset.name}'.");
            }

            var command = new ActivityPauseContentBindingCommand(
                identity,
                routePauseSurfaceContext,
                profile,
                source,
                reason);
            var result = adapter.Bind(command);
            if (!result.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseContent] Activity pause content adapter returned invalid result activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' profileId='{profile.ProfileId}'.");
            }

            runtimeState.StoreCurrentBinding(result, source, reason);
            DebugUtility.Log(typeof(ActivityEntryPauseContentStage),
                $"event='ActivityPauseContentBound' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' profileId='{profile.ProfileId}' requestedCount='{result.RequestedCount}' boundCount='{result.BoundCount}' skippedCount='{result.SkippedCount}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Success);
            return result;
        }

        public static ActivityPauseContentReleaseResult Release(
            SessionActivityIdentity identity,
            SessionActivityRoutePauseSurfaceContext routePauseSurfaceContext,
            ISessionActivityPauseContentAdapter adapter,
            ActivityPauseContentRuntimeState runtimeState,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryPauseContentStage release requires a valid identity.");
            }

            adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));

            if (!runtimeState.HasCurrentBinding)
            {
                var skipped = new ActivityPauseContentReleaseResult(
                    true,
                    true,
                    identity,
                    string.Empty,
                    0,
                    "activity_pause_content_release_skipped_no_bound_content");
                runtimeState.ClearCurrentBinding(source, reason);
                DebugUtility.LogVerbose(typeof(ActivityEntryPauseContentStage),
                    $"event='ActivityPauseContentReleaseSkippedNoBoundContent' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return skipped;
            }

            var command = new ActivityPauseContentReleaseCommand(
                identity,
                routePauseSurfaceContext,
                runtimeState.CurrentBinding,
                source,
                reason);
            var result = adapter.Release(command);
            if (!result.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][PauseContent] Activity pause content adapter returned invalid release result activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}'.");
            }

            runtimeState.ClearCurrentBinding(source, reason);
            DebugUtility.Log(typeof(ActivityEntryPauseContentStage),
                $"event='ActivityPauseContentReleased' owner='{Owner}' macroLifecycleOwner='{MacroLifecycleOwner}' activityId='{identity.ActivityId}' entrySequence='{identity.EntrySequence}' profileId='{result.ProfileId}' releasedCount='{result.ReleasedCount}' skipped='{result.Skipped.ToString().ToLowerInvariant()}' source='{source}' reason='{reason}'.",
                result.Skipped ? DebugUtility.Colors.Warning : DebugUtility.Colors.Success);
            return result;
        }
    }
}
