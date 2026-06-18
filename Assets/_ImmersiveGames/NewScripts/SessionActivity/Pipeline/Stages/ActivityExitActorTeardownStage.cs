using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
using _ImmersiveGames.NewScripts.UnityUtils;
using PlayerActivityParticipantBinding = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.ActivityParticipantBinding;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal readonly struct ActivityExitActorTeardownCommand
    {
        public ActivityExitActorTeardownCommand(
            SessionActivityIdentity identity,
            SessionActivityCommand command,
            int entrySequence,
            SessionActivityPipeline.ActorPresentationReleaseRail releaseRail,
            ActorInstanceRuntimeId targetActorInstanceRuntimeId = default)
        {
            Identity = identity;
            Command = command;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            ReleaseRail = releaseRail;
            TargetActorInstanceRuntimeId = targetActorInstanceRuntimeId;
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityCommand Command { get; }
        public int EntrySequence { get; }
        public SessionActivityPipeline.ActorPresentationReleaseRail ReleaseRail { get; }
        public ActorInstanceRuntimeId TargetActorInstanceRuntimeId { get; }

        public string Source => Command.Source;
        public string Reason => Command.Reason;

        public bool IsValid =>
            Identity.IsValid &&
            EntrySequence > 0 &&
            ReleaseRail != SessionActivityPipeline.ActorPresentationReleaseRail.Unknown &&
            !string.IsNullOrWhiteSpace(Source);
    }

    internal readonly struct ActivityExitActorTeardownResult
    {
        public ActivityExitActorTeardownResult(
            bool completed,
            SessionActivityIdentity identity,
            string reason)
        {
            Completed = completed;
            Identity = identity;
            Reason = reason.TrimToEmpty();
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);
    }

    internal interface IActivityExitActorTeardownRuntimeBridge
    {
        ActorPresentationResult ReleaseActorPresentation(ActorPresentationRuntimeHandle handle, string source, string reason);

        IReadOnlyList<PlayerActorParticipationExitRecord> ExecutePlayerActorParticipationExit(
            PlayerActorParticipationExitCommand command,
            SessionActivityIdentity identity);
    }

    internal static class ActivityExitActorTeardownStage
    {
        public static ActivityExitActorTeardownResult Execute(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            ActivityActorExitRuntimeState runtimeState,
            IActivityExitActorTeardownRuntimeBridge bridge,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityExitActorTeardownCommand is invalid.");
            }

            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            if (bridge == null)
            {
                throw new ArgumentNullException(nameof(bridge));
            }

            ExecuteActorPresentationRelease(command, definition, endpoint, runtimeState, bridge, facts, snapshots);
            ExecuteActorAttributeRelease(command, definition, endpoint, runtimeState, bridge, facts, snapshots);
            var completedIdentity = ExecuteActorParticipationExit(command, definition, endpoint, runtimeState, bridge, sessionActorRuntimeStore, facts, snapshots);

            return new ActivityExitActorTeardownResult(
                true,
                completedIdentity,
                "activity_exit_actor_teardown_completed");
        }

        public static void ExecuteActorPresentationRelease(
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
                        DebugUtility.LogVerbose(typeof(ActivityExitActorTeardownStage),
                            $"event='ActorPresentationRetained' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' actorScope='{ResolveActorScopeLabel(state.ActorInstanceRuntimeId)}' componentKind='ActorPresentation' componentLifetimePolicy='{policy}' releaseTrigger='{rail}' releaseDecision='Retain' reason='policy_mismatch' source='{command.Source}' reason='{command.Reason}'.",
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
                        DebugUtility.Log(typeof(ActivityExitActorTeardownStage),
                            $"event='ActorPresentationReleaseFailed' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' reason='{releaseResult.ReasonCode}' source='{command.Source}' reasonDetail='{command.Reason}'.",
                            DebugUtility.Colors.Error);
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorPresentationRelease] Release failed actorId='{state.ActorId}' reason='{releaseResult.ReasonCode}'.");
                    }

                    bool keptBound = policy == ActorPresentationReleasePolicy.KeepBound;
                    var releasedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleased, entrySequence);
                    endpoint.SetCurrentIdentity(releasedIdentity, SessionActivityStage.ActorPresentationReleased);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleased, releasedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation released actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' policy='{policy}' rail='{rail}' keptBound='{keptBound}' mode='{(keptBound ? "Retained" : "Released")}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_released", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation released actorId='{state.ActorId}' policy='{policy}' rail='{rail}' keptBound='{keptBound}'.");
                    DebugUtility.Log(typeof(ActivityExitActorTeardownStage),
                        $"event='ActorPresentationReleased' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' actorScope='{ResolveActorScopeLabel(state.ActorInstanceRuntimeId)}' componentKind='ActorPresentation' componentLifetimePolicy='{policy}' releaseTrigger='{rail}' releaseDecision='{(keptBound ? "Retain" : "Release")}' source='{command.Source}' reason='{command.Reason}'.",
                        DebugUtility.Colors.Success);

                    if (keptBound)
                    {
                        if (rail == SessionActivityPipeline.ActorPresentationReleaseRail.BeforeRematerialization)
                        {
                            var failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseFailed, entrySequence);
                            endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationReleaseFailed);
                            endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' reason='keep_bound_blocks_rematerialization'.");
                            endpoint.EmitSnapshot(snapshots, "actor_presentation_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' reason='keep_bound_blocks_rematerialization'.");
                            DebugUtility.Log(typeof(ActivityExitActorTeardownStage),
                                $"event='ActorPresentationReleaseFailed' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' reason='keep_bound_blocks_rematerialization' source='{command.Source}' reasonDetail='{command.Reason}'.",
                                DebugUtility.Colors.Error);
                            throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorPresentationRelease] KeepBound blocks rematerialization actorId='{state.ActorId}'.");
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

        public static void ExecuteActorAttributeRelease(
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
                        DebugUtility.Log(typeof(ActivityExitActorTeardownStage),
                            $"event='ActorAttributeInitialStateResetFailed' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{actorId}' actorInstanceRuntimeId='{capabilityState.ActorInstanceRuntimeId}' resetIntent='{initialStateResetIntent}' resetStateProfile='{initialStateResetProfile}' resetProfileSource='entry_initialize_initial_state' outcomeReason='{initialStateResetResult.Reason}' source='{command.Source}' reason='{command.Reason}'.",
                            DebugUtility.Colors.Error);
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorAttributeInitialStateReset] Initial-state reset failed actorId='{actorId}' reason='{initialStateResetResult.Reason}'.");
                    }

                    if (initialStateResetResult.Rejected || initialStateResetResult.Failed)
                    {
                        failedCount += 1;
                        var failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeReleaseFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute initial-state reset failed actorId='{actorId}' resetIntent='{initialStateResetIntent}' resetStateProfile='{initialStateResetProfile}' reason='{initialStateResetResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_initial_state_reset_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute initial-state reset failed actorId='{actorId}' reason='{initialStateResetResult.Reason}'.");
                        DebugUtility.Log(typeof(ActivityExitActorTeardownStage),
                            $"event='ActorAttributeInitialStateResetFailed' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{actorId}' actorInstanceRuntimeId='{capabilityState.ActorInstanceRuntimeId}' resetIntent='{initialStateResetIntent}' resetStateProfile='{initialStateResetProfile}' resetProfileSource='entry_initialize_initial_state' outcomeReason='{initialStateResetResult.Reason}' source='{command.Source}' reason='{command.Reason}'.",
                            DebugUtility.Colors.Error);
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorAttributeInitialStateReset] Initial-state reset failed actorId='{actorId}' reason='{initialStateResetResult.Reason}'.");
                    }

                    if (initialStateResetResult.SkippedNoContent)
                    {
                        initialStateResetSkippedCount += 1;
                    }
                    else
                    {
                        initialStateResetAppliedCount += 1;
                    }

                    DebugUtility.Log(typeof(ActivityExitActorTeardownStage),
                        $"event='{(initialStateResetResult.SkippedNoContent ? "ActorAttributeInitialStateResetSkipped" : "ActorAttributeInitialStateResetApplied")}' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{actorId}' actorInstanceRuntimeId='{capabilityState.ActorInstanceRuntimeId}' resetIntent='{initialStateResetIntent}' resetStateProfile='{initialStateResetProfile}' resetProfileSource='entry_initialize_initial_state' resetAttributeCount='{initialStateResetResult.ResetAttributeCount}' outcomeReason='{initialStateResetResult.Reason}' source='{command.Source}' reason='{command.Reason}'.",
                        initialStateResetResult.SkippedNoContent ? DebugUtility.Colors.Info : DebugUtility.Colors.Success);

                    if (!capabilityState.Endpoint.TryRelease(startedIdentity, out var releaseResult))
                    {
                        failedCount += 1;
                        var failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeReleaseFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorAttributeRelease] Release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                    }

                    if (releaseResult.Rejected || releaseResult.Failed)
                    {
                        failedCount += 1;
                        var failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeReleaseFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorAttributeRelease] Release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
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
                    DebugUtility.Log(typeof(ActivityExitActorTeardownStage),
                        $"event='{eventName}' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{actorId}' actorInstanceRuntimeId='{capabilityState.ActorInstanceRuntimeId}' actorScope='{ResolveActorScopeLabel(capabilityState.ActorInstanceRuntimeId)}' componentKind='ActorAttribute' componentScope='ActivityScoped' componentLifetimePolicy='ActorAttributeEndpointRelease' releaseTrigger='{command.ReleaseRail}' releaseDecision='{(releaseResult.SkippedNoContent ? "Skip" : "Release")}' outcomeReason='{releaseResult.Reason}' releasedAttributeCount='{releaseResult.ReleasedAttributeCount}' source='{command.Source}' reason='{command.Reason}'.",
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

        public static SessionActivityIdentity ExecuteActorParticipationExit(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            ActivityActorExitRuntimeState runtimeState,
            IActivityExitActorTeardownRuntimeBridge bridge,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            var boundaryCommand = PrepareActorParticipationExitBoundaryCommand(command, definition, endpoint);
            var boundaryResult = ExecuteActorParticipationExitBoundary(
                boundaryCommand,
                runtimeState,
                bridge,
                sessionActorRuntimeStore,
                endpoint,
                facts,
                snapshots);
            return boundaryResult.CompletedIdentity;
        }

        private static ActivityActorParticipationExitBoundaryCommand PrepareActorParticipationExitBoundaryCommand(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint)
        {
            int entrySequence = command.EntrySequence;
            var lifetimeTrigger = ResolveLifetimeTrigger(command.ReleaseRail);
            var startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitStarted, entrySequence);
            return new ActivityActorParticipationExitBoundaryCommand(command, definition, startedIdentity, lifetimeTrigger);
        }

        private static ActivityActorParticipationExitBoundaryResult ExecuteActorParticipationExitBoundary(
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            ActivityActorExitRuntimeState runtimeState,
            IActivityExitActorTeardownRuntimeBridge bridge,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!boundaryCommand.IsValid)
            {
                throw new InvalidOperationException("ActivityActorParticipationExitBoundaryCommand is invalid.");
            }

            var command = boundaryCommand.Command;
            var definition = boundaryCommand.Definition;
            var startedIdentity = boundaryCommand.StartedIdentity;
            int entrySequence = command.EntrySequence;

            EmitActorParticipationExitStarted(boundaryCommand, endpoint, facts, snapshots);

            var exitResult = ExecuteActorParticipationExitTechnicalStep(boundaryCommand, runtimeState);

            List<PlayerActorIdentityRecord> exitedPlayerActors = new();
            HashSet<ActorInstanceRuntimeId> actorLifetimeDecisionRuntimeIds = new();
            ApplyActorParticipationExitResults(
                boundaryCommand,
                runtimeState,
                endpoint,
                facts,
                snapshots,
                exitResult,
                exitedPlayerActors,
                actorLifetimeDecisionRuntimeIds);

            EmitRouteExitSessionActorLifetimeDecisions(
                command,
                endpoint,
                sessionActorRuntimeStore,
                facts,
                startedIdentity,
                actorLifetimeDecisionRuntimeIds);

            if (exitedPlayerActors.Count > 0)
            {
                ExecutePlayerActorParticipationExit(command, definition, endpoint, bridge, facts, snapshots, startedIdentity, exitedPlayerActors);
            }

            int total = exitResult.Total;
            int exited = exitResult.Exited;
            int skipped = exitResult.Skipped;
            int failed = exitResult.Failed;
            if (exited == 0)
            {
                skipped += 1;
                EmitActorParticipationExitSkippedForNoExitedActors(command, definition, endpoint, facts, snapshots, entrySequence);
            }

            var completedIdentity = EmitActorParticipationExitCompleted(
                boundaryCommand,
                endpoint,
                facts,
                snapshots,
                total,
                exited,
                skipped,
                failed);

            return new ActivityActorParticipationExitBoundaryResult(
                completedIdentity,
                total,
                exited,
                skipped,
                failed,
                exitedPlayerActors,
                actorLifetimeDecisionRuntimeIds);
        }

        private static ActorParticipationExitResult ExecuteActorParticipationExitTechnicalStep(
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            ActivityActorExitRuntimeState runtimeState)
        {
            var command = boundaryCommand.Command;
            var definition = boundaryCommand.Definition;
            var startedIdentity = boundaryCommand.StartedIdentity;
            int entrySequence = command.EntrySequence;

            var feedResult = runtimeState.GetActorInventoryFeedForExit(startedIdentity, command.Source, command.Reason);
            ActorParticipationExitCommand exitCommand = new(
                startedIdentity,
                definition.ActivityId,
                entrySequence,
                feedResult,
                command.Source,
                command.Reason);
            return runtimeState.ExecuteActorParticipationExit(exitCommand);
        }

        private static void EmitActorParticipationExitStarted(
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            var command = boundaryCommand.Command;
            var definition = boundaryCommand.Definition;
            var startedIdentity = boundaryCommand.StartedIdentity;

            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorParticipationExitStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit started mode='inventory_feed'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit started.");
            DebugUtility.LogVerbose(
                typeof(ActivityExitActorTeardownStage),
                $"event='ActivityParticipationExitStarted' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{command.EntrySequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void ApplyActorParticipationExitResults(
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            ActivityActorExitRuntimeState runtimeState,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            ActorParticipationExitResult exitResult,
            List<PlayerActorIdentityRecord> exitedPlayerActors,
            HashSet<ActorInstanceRuntimeId> actorLifetimeDecisionRuntimeIds)
        {
            var command = boundaryCommand.Command;
            var startedIdentity = boundaryCommand.StartedIdentity;
            int entrySequence = command.EntrySequence;

            for (int index = 0; index < exitResult.ActorResults.Count; index++)
            {
                var actorResult = exitResult.ActorResults[index];
                ApplyActorParticipationExitActorResult(
                    boundaryCommand,
                    runtimeState,
                    endpoint,
                    facts,
                    snapshots,
                    startedIdentity,
                    entrySequence,
                    actorResult,
                    exitedPlayerActors,
                    actorLifetimeDecisionRuntimeIds);
            }
        }

        private static void ApplyActorParticipationExitActorResult(
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            ActivityActorExitRuntimeState runtimeState,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity startedIdentity,
            int entrySequence,
            ActorParticipationExitActorResult actorResult,
            List<PlayerActorIdentityRecord> exitedPlayerActors,
            HashSet<ActorInstanceRuntimeId> actorLifetimeDecisionRuntimeIds)
        {
            if (!actorResult.IsValid)
            {
                return;
            }

            if (actorResult.IsSkipped)
            {
                HandleActorParticipationExitSkippedResult(
                    endpoint,
                    facts,
                    snapshots,
                    boundaryCommand,
                    entrySequence,
                    actorResult);
                return;
            }

            if (actorResult.IsFailed)
            {
                HandleActorParticipationExitFailedResult(
                    endpoint,
                    facts,
                    snapshots,
                    boundaryCommand,
                    entrySequence,
                    actorResult);
                return;
            }

            ApplyActorParticipationExitSucceededResult(
                boundaryCommand,
                runtimeState,
                endpoint,
                facts,
                snapshots,
                startedIdentity,
                entrySequence,
                actorResult,
                exitedPlayerActors,
                actorLifetimeDecisionRuntimeIds);
        }

        private static void HandleActorParticipationExitSkippedResult(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            int entrySequence,
            ActorParticipationExitActorResult actorResult)
        {
            var command = boundaryCommand.Command;
            var definition = boundaryCommand.Definition;
            var instance = actorResult.Instance;
            EmitActorParticipationExitSkipped(
                endpoint,
                facts,
                snapshots,
                definition,
                command,
                entrySequence,
                instance,
                actorResult);
        }

        private static void HandleActorParticipationExitFailedResult(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            int entrySequence,
            ActorParticipationExitActorResult actorResult)
        {
            var command = boundaryCommand.Command;
            var definition = boundaryCommand.Definition;
            var instance = actorResult.Instance;

            EmitActorParticipationExitFailed(
                endpoint,
                facts,
                snapshots,
                definition,
                command,
                entrySequence,
                instance,
                actorResult);

            throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorParticipationExit] failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
        }

        private static void ApplyActorParticipationExitSucceededResult(
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            ActivityActorExitRuntimeState runtimeState,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity startedIdentity,
            int entrySequence,
            ActorParticipationExitActorResult actorResult,
            List<PlayerActorIdentityRecord> exitedPlayerActors,
            HashSet<ActorInstanceRuntimeId> actorLifetimeDecisionRuntimeIds)
        {
            var command = boundaryCommand.Command;
            var definition = boundaryCommand.Definition;
            var instance = actorResult.Instance;
            var actorInstanceRuntimeId = instance.ActorInstanceRuntimeId;
            if (!actorInstanceRuntimeId.IsValid)
            {
                EmitActorParticipationExitFailed(
                    endpoint,
                    facts,
                    snapshots,
                    definition,
                    command,
                    entrySequence,
                    instance,
                    actorResult,
                    "actor_instance_runtime_id_invalid");
                throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorParticipationExit] runtime actor instance id invalid actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}'.");
            }

            ApplyActorParticipationRuntimeCleanup(
                runtimeState,
                actorInstanceRuntimeId,
                definition.ActivityId,
                entrySequence,
                command.Source,
                command.Reason);

            EmitActorParticipationExitExited(endpoint, facts, snapshots, definition, command, startedIdentity, instance);

            ApplyActorParticipationExitLifetimeDecision(
                boundaryCommand,
                endpoint,
                facts,
                instance,
                startedIdentity,
                actorInstanceRuntimeId,
                actorLifetimeDecisionRuntimeIds);

            ResolveActorParticipationExitPlayerBinding(
                runtimeState,
                actorResult,
                instance,
                startedIdentity,
                definition.ActivityId,
                entrySequence,
                exitedPlayerActors);
        }

        private static void ApplyActorParticipationRuntimeCleanup(
            ActivityActorExitRuntimeState runtimeState,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string activityId,
            int entrySequence,
            string source,
            string reason)
        {
            runtimeState.RemoveActiveActorParticipation(actorInstanceRuntimeId, activityId, entrySequence, source, reason);
        }

        private static void ApplyActorParticipationExitLifetimeDecision(
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            ActorInstanceRecord instance,
            SessionActivityIdentity startedIdentity,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            HashSet<ActorInstanceRuntimeId> actorLifetimeDecisionRuntimeIds)
        {
            var command = boundaryCommand.Command;
            var lifetimeDecision = ResolveActorParticipationExitLifetimeDecision(
                command,
                startedIdentity,
                instance,
                boundaryCommand.LifetimeTrigger);
            if (lifetimeDecision.Decision == ActorLifetimeDecision.Release)
            {
                ActorReleaseContributionStage.ExecuteOrFail(new ActorReleaseContributionStageCommand(
                    startedIdentity,
                    new ActorId(instance.ActorId),
                    instance.ActorInstanceRuntimeId,
                    instance.Kind,
                    instance.Role,
                    instance.Scope,
                    instance.CapabilitySurface,
                    instance.ComponentBasePath,
                    boundaryCommand.LifetimeTrigger,
                    command.Source,
                    command.Reason));
            }

            EmitActorLifetimeDecision(endpoint, facts, lifetimeDecision);
            if (actorInstanceRuntimeId.IsValid)
            {
                actorLifetimeDecisionRuntimeIds.Add(actorInstanceRuntimeId);
            }
        }

        private static void ResolveActorParticipationExitPlayerBinding(
            ActivityActorExitRuntimeState runtimeState,
            ActorParticipationExitActorResult actorResult,
            ActorInstanceRecord instance,
            SessionActivityIdentity startedIdentity,
            string activityId,
            int entrySequence,
            List<PlayerActorIdentityRecord> exitedPlayerActors)
        {
            if (TryResolvePlayerActorIdentityForParticipationExit(
                runtimeState,
                actorResult,
                instance,
                out var participantBinding,
                out string participantBindingFailureReason))
            {
                PlayerActorIdentityRecord resolvedIdentity = new(
                    startedIdentity,
                    participantBinding,
                    actorResult.PlayerActorId);
                exitedPlayerActors.Add(resolvedIdentity);
                return;
            }

            if (actorResult.HasResolvedPlayerIdentity)
            {
                throw new InvalidOperationException(
                    $"[FATAL][ActivityExitActorTeardownStage][ActorParticipationExit] player_participant_binding_resolution_failed reason='{participantBindingFailureReason}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}' playerActorId='{actorResult.PlayerActorId}' playerSlotId='{actorResult.PlayerSlotId}' activityId='{activityId}' entrySequence='{entrySequence}'.");
            }
        }

        private static void EmitActorParticipationExitSkipped(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityDefinition definition,
            ActivityExitActorTeardownCommand command,
            int entrySequence,
            ActorInstanceRecord instance,
            ActorParticipationExitActorResult actorResult)
        {
            var skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitSkipped, entrySequence);
            endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationExitSkipped);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
            DebugUtility.LogVerbose(typeof(ActivityExitActorTeardownStage),
                $"event='ActorParticipationExitSkipped' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}' skipKind='{actorResult.SkipOrFailureKind}' skipReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Warning);
        }

        private static void EmitActorParticipationExitFailed(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityDefinition definition,
            ActivityExitActorTeardownCommand command,
            int entrySequence,
            ActorInstanceRecord instance,
            ActorParticipationExitActorResult actorResult,
            string reasonCodeOverride = null)
        {
            string reasonCode = reasonCodeOverride.TrimToOrDefault(actorResult.ReasonCode);
            var failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitFailed, entrySequence);
            endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorParticipationExitFailed);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit failed actorId='{instance.ActorId}' reason='{reasonCode}'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit failed actorId='{instance.ActorId}' reason='{reasonCode}'.");
            DebugUtility.Log(typeof(ActivityExitActorTeardownStage),
                $"event='ActorParticipationExitFailed' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}' failureKind='{reasonCode}' failureReason='{reasonCode}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Error);
        }

        private static void EmitActorParticipationExitExited(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityDefinition definition,
            ActivityExitActorTeardownCommand command,
            SessionActivityIdentity startedIdentity,
            ActorInstanceRecord instance)
        {
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExited, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exited actorId='{instance.ActorId}' actorRole='{instance.Role}'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exited", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exited actorId='{instance.ActorId}'.");
            DebugUtility.LogVerbose(
                typeof(ActivityExitActorTeardownStage),
                $"event='ActivityParticipationExited' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{command.EntrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceRuntimeId}' actorScope='{instance.Scope}' resultKind='Exited' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static bool TryResolvePlayerActorIdentityForParticipationExit(
            ActivityActorExitRuntimeState runtimeState,
            ActorParticipationExitActorResult actorResult,
            ActorInstanceRecord instance,
            out PlayerActivityParticipantBinding binding,
            out string failureReason)
        {
            binding = default;
            failureReason = "unknown";

            if (!actorResult.HasResolvedPlayerIdentity)
            {
                failureReason = "player_identity_not_resolved";
                return false;
            }

            if (!instance.IsValid)
            {
                failureReason = "actor_instance_invalid";
                return false;
            }

            ActorId actorId = new(instance.ActorId);
            if (!actorId.IsValid)
            {
                failureReason = "actor_id_missing_in_actor_participation_record";
                return false;
            }

            var participationContext = runtimeState.CurrentActivityParticipationContext;
            if (participationContext is { IsValid: true, Participants: not null })
            {
                for (int index = 0; index < participationContext.Participants.Count; index++)
                {
                    var candidate = participationContext.Participants[index];
                    if (!candidate.IsValid || !candidate.RequiresPlayerActor || !candidate.ActorId.IsValid)
                    {
                        continue;
                    }

                    if (candidate.ActorId == actorId)
                    {
                        binding = candidate;
                        failureReason = string.Empty;
                        return true;
                    }
                }
            }

            failureReason = "activity_participant_binding_missing_for_actor_id";
            return false;
        }

        private static ActivityActorParticipationExitDecisionRecord ResolveActorParticipationExitLifetimeDecision(
            ActivityExitActorTeardownCommand command,
            SessionActivityIdentity identity,
            ActorInstanceRecord instance,
            ActorLifetimeTrigger trigger)
        {
            var decision = ActorLifetimePolicyRuntime.ResolveDecision(instance.Scope, trigger);
            return BuildActorParticipationExitDecisionRecord(command, identity, instance, trigger, decision);
        }

        private static ActivityActorParticipationExitDecisionRecord ResolveActorParticipationExitLifetimeDecision(
            ActivityExitActorTeardownCommand command,
            SessionActivityIdentity identity,
            SessionActorRuntimeEntry entry,
            ActorLifetimeTrigger trigger)
        {
            var decision = ActorLifetimePolicyRuntime.ResolveDecision(entry.ActorScope, trigger);
            return BuildActorParticipationExitDecisionRecord(command, identity, entry, trigger, decision);
        }

        private static ActivityActorParticipationExitDecisionRecord BuildActorParticipationExitDecisionRecord(
            ActivityExitActorTeardownCommand command,
            SessionActivityIdentity identity,
            ActorInstanceRecord instance,
            ActorLifetimeTrigger trigger,
            ActorLifetimeDecision decision)
        {
            return new ActivityActorParticipationExitDecisionRecord(
                identity,
                command.Identity.ActivityId,
                command.EntrySequence,
                instance.ActorId,
                instance.ActorInstanceRuntimeId,
                instance.Scope,
                trigger,
                decision,
                command.Source,
                command.Reason);
        }

        private static ActivityActorParticipationExitDecisionRecord BuildActorParticipationExitDecisionRecord(
            ActivityExitActorTeardownCommand command,
            SessionActivityIdentity identity,
            SessionActorRuntimeEntry entry,
            ActorLifetimeTrigger trigger,
            ActorLifetimeDecision decision)
        {
            return new ActivityActorParticipationExitDecisionRecord(
                identity,
                command.Identity.ActivityId,
                command.EntrySequence,
                entry.ActorId.Value,
                entry.ActorInstanceRuntimeId,
                entry.ActorScope,
                trigger,
                decision,
                command.Source,
                command.Reason);
        }

        private static void EmitActorParticipationExitSkippedForNoExitedActors(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int entrySequence)
        {
            var skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitSkipped, entrySequence);
            endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationExitSkipped);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped reason='no_exited_actors'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped reason='no_exited_actors'.");
            DebugUtility.LogVerbose(typeof(ActivityExitActorTeardownStage), $"event='ActorParticipationExitSkipped' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='<none>' actorInstanceRuntimeId='<none>' skipKind='aggregate' skipReason='no_exited_actors' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Warning);
        }

        private static SessionActivityIdentity EmitActorParticipationExitCompleted(
            ActivityActorParticipationExitBoundaryCommand boundaryCommand,
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            int total,
            int exited,
            int skipped,
            int failed)
        {
            var command = boundaryCommand.Command;
            var definition = boundaryCommand.Definition;
            int entrySequence = command.EntrySequence;
            var completedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitCompleted, entrySequence);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorParticipationExitCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit completed total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit completed total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}'.");
            DebugUtility.LogVerbose(
                typeof(ActivityExitActorTeardownStage),
                $"event='ActivityParticipationExitCompleted' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            return completedIdentity;
        }

        private static void EmitRouteExitSessionActorLifetimeDecisions(
            ActivityExitActorTeardownCommand command,
            IActivityEntryRuntimeBridge endpoint,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            List<SessionActivityFact> facts,
            SessionActivityIdentity identity,
            HashSet<ActorInstanceRuntimeId> alreadyResolvedRuntimeIds)
        {
            if (command.ReleaseRail != SessionActivityPipeline.ActorPresentationReleaseRail.RouteExit ||
                sessionActorRuntimeStore == null ||
                !identity.IsValid)
            {
                return;
            }

            IReadOnlyList<SessionActorRuntimeEntry> sessionActors = sessionActorRuntimeStore.GetEntriesForSession(identity);
            if (sessionActors == null || sessionActors.Count == 0)
            {
                return;
            }

            HashSet<ActorInstanceRuntimeId> resolvedRuntimeIds = alreadyResolvedRuntimeIds ?? new HashSet<ActorInstanceRuntimeId>();
            for (int index = 0; index < sessionActors.Count; index++)
            {
                var entry = sessionActors[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                var runtimeId = entry.ActorInstanceRuntimeId;
                if (!runtimeId.IsValid || resolvedRuntimeIds.Contains(runtimeId))
                {
                    continue;
                }

                var lifetimeDecision = ResolveActorParticipationExitLifetimeDecision(command, identity, entry, ActorLifetimeTrigger.RouteExit);
                EmitActorLifetimeDecision(endpoint, facts, lifetimeDecision);
                resolvedRuntimeIds.Add(runtimeId);
            }
        }

        private static void EmitActorLifetimeDecision(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            ActivityActorParticipationExitDecisionRecord decisionRecord)
        {
            if (!decisionRecord.IsValid)
            {
                throw new InvalidOperationException("ActivityActorParticipationExitDecisionRecord is invalid.");
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActorLifetimeDecisionResolved,
                decisionRecord.Identity,
                decisionRecord.Source,
                decisionRecord.Reason,
                $"'{decisionRecord.ActivityId}' actor lifetime decision resolved actorId='{decisionRecord.ActorId}' actorInstanceRuntimeId='{decisionRecord.ActorInstanceRuntimeId}' actorScope='{decisionRecord.ActorScope}' trigger='{decisionRecord.Trigger}' decision='{decisionRecord.Decision}'.");
            DebugUtility.LogVerbose(
                typeof(ActivityExitActorTeardownStage),
                $"event='ActorLifetimeDecisionResolved' owner='ActivityExitActorTeardownStage' macroLifecycleOwner='SessionActivityPipeline' activityId='{decisionRecord.ActivityId}' entrySequence='{decisionRecord.EntrySequence}' actorId='{decisionRecord.ActorId}' actorInstanceRuntimeId='{decisionRecord.ActorInstanceRuntimeId}' actorScope='{decisionRecord.ActorScope}' trigger='{decisionRecord.Trigger}' decision='{decisionRecord.Decision}' source='{decisionRecord.Source}' reason='{decisionRecord.Reason}'.",
                DebugUtility.Colors.Info);

            if (decisionRecord.Decision == ActorLifetimeDecision.Retain)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActorLifetimeRetained,
                    decisionRecord.Identity,
                    decisionRecord.Source,
                    decisionRecord.Reason,
                    $"'{decisionRecord.ActivityId}' actor lifetime retained actorId='{decisionRecord.ActorId}' actorInstanceRuntimeId='{decisionRecord.ActorInstanceRuntimeId}' actorScope='{decisionRecord.ActorScope}' trigger='{decisionRecord.Trigger}'.");
                return;
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActorLifetimeReleased,
                decisionRecord.Identity,
                decisionRecord.Source,
                decisionRecord.Reason,
                $"'{decisionRecord.ActivityId}' actor lifetime released actorId='{decisionRecord.ActorId}' actorInstanceRuntimeId='{decisionRecord.ActorInstanceRuntimeId}' actorScope='{decisionRecord.ActorScope}' trigger='{decisionRecord.Trigger}'.");
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
        private static ActorLifetimeTrigger ResolveLifetimeTrigger(SessionActivityPipeline.ActorPresentationReleaseRail releaseRail)
        {
            return releaseRail switch
            {
                SessionActivityPipeline.ActorPresentationReleaseRail.ActivityExit => ActorLifetimeTrigger.ActivityExit,
                SessionActivityPipeline.ActorPresentationReleaseRail.RouteExit => ActorLifetimeTrigger.RouteExit,
                _ => throw new InvalidOperationException($"Unsupported ActorPresentationReleaseRail='{releaseRail}' for lifetime decision.")
            };
        }

        private static void ExecutePlayerActorParticipationExit(
            ActivityExitActorTeardownCommand command,
            SessionActivityDefinition definition,
            IActivityEntryRuntimeBridge endpoint,
            IActivityExitActorTeardownRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity stageStartedIdentity,
            IReadOnlyList<PlayerActorIdentityRecord> exitedPlayerActors)
        {
            int entrySequence = command.EntrySequence;
            IReadOnlyList<PlayerActorIdentityRecord> actors = exitedPlayerActors ?? Array.Empty<PlayerActorIdentityRecord>();
            if (actors.Count == 0)
            {
                return;
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorParticipationExitStageStarted,
                stageStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit stage started.");
            endpoint.EmitSnapshot(
                snapshots,
                "player_actor_participation_exit_stage_started",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit stage started.");

            PlayerActorParticipationExitCommand exitCommand = new(stageStartedIdentity, actors, command.Source, command.Reason);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorParticipationExitCommandIssued,
                stageStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit command issued. actors='{actors.Count}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "player_actor_participation_exit_command_issued",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit command issued. actors='{actors.Count}'.");

            IReadOnlyList<PlayerActorParticipationExitRecord> exitRecords = bridge.ExecutePlayerActorParticipationExit(exitCommand, stageStartedIdentity);
            for (int index = 0; index < exitRecords.Count; index++)
            {
                var record = exitRecords[index];
                if (!record.IsValid)
                {
                    throw new InvalidOperationException(
                        $"PlayerActorParticipationExitRecord at index '{index}' is invalid for activity '{definition.ActivityId}'.");
                }
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorParticipationExited,
                stageStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exited. actors='{exitRecords.Count}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "player_actor_participation_exited",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exited. actors='{exitRecords.Count}'.");

            var stageCompletedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.PlayerActorParticipationExitStageCompleted, entrySequence);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorParticipationExitStageCompleted,
                stageCompletedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit stage completed.");
            endpoint.SetCurrentIdentity(stageCompletedIdentity, SessionActivityStage.PlayerActorParticipationExitStageCompleted);
            endpoint.EmitSnapshot(
                snapshots,
                "player_actor_participation_exit_stage_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit stage completed.");
        }
    }
}
