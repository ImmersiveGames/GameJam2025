using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityExitActorAttributeReleaseStage
    {
        public static void Execute(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            ActivityActorExitRuntimeState runtimeState,
            IActivityExitActorTeardownRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            int entrySequence = command.EntrySequence;
            var startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorAttributeReleaseStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release started.");
            endpoint.EmitSnapshot(snapshots, "actor_attribute_release_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release started.");
            IReadOnlyList<SessionActivityPipeline.ActorAttributeCapabilityState> activeStates = runtimeState.ResolveActiveActorAttributeStates();
            int totalCount = activeStates?.Count ?? 0;
            int releasedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            int initialStateResetAppliedCount = 0;
            int initialStateResetSkippedCount = 0;
            if (activeStates == null || activeStates.Count == 0)
            {
                var skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseSkipped, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeReleaseSkipped);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release skipped reason='no_active_attribute_capability'.");
                endpoint.EmitSnapshot(snapshots, "actor_attribute_release_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release skipped reason='no_active_attribute_capability'.");
                skippedCount += 1;
            }
            else
            {
                int releasedOrSkippedCount = 0;
                for (int index = 0; index < activeStates.Count; index++)
                {
                    var capabilityState = activeStates[index];
                    if (!capabilityState.IsValid || capabilityState.Endpoint == null)
                    {
                        continue;
                    }

                    string actorId = capabilityState.ActorId;
                    var initialStateResetIntent = ActivityResetIntent.EntryInitialize;
                    var initialStateResetProfile = ActivityResetIntentProfileDefaults.ResolveStateProfile(initialStateResetIntent);
                    if (!capabilityState.Endpoint.TryResetToInitial(
                        startedIdentity,
                        command.Source,
                        command.Reason,
                        out var initialStateResetResult))
                    {
                        failedCount += 1;
                        var failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeReleaseFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute initial-state reset failed actorId='{actorId}' resetIntent='{initialStateResetIntent}' resetStateProfile='{initialStateResetProfile}' reason='{initialStateResetResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_initial_state_reset_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute initial-state reset failed actorId='{actorId}' reason='{initialStateResetResult.Reason}'.");
                        DebugUtility.Log(typeof(ActivityExitActorAttributeReleaseStage),
                            $"event='ActorAttributeInitialStateResetFailed' owner='ActivityExitActorAttributeReleaseStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{actorId}' actorInstanceRuntimeId='{capabilityState.ActorInstanceRuntimeId}' resetIntent='{initialStateResetIntent}' resetStateProfile='{initialStateResetProfile}' resetProfileSource='entry_initialize_initial_state' outcomeReason='{initialStateResetResult.Reason}' source='{command.Source}' reason='{command.Reason}'.",
                            DebugUtility.Colors.Error);
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorAttributeReleaseStage][ActorAttributeInitialStateReset] Initial-state reset failed actorId='{actorId}' reason='{initialStateResetResult.Reason}'.");
                    }

                    if (initialStateResetResult.Rejected || initialStateResetResult.Failed)
                    {
                        failedCount += 1;
                        var failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeReleaseFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute initial-state reset failed actorId='{actorId}' resetIntent='{initialStateResetIntent}' resetStateProfile='{initialStateResetProfile}' reason='{initialStateResetResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_initial_state_reset_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute initial-state reset failed actorId='{actorId}' reason='{initialStateResetResult.Reason}'.");
                        DebugUtility.Log(typeof(ActivityExitActorAttributeReleaseStage),
                            $"event='ActorAttributeInitialStateResetFailed' owner='ActivityExitActorAttributeReleaseStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{actorId}' actorInstanceRuntimeId='{capabilityState.ActorInstanceRuntimeId}' resetIntent='{initialStateResetIntent}' resetStateProfile='{initialStateResetProfile}' resetProfileSource='entry_initialize_initial_state' outcomeReason='{initialStateResetResult.Reason}' source='{command.Source}' reason='{command.Reason}'.",
                            DebugUtility.Colors.Error);
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorAttributeReleaseStage][ActorAttributeInitialStateReset] Initial-state reset failed actorId='{actorId}' reason='{initialStateResetResult.Reason}'.");
                    }

                    if (initialStateResetResult.SkippedNoContent)
                    {
                        initialStateResetSkippedCount += 1;
                    }
                    else
                    {
                        initialStateResetAppliedCount += 1;
                    }

                    DebugUtility.Log(typeof(ActivityExitActorAttributeReleaseStage),
                        $"event='{(initialStateResetResult.SkippedNoContent ? "ActorAttributeInitialStateResetSkipped" : "ActorAttributeInitialStateResetApplied")}' owner='ActivityExitActorAttributeReleaseStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{actorId}' actorInstanceRuntimeId='{capabilityState.ActorInstanceRuntimeId}' resetIntent='{initialStateResetIntent}' resetStateProfile='{initialStateResetProfile}' resetProfileSource='entry_initialize_initial_state' resetAttributeCount='{initialStateResetResult.ResetAttributeCount}' outcomeReason='{initialStateResetResult.Reason}' source='{command.Source}' reason='{command.Reason}'.",
                        initialStateResetResult.SkippedNoContent ? DebugUtility.Colors.Info : DebugUtility.Colors.Success);

                    if (!capabilityState.Endpoint.TryRelease(startedIdentity, out var releaseResult))
                    {
                        failedCount += 1;
                        var failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeReleaseFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorAttributeReleaseStage][ActorAttributeRelease] Release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                    }

                    if (releaseResult.Rejected || releaseResult.Failed)
                    {
                        failedCount += 1;
                        var failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeReleaseFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorAttributeReleaseStage][ActorAttributeRelease] Release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                    }

                    var stage = releaseResult.SkippedNoContent
                        ? SessionActivityStage.ActorAttributeReleaseSkipped
                        : SessionActivityStage.ActorAttributeReleased;
                    var factKind = releaseResult.SkippedNoContent
                        ? SessionActivityFactKind.ActorAttributeReleaseSkipped
                        : SessionActivityFactKind.ActorAttributeReleased;
                    string snapshotKind = releaseResult.SkippedNoContent ? "actor_attribute_release_skipped" : "actor_attribute_released";
                    string eventName = releaseResult.SkippedNoContent ? "ActorAttributeReleaseSkipped" : "ActorAttributeReleased";

                    var outcomeIdentity = endpoint.BuildIdentity(definition, stage, entrySequence);
                    endpoint.SetCurrentIdentity(outcomeIdentity, stage);
                    endpoint.EmitFact(facts, factKind, outcomeIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release outcome actorId='{actorId}' reason='{releaseResult.Reason}' releasedAttributeCount='{releaseResult.ReleasedAttributeCount}'.");
                    endpoint.EmitSnapshot(snapshots, snapshotKind, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release outcome actorId='{actorId}' reason='{releaseResult.Reason}' releasedAttributeCount='{releaseResult.ReleasedAttributeCount}'.");
                    DebugUtility.Log(typeof(ActivityExitActorAttributeReleaseStage),
                        $"event='{eventName}' owner='ActivityExitActorAttributeReleaseStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{actorId}' actorInstanceRuntimeId='{capabilityState.ActorInstanceRuntimeId}' actorScope='{ResolveActorScopeLabel(capabilityState.ActorInstanceRuntimeId)}' componentKind='ActorAttribute' componentScope='ActivityScoped' componentLifetimePolicy='ActorAttributeEndpointRelease' releaseTrigger='{command.ReleaseRail}' releaseDecision='{(releaseResult.SkippedNoContent ? "Skip" : "Release")}' outcomeReason='{releaseResult.Reason}' releasedAttributeCount='{releaseResult.ReleasedAttributeCount}' source='{command.Source}' reason='{command.Reason}'.",
                        releaseResult.SkippedNoContent ? DebugUtility.Colors.Info : DebugUtility.Colors.Success);

                    if (releaseResult.SkippedNoContent)
                    {
                        skippedCount += 1;
                    }
                    else
                    {
                        releasedCount += 1;
                    }

                    runtimeState.RemoveActiveActorAttributeCapability(capabilityState.ActorInstanceRuntimeId, definition.ActivityId, entrySequence, command.Source, command.Reason);
                    releasedOrSkippedCount += 1;
                }

                if (releasedOrSkippedCount == 0)
                {
                    skippedCount += 1;
                    var skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseSkipped, entrySequence);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeReleaseSkipped);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release skipped reason='no_active_attribute_capability'.");
                    endpoint.EmitSnapshot(snapshots, "actor_attribute_release_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release skipped reason='no_active_attribute_capability'.");
                }
            }

            var completedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseCompleted, entrySequence);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorAttributeReleaseCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release completed total='{totalCount}' released='{releasedCount}' skipped='{skippedCount}' failed='{failedCount}' initialStateResetApplied='{initialStateResetAppliedCount}' initialStateResetSkipped='{initialStateResetSkippedCount}'.");
            endpoint.EmitSnapshot(snapshots, "actor_attribute_release_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release completed total='{totalCount}' released='{releasedCount}' skipped='{skippedCount}' failed='{failedCount}' initialStateResetApplied='{initialStateResetAppliedCount}' initialStateResetSkipped='{initialStateResetSkippedCount}'.");
        }
        private static string ResolveActorScopeLabel(ActorInstanceRuntimeId actorInstanceRuntimeId)
        {
            if (!actorInstanceRuntimeId.IsValid || string.IsNullOrWhiteSpace(actorInstanceRuntimeId.Value))
            {
                return "Unknown";
            }

            string value = actorInstanceRuntimeId.Value.Trim();
            if (value.EndsWith("|SessionScoped", StringComparison.Ordinal))
            {
                return "SessionScoped";
            }

            if (value.EndsWith("|RouteScoped", StringComparison.Ordinal))
            {
                return "RouteScoped";
            }

            if (value.EndsWith("|ActivityScoped", StringComparison.Ordinal))
            {
                return "ActivityScoped";
            }

            return "Unknown";
        }
    }
}
