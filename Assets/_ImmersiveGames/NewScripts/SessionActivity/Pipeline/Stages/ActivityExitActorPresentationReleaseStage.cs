using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityExitActorPresentationReleaseStage
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
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityExitActorTeardownCommand is invalid for ActorPresentation release.");
            }

            int entrySequence = command.EntrySequence;
            var rail = command.ReleaseRail;
            var targetActorInstanceRuntimeId = command.TargetActorInstanceRuntimeId;

            var startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorPresentationReleaseStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release started rail='{rail}' target='{(targetActorInstanceRuntimeId.IsValid ? targetActorInstanceRuntimeId.Value : "all")}'.");
            endpoint.EmitSnapshot(snapshots, "actor_presentation_release_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release started rail='{rail}'.");
            IReadOnlyList<ActivityActorExitRuntimeState.ActorPresentationCapabilityState> activeStates = runtimeState.ResolveActiveActorPresentationStates(targetActorInstanceRuntimeId);
            if (activeStates == null || activeStates.Count == 0)
            {
                var skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseSkipped, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationReleaseSkipped);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release skipped reason='no_active_actor_presentation_handle' rail='{rail}'.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_release_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release skipped reason='no_active_actor_presentation_handle' rail='{rail}'.");
            }
            else
            {
                for (int index = 0; index < activeStates.Count; index++)
                {
                    var state = activeStates[index];
                    var handle = state.RuntimeHandle;
                    if (!handle.IsValid)
                    {
                        continue;
                    }

                    var policy = handle.ResolvedPlan.ReleasePolicy;
                    bool shouldRelease = rail switch
                    {
                        SessionActivityPipeline.ActorPresentationReleaseRail.BeforeRematerialization => true,
                        SessionActivityPipeline.ActorPresentationReleaseRail.ActivityExit => policy == ActorPresentationReleasePolicy.ReleaseOnActivityExit,
                        SessionActivityPipeline.ActorPresentationReleaseRail.RouteExit => policy == ActorPresentationReleasePolicy.ReleaseOnRouteExit,
                        _ => false
                    };

                    if (!shouldRelease)
                    {
                        var skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseSkipped, entrySequence);
                        endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationReleaseSkipped);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release skipped actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' reason='policy_mismatch' policy='{policy}' rail='{rail}' mode='Retained'.");
                        DebugUtility.LogVerbose(typeof(ActivityExitActorPresentationReleaseStage),
                            $"event='ActorPresentationRetained' owner='ActivityExitActorPresentationReleaseStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' actorScope='{ResolveActorScopeLabel(state.ActorInstanceRuntimeId)}' componentKind='ActorPresentation' componentLifetimePolicy='{policy}' releaseTrigger='{rail}' releaseDecision='Retain' reason='policy_mismatch' source='{command.Source}' reason='{command.Reason}'.",
                            DebugUtility.Colors.Info);
                        continue;
                    }

                    var releaseResult = bridge.ReleaseActorPresentation(handle, command.Source, command.Reason);
                    if (!releaseResult.IsSuccess)
                    {
                        var failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationReleaseFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' reason='{releaseResult.ReasonCode}' rail='{rail}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_presentation_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' reason='{releaseResult.ReasonCode}' rail='{rail}'.");
                        DebugUtility.Log(typeof(ActivityExitActorPresentationReleaseStage),
                            $"event='ActorPresentationReleaseFailed' owner='ActivityExitActorPresentationReleaseStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' reason='{releaseResult.ReasonCode}' source='{command.Source}' reasonDetail='{command.Reason}'.",
                            DebugUtility.Colors.Error);
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorPresentationReleaseStage][ActorPresentationRelease] Release failed actorId='{state.ActorId}' reason='{releaseResult.ReasonCode}'.");
                    }

                    bool keptBound = policy == ActorPresentationReleasePolicy.KeepBound;
                    var releasedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleased, entrySequence);
                    endpoint.SetCurrentIdentity(releasedIdentity, SessionActivityStage.ActorPresentationReleased);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleased, releasedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation released actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' policy='{policy}' rail='{rail}' keptBound='{keptBound}' mode='{(keptBound ? "Retained" : "Released")}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_released", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation released actorId='{state.ActorId}' policy='{policy}' rail='{rail}' keptBound='{keptBound}'.");
                    DebugUtility.Log(typeof(ActivityExitActorPresentationReleaseStage),
                        $"event='ActorPresentationReleased' owner='ActivityExitActorPresentationReleaseStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' actorScope='{ResolveActorScopeLabel(state.ActorInstanceRuntimeId)}' componentKind='ActorPresentation' componentLifetimePolicy='{policy}' releaseTrigger='{rail}' releaseDecision='{(keptBound ? "Retain" : "Release")}' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Success);

                    if (keptBound)
                    {
                        if (rail == SessionActivityPipeline.ActorPresentationReleaseRail.BeforeRematerialization)
                        {
                            var failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseFailed, entrySequence);
                            endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationReleaseFailed);
                            endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' reason='keep_bound_blocks_rematerialization'.");
                            endpoint.EmitSnapshot(snapshots, "actor_presentation_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' reason='keep_bound_blocks_rematerialization'.");
                            DebugUtility.Log(typeof(ActivityExitActorPresentationReleaseStage),
                                $"event='ActorPresentationReleaseFailed' owner='ActivityExitActorPresentationReleaseStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' reason='keep_bound_blocks_rematerialization' source='{command.Source}' reasonDetail='{command.Reason}'.",
                                DebugUtility.Colors.Error);
                            throw new InvalidOperationException($"[FATAL][ActivityExitActorPresentationReleaseStage][ActorPresentationRelease] KeepBound blocks rematerialization actorId='{state.ActorId}'.");
                        }

                        continue;
                    }

                    runtimeState.RemoveActiveActorPresentation(state.ActorInstanceRuntimeId, definition.ActivityId, entrySequence, command.Source, command.Reason);
                }
            }

            var completedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseCompleted, entrySequence);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorPresentationReleaseCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release completed rail='{rail}'.");
            endpoint.EmitSnapshot(snapshots, "actor_presentation_release_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release completed rail='{rail}'.");
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
