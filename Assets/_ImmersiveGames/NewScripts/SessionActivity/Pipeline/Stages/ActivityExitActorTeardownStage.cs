using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;
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
            ActorInstanceId targetActorInstanceRuntimeId = default)
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
        public ActorInstanceId TargetActorInstanceRuntimeId { get; }

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
            Reason = Normalize(reason);
        }

        public bool Completed { get; }
        public SessionActivityIdentity Identity { get; }
        public string Reason { get; }
        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    internal interface IActivityExitActorTeardownRuntimeBridge
    {
        ActorPresentationResult ReleaseActorPresentation(ActorPresentationRuntimeHandle handle, string source, string reason);
        void ClearActorPresentationRegistryHandle(SessionActivityIdentity identity, SessionActivityPipeline.ActorPresentationCapabilityState state);

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
            SessionActivityIdentity completedIdentity = ExecuteActorParticipationExit(command, definition, endpoint, runtimeState, bridge, sessionActorRuntimeStore, facts, snapshots);

            return new ActivityExitActorTeardownResult(
                completed: true,
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
            SessionActivityPipeline.ActorPresentationReleaseRail rail = command.ReleaseRail;
            ActorInstanceId targetActorInstanceRuntimeId = command.TargetActorInstanceRuntimeId;

            SessionActivityIdentity startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorPresentationReleaseStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release started rail='{rail}' target='{(targetActorInstanceRuntimeId.IsValid ? targetActorInstanceRuntimeId.Value : "all")}'.");
            endpoint.EmitSnapshot(snapshots, "actor_presentation_release_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release started rail='{rail}'.");
            IReadOnlyList<SessionActivityPipeline.ActorPresentationCapabilityState> activeStates = runtimeState.ResolveActiveActorPresentationStates(targetActorInstanceRuntimeId);
            if (activeStates == null || activeStates.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseSkipped, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationReleaseSkipped);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release skipped reason='no_active_actor_presentation_handle' rail='{rail}'.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_release_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release skipped reason='no_active_actor_presentation_handle' rail='{rail}'.");
            }
            else
            {
                for (int index = 0; index < activeStates.Count; index++)
                {
                    SessionActivityPipeline.ActorPresentationCapabilityState state = activeStates[index];
                    ActorPresentationRuntimeHandle handle = state.RuntimeHandle;
                    if (!handle.IsValid)
                    {
                        continue;
                    }

                    ActorPresentationReleasePolicy policy = handle.ResolvedPlan.ReleasePolicy;
                    bool shouldRelease = rail switch
                    {
                        SessionActivityPipeline.ActorPresentationReleaseRail.BeforeRematerialization => true,
                        SessionActivityPipeline.ActorPresentationReleaseRail.ActivityExit => policy == ActorPresentationReleasePolicy.ReleaseOnActivityExit,
                        SessionActivityPipeline.ActorPresentationReleaseRail.RouteExit => policy == ActorPresentationReleasePolicy.ReleaseOnRouteExit,
                        _ => false,
                    };

                    if (!shouldRelease)
                    {
                        SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseSkipped, entrySequence);
                        endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationReleaseSkipped);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release skipped actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' reason='policy_mismatch' policy='{policy}' rail='{rail}' mode='Retained'.");
                        DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ComponentLifetime] event='ActorPresentationRetained' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' actorScope='{ResolveActorScopeLabel(state.ActorInstanceRuntimeId)}' componentKind='ActorPresentation' componentLifetimePolicy='{policy}' releaseTrigger='{rail}' releaseDecision='Retain' reason='policy_mismatch' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);
                        continue;
                    }

                    ActorPresentationResult releaseResult = bridge.ReleaseActorPresentation(handle, command.Source, command.Reason);
                    if (!releaseResult.IsSuccess)
                    {
                        SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationReleaseFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' reason='{releaseResult.ReasonCode}' rail='{rail}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_presentation_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' reason='{releaseResult.ReasonCode}' rail='{rail}'.");
                        DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][ActorPresentation] event='ActorPresentationReleaseFailed' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' reason='{releaseResult.ReasonCode}' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Error);
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorPresentationRelease] Release failed actorId='{state.ActorId}' reason='{releaseResult.ReasonCode}'.");
                    }

                    bool keptBound = policy == ActorPresentationReleasePolicy.KeepBound;
                    SessionActivityIdentity releasedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleased, entrySequence);
                    endpoint.SetCurrentIdentity(releasedIdentity, SessionActivityStage.ActorPresentationReleased);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleased, releasedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation released actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' policy='{policy}' rail='{rail}' keptBound='{keptBound}' mode='{(keptBound ? "Retained" : "Released")}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_released", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation released actorId='{state.ActorId}' policy='{policy}' rail='{rail}' keptBound='{keptBound}'.");
                    DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ComponentLifetime] event='ActorPresentationReleased' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' actorScope='{ResolveActorScopeLabel(state.ActorInstanceRuntimeId)}' componentKind='ActorPresentation' componentLifetimePolicy='{policy}' releaseTrigger='{rail}' releaseDecision='{(keptBound ? "Retain" : "Release")}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

                    if (keptBound)
                    {
                        if (rail == SessionActivityPipeline.ActorPresentationReleaseRail.BeforeRematerialization)
                        {
                            SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseFailed, entrySequence);
                            endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationReleaseFailed);
                            endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' reason='keep_bound_blocks_rematerialization'.");
                            endpoint.EmitSnapshot(snapshots, "actor_presentation_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release failed actorId='{state.ActorId}' reason='keep_bound_blocks_rematerialization'.");
                            DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][ActorPresentation] event='ActorPresentationReleaseFailed' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' reason='keep_bound_blocks_rematerialization' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Error);
                            throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorPresentationRelease] KeepBound blocks rematerialization actorId='{state.ActorId}'.");
                        }

                        continue;
                    }

                    runtimeState.RemoveActiveActorPresentation(state.ActorInstanceRuntimeId, definition.ActivityId, entrySequence, command.Source, command.Reason);
                    bridge.ClearActorPresentationRegistryHandle(startedIdentity, state);
                }
            }

            SessionActivityIdentity completedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseCompleted, entrySequence);
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
            SessionActivityIdentity startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorAttributeReleaseStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release started.");
            endpoint.EmitSnapshot(snapshots, "actor_attribute_release_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release started.");
            IReadOnlyList<SessionActivityPipeline.ActorAttributeCapabilityState> activeStates = runtimeState.ResolveActiveActorAttributeStates();
            int totalCount = activeStates?.Count ?? 0;
            int releasedCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            if (activeStates == null || activeStates.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseSkipped, entrySequence);
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
                    SessionActivityPipeline.ActorAttributeCapabilityState capabilityState = activeStates[index];
                    if (!capabilityState.IsValid || capabilityState.Endpoint == null)
                    {
                        continue;
                    }

                    string actorId = capabilityState.ActorId;
                    if (!capabilityState.Endpoint.TryRelease(startedIdentity, out ActorAttributeReleaseResult releaseResult))
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeReleaseFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorAttributeRelease] Release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                    }

                    if (releaseResult.Rejected || releaseResult.Failed)
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeReleaseFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_release_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorAttributeRelease] Release failed actorId='{actorId}' reason='{releaseResult.Reason}'.");
                    }

                    SessionActivityStage stage = releaseResult.SkippedNoContent
                        ? SessionActivityStage.ActorAttributeReleaseSkipped
                        : SessionActivityStage.ActorAttributeReleased;
                    SessionActivityFactKind factKind = releaseResult.SkippedNoContent
                        ? SessionActivityFactKind.ActorAttributeReleaseSkipped
                        : SessionActivityFactKind.ActorAttributeReleased;
                    string snapshotKind = releaseResult.SkippedNoContent ? "actor_attribute_release_skipped" : "actor_attribute_released";
                    string eventName = releaseResult.SkippedNoContent ? "ActorAttributeReleaseSkipped" : "ActorAttributeReleased";

                    SessionActivityIdentity outcomeIdentity = endpoint.BuildIdentity(definition, stage, entrySequence);
                    endpoint.SetCurrentIdentity(outcomeIdentity, stage);
                    endpoint.EmitFact(facts, factKind, outcomeIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release outcome actorId='{actorId}' reason='{releaseResult.Reason}' releasedAttributeCount='{releaseResult.ReleasedAttributeCount}'.");
                    endpoint.EmitSnapshot(snapshots, snapshotKind, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release outcome actorId='{actorId}' reason='{releaseResult.Reason}' releasedAttributeCount='{releaseResult.ReleasedAttributeCount}'.");
                    DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ComponentLifetime] event='{eventName}' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{actorId}' actorInstanceRuntimeId='{capabilityState.ActorInstanceRuntimeId}' actorScope='{ResolveActorScopeLabel(capabilityState.ActorInstanceRuntimeId)}' componentKind='ActorAttribute' componentScope='ActivityScoped' componentLifetimePolicy='ActorAttributeEndpointRelease' releaseTrigger='{command.ReleaseRail}' releaseDecision='{(releaseResult.SkippedNoContent ? "Skip" : "Release")}' outcomeReason='{releaseResult.Reason}' releasedAttributeCount='{releaseResult.ReleasedAttributeCount}' source='{command.Source}' reason='{command.Reason}'.", releaseResult.SkippedNoContent ? DebugUtility.Colors.Info : DebugUtility.Colors.Success);

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
                    SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseSkipped, entrySequence);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeReleaseSkipped);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release skipped reason='no_active_attribute_capability'.");
                    endpoint.EmitSnapshot(snapshots, "actor_attribute_release_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release skipped reason='no_active_attribute_capability'.");
                }
            }

            SessionActivityIdentity completedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseCompleted, entrySequence);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorAttributeReleaseCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release completed total='{totalCount}' released='{releasedCount}' skipped='{skippedCount}' failed='{failedCount}'.");
            endpoint.EmitSnapshot(snapshots, "actor_attribute_release_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release completed total='{totalCount}' released='{releasedCount}' skipped='{skippedCount}' failed='{failedCount}'.");
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
            int entrySequence = command.EntrySequence;
            ActorLifetimeTrigger lifetimeTrigger = ResolveLifetimeTrigger(command.ReleaseRail);
            SessionActivityIdentity startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorParticipationExitStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit started mode='inventory_feed'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit started.");
            ActorInventoryFeedResult feedResult = runtimeState.GetActorInventoryFeedForExit(startedIdentity, command.Source, command.Reason);
            ActorParticipationExitCommand exitCommand = new(
                startedIdentity,
                definition.ActivityId,
                entrySequence,
                feedResult,
                command.Source,
                command.Reason);
            ActorParticipationExitResult exitResult = runtimeState.ExecuteActorParticipationExit(exitCommand);
            int total = exitResult.Total;
            int exited = exitResult.Exited;
            int skipped = exitResult.Skipped;
            int failed = exitResult.Failed;
            List<PlayerActorIdentityRecord> exitedPlayerActors = new();
            HashSet<string> actorLifetimeDecisionRuntimeIds = new(StringComparer.Ordinal);

            for (int index = 0; index < exitResult.ActorResults.Count; index++)
            {
                ActorParticipationExitActorResult actorResult = exitResult.ActorResults[index];
                if (!actorResult.IsValid)
                {
                    continue;
                }

                ActorInstanceRecord instance = actorResult.Instance;
                if (actorResult.IsSkipped)
                {
                    SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitSkipped, entrySequence);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationExitSkipped);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_participation_exit_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                    DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorParticipationExitSkipped' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' skipKind='{actorResult.SkipOrFailureKind}' skipReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Warning);
                    continue;
                }

                if (actorResult.IsFailed)
                {
                    SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitFailed, entrySequence);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorParticipationExitFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_participation_exit_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                    DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorParticipationExitFailed' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' failureKind='{actorResult.SkipOrFailureKind}' failureReason='{actorResult.ReasonCode}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Error);
                    throw new InvalidOperationException($"[FATAL][ActivityExitActorTeardownStage][ActorParticipationExit] failed actorId='{instance.ActorId}' reason='{actorResult.ReasonCode}'.");
                }

                runtimeState.RemoveActiveActorParticipation(instance.ActorInstanceId, definition.ActivityId, entrySequence, command.Source, command.Reason);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExited, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exited actorId='{instance.ActorId}' actorRole='{instance.Role}'.");
                endpoint.EmitSnapshot(snapshots, "actor_participation_exited", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exited actorId='{instance.ActorId}'.");
                EmitActorLifetimeDecision(endpoint, facts, startedIdentity, command, instance, lifetimeTrigger);
                if (instance.ActorInstanceId.IsValid)
                {
                    actorLifetimeDecisionRuntimeIds.Add(instance.ActorInstanceId.Value);
                }

                if (runtimeState.TryResolveActivePlayerParticipantBindingForExit(
                        actorResult,
                        instance,
                        out PlayerActivityParticipantBinding participantBinding,
                        out string participantBindingFailureReason))
                {
                    PlayerActorIdentityRecord resolvedIdentity = new(
                        startedIdentity,
                        participantBinding,
                        actorResult.PlayerActorId);
                    exitedPlayerActors.Add(resolvedIdentity);
                }
                else if (actorResult.HasResolvedPlayerIdentity)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][ActivityExitActorTeardownStage][ActorParticipationExit] player_participant_binding_resolution_failed reason='{participantBindingFailureReason}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' playerActorId='{actorResult.PlayerActorId}' playerSlotId='{actorResult.PlayerSlotId}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }
            }

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

            if (exited == 0)
            {
                skipped += 1;
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitSkipped, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorParticipationExitSkipped);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped reason='no_exited_actors'.");
                endpoint.EmitSnapshot(snapshots, "actor_participation_exit_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit skipped reason='no_exited_actors'.");
                DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorParticipationExitSkipped' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='<none>' actorInstanceRuntimeId='<none>' skipKind='aggregate' skipReason='no_exited_actors' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Warning);
            }

            SessionActivityIdentity completedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitCompleted, entrySequence);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorParticipationExitCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit completed total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit completed total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}'.");
            return completedIdentity;
        }

        private static void EmitRouteExitSessionActorLifetimeDecisions(
            ActivityExitActorTeardownCommand command,
            IActivityEntryRuntimeBridge endpoint,
            SessionActorRuntimeStore sessionActorRuntimeStore,
            List<SessionActivityFact> facts,
            SessionActivityIdentity identity,
            HashSet<string> alreadyResolvedRuntimeIds)
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

            HashSet<string> resolvedRuntimeIds = alreadyResolvedRuntimeIds ?? new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < sessionActors.Count; index++)
            {
                SessionActorRuntimeEntry entry = sessionActors[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                string runtimeId = Normalize(entry.ActorInstanceRuntimeId.Value);
                if (string.IsNullOrWhiteSpace(runtimeId) || resolvedRuntimeIds.Contains(runtimeId))
                {
                    continue;
                }

                EmitActorLifetimeDecision(endpoint, facts, identity, command, entry, ActorLifetimeTrigger.RouteExit);
                resolvedRuntimeIds.Add(runtimeId);
            }
        }

        private static void EmitActorLifetimeDecision(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            SessionActivityIdentity identity,
            ActivityExitActorTeardownCommand command,
            SessionActorRuntimeEntry entry,
            ActorLifetimeTrigger trigger)
        {
            ActorLifetimeDecision decision = ActorLifetimePolicy.ResolveDecision(entry.ActorScope, trigger);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActorLifetimeDecisionResolved,
                identity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' actor lifetime decision resolved actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' trigger='{trigger}' decision='{decision}'.");
            DebugUtility.LogVerbose<SessionActivityPipeline>(
                $"[OBS][ActorLifetime] event='ActorLifetimeDecisionResolved' owner='ActivityExitActorTeardownStage' activityId='{command.Identity.ActivityId}' entrySequence='{command.EntrySequence}' actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' trigger='{trigger}' decision='{decision}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (decision == ActorLifetimeDecision.Retain)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActorLifetimeRetained,
                    identity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' actor lifetime retained actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' trigger='{trigger}'.");
                return;
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActorLifetimeReleased,
                identity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' actor lifetime released actorId='{entry.ActorId}' actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}' actorScope='{entry.ActorScope}' trigger='{trigger}'.");
        }

        private static void EmitActorLifetimeDecision(
            IActivityEntryRuntimeBridge endpoint,
            List<SessionActivityFact> facts,
            SessionActivityIdentity identity,
            ActivityExitActorTeardownCommand command,
            ActorInstanceRecord instance,
            ActorLifetimeTrigger trigger)
        {
            ActorLifetimeDecision decision = ActorLifetimePolicy.ResolveDecision(instance.Scope, trigger);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActorLifetimeDecisionResolved,
                identity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' actor lifetime decision resolved actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' actorScope='{instance.Scope}' trigger='{trigger}' decision='{decision}'.");
            DebugUtility.LogVerbose<SessionActivityPipeline>(
                $"[OBS][ActorLifetime] event='ActorLifetimeDecisionResolved' owner='ActivityExitActorTeardownStage' activityId='{command.Identity.ActivityId}' entrySequence='{command.EntrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' actorScope='{instance.Scope}' trigger='{trigger}' decision='{decision}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (decision == ActorLifetimeDecision.Retain)
            {
                endpoint.EmitFact(
                    facts,
                    SessionActivityFactKind.ActorLifetimeRetained,
                    identity,
                    command.Source,
                    command.Reason,
                    $"'{command.Identity.ActivityId}' actor lifetime retained actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' actorScope='{instance.Scope}' trigger='{trigger}'.");
                return;
            }

            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.ActorLifetimeReleased,
                identity,
                command.Source,
                command.Reason,
                $"'{command.Identity.ActivityId}' actor lifetime released actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' actorScope='{instance.Scope}' trigger='{trigger}'.");
        }

        private static string ResolveActorScopeLabel(ActorInstanceId actorInstanceRuntimeId)
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static ActorLifetimeTrigger ResolveLifetimeTrigger(SessionActivityPipeline.ActorPresentationReleaseRail releaseRail)
        {
            return releaseRail switch
            {
                SessionActivityPipeline.ActorPresentationReleaseRail.ActivityExit => ActorLifetimeTrigger.ActivityExit,
                SessionActivityPipeline.ActorPresentationReleaseRail.RouteExit => ActorLifetimeTrigger.RouteExit,
                _ => throw new InvalidOperationException($"Unsupported ActorPresentationReleaseRail='{releaseRail}' for lifetime decision."),
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
                PlayerActorParticipationExitRecord record = exitRecords[index];
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

            SessionActivityIdentity stageCompletedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.PlayerActorParticipationExitStageCompleted, entrySequence);
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
