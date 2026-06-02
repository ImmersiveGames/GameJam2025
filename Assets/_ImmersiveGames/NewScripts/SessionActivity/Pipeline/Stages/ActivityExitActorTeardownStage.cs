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
            SessionActivityDefinition definition,
            SessionActivityCommand command,
            int entrySequence,
            SessionActivityPipeline.ActorPresentationReleaseRail releaseRail,
            ActorInstanceId targetActorInstanceRuntimeId = default)
        {
            Definition = definition;
            Command = command;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            ReleaseRail = releaseRail;
            TargetActorInstanceRuntimeId = targetActorInstanceRuntimeId;
        }

        public SessionActivityDefinition Definition { get; }
        public SessionActivityCommand Command { get; }
        public int EntrySequence { get; }
        public SessionActivityPipeline.ActorPresentationReleaseRail ReleaseRail { get; }
        public ActorInstanceId TargetActorInstanceRuntimeId { get; }

        public string Source => Command.Source;
        public string Reason => Command.Reason;

        public bool IsValid =>
            Definition.IsValid &&
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
            IActivityEntryRuntimeBridge endpoint,
            ActivityActorExitRuntimeState runtimeState,
            IActivityExitActorTeardownRuntimeBridge bridge,
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

            ExecuteActorPresentationRelease(command, endpoint, runtimeState, bridge, facts, snapshots);
            ExecuteActorAttributeRelease(command, endpoint, runtimeState, bridge, facts, snapshots);
            SessionActivityIdentity completedIdentity = ExecuteActorParticipationExit(command, endpoint, runtimeState, bridge, facts, snapshots);

            return new ActivityExitActorTeardownResult(
                completed: true,
                completedIdentity,
                "activity_exit_actor_teardown_completed");
        }

        public static void ExecuteActorPresentationRelease(
            ActivityExitActorTeardownCommand command,
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

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.EntrySequence;
            SessionActivityPipeline.ActorPresentationReleaseRail rail = command.ReleaseRail;
            ActorInstanceId targetActorInstanceRuntimeId = command.TargetActorInstanceRuntimeId;

            SessionActivityIdentity startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorPresentationReleaseStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release started rail='{rail}' target='{(targetActorInstanceRuntimeId.IsValid ? targetActorInstanceRuntimeId.Value : "all")}'.");
            endpoint.EmitSnapshot(snapshots, "actor_presentation_release_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release started rail='{rail}'.");
            DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][ActorPresentation] event='ActorPresentationReleaseStarted' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' rail='{rail}' target='{(targetActorInstanceRuntimeId.IsValid ? targetActorInstanceRuntimeId.Value : "all")}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

            IReadOnlyList<SessionActivityPipeline.ActorPresentationCapabilityState> activeStates = runtimeState.ResolveActiveActorPresentationStates(targetActorInstanceRuntimeId);
            if (activeStates == null || activeStates.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReleaseSkipped, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationReleaseSkipped);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReleaseSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release skipped reason='no_active_actor_presentation_handle' rail='{rail}'.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_release_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation release skipped reason='no_active_actor_presentation_handle' rail='{rail}'.");
                DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][ActorPresentation] event='ActorPresentationReleaseSkipped' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' rail='{rail}' target='{(targetActorInstanceRuntimeId.IsValid ? targetActorInstanceRuntimeId.Value : "all")}' mode='Skipped' reason='no_active_actor_presentation_handle' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);
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
                        DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][ActorPresentation] event='ActorPresentationReleaseSkipped' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' policy='{policy}' mode='Retained' reason='policy_mismatch' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);
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
                    DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][ActorPresentation] event='ActorPresentationReleased' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{state.ActorId}' actorInstanceRuntimeId='{state.ActorInstanceRuntimeId}' rail='{rail}' policy='{policy}' mode='{(keptBound ? "Retained" : "Released")}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

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
            DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][ActorPresentation] event='ActorPresentationReleaseCompleted' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' rail='{rail}' target='{(targetActorInstanceRuntimeId.IsValid ? targetActorInstanceRuntimeId.Value : "all")}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
        }

        public static void ExecuteActorAttributeRelease(
            ActivityExitActorTeardownCommand command,
            IActivityEntryRuntimeBridge endpoint,
            ActivityActorExitRuntimeState runtimeState,
            IActivityExitActorTeardownRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.EntrySequence;
            SessionActivityIdentity startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReleaseStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorAttributeReleaseStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReleaseStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release started.");
            endpoint.EmitSnapshot(snapshots, "actor_attribute_release_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute release started.");
            DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorAttributeReleaseStarted' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

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
                    if (!capabilityState.Endpoint.TryRelease(capabilityState.PipelineIdentity, capabilityState.ActivityIdentity, out ActorAttributeReleaseResult releaseResult))
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
                    DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][Actor] event='{eventName}' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{actorId}' reason='{releaseResult.Reason}' releasedAttributeCount='{releaseResult.ReleasedAttributeCount}' source='{command.Source}' reasonDetail='{command.Reason}'.", releaseResult.SkippedNoContent ? DebugUtility.Colors.Info : DebugUtility.Colors.Success);

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
            DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorAttributeReleaseCompleted' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{totalCount}' released='{releasedCount}' skipped='{skippedCount}' failed='{failedCount}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
        }

        public static SessionActivityIdentity ExecuteActorParticipationExit(
            ActivityExitActorTeardownCommand command,
            IActivityEntryRuntimeBridge endpoint,
            ActivityActorExitRuntimeState runtimeState,
            IActivityExitActorTeardownRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.EntrySequence;
            SessionActivityIdentity startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorParticipationExitStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorParticipationExitStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit started mode='inventory_feed'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit started.");
            DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorParticipationExitStarted' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

            ActorInventoryFeedResult feedResult = runtimeState.GetActorInventoryFeedForExit(startedIdentity, command.Source, command.Reason);
            ActorParticipationExitCommand exitCommand = new(
                startedIdentity,
                definition.ActivityId,
                entrySequence,
                feedResult,
                command.Source,
                command.Reason);
            ActorParticipationExitResult exitResult = runtimeState.ExecuteActorParticipationExit(exitCommand);
            DebugUtility.Log(
                typeof(ActivityExitActorTeardownStage),
                $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorParticipationExitResultBuilt' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{exitResult.Total}' exited='{exitResult.Exited}' skipped='{exitResult.Skipped}' failed='{exitResult.Failed}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            int total = exitResult.Total;
            int exited = exitResult.Exited;
            int skipped = exitResult.Skipped;
            int failed = exitResult.Failed;
            List<PlayerActorIdentityRecord> exitedPlayerActors = new();
            List<string> exitedPlayerActorDetails = new();

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
                DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorParticipationExited' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' actorRole='{instance.Role}' actorScope='{instance.Scope}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

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
                    exitedPlayerActorDetails.Add(
                        $"participantId='{participantBinding.ParticipantId}' requirementId='{participantBinding.RequirementId}' role='{participantBinding.Role}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' playerActorId='{actorResult.PlayerActorId}' playerSlotId='{participantBinding.PlayerSlotId}'");
                }
                else if (actorResult.HasResolvedPlayerIdentity)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][ActivityExitActorTeardownStage][ActorParticipationExit] player_participant_binding_resolution_failed reason='{participantBindingFailureReason}' actorId='{instance.ActorId}' actorInstanceRuntimeId='{instance.ActorInstanceId}' playerActorId='{actorResult.PlayerActorId}' playerSlotId='{actorResult.PlayerSlotId}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }
            }

            if (exitedPlayerActors.Count > 0)
            {
                string actorDetails = exitedPlayerActorDetails.Count == 0
                    ? "actorId='<none>' actorInstanceRuntimeId='<none>' playerActorId='<none>' playerSlotId='<none>'"
                    : string.Join(" | ", exitedPlayerActorDetails);
                DebugUtility.Log(
                    typeof(ActivityExitActorTeardownStage),
                    $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorParticipationPlayerExitStarted' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' playerCount='{exitedPlayerActors.Count}' ownership='ActivityParticipantBinding' details=\"{actorDetails}\" source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                ExecutePlayerActorParticipationExit(command, endpoint, bridge, facts, snapshots, startedIdentity, exitedPlayerActors);
                DebugUtility.Log(
                    typeof(ActivityExitActorTeardownStage),
                    $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorParticipationPlayerExitCompleted' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' playerCount='{exitedPlayerActors.Count}' ownership='ActivityParticipantBinding' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
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
            DebugUtility.Log(
                typeof(ActivityExitActorTeardownStage),
                $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorParticipationExitCompletionAboutToEmit' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorParticipationExitCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorParticipationExitCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit completed total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}'.");
            endpoint.EmitSnapshot(snapshots, "actor_participation_exit_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor participation exit completed total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}'.");
            DebugUtility.Log(typeof(ActivityExitActorTeardownStage), $"[OBS][ActivityExitActorTeardownStage][Actor] event='ActorParticipationExitCompleted' owner='ActivityExitActorTeardownStage' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' total='{total}' exited='{exited}' skipped='{skipped}' failed='{failed}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
            return completedIdentity;
        }

        private static void ExecutePlayerActorParticipationExit(
            ActivityExitActorTeardownCommand command,
            IActivityEntryRuntimeBridge endpoint,
            IActivityExitActorTeardownRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots,
            SessionActivityIdentity stageStartedIdentity,
            IReadOnlyList<PlayerActorIdentityRecord> exitedPlayerActors)
        {
            SessionActivityDefinition definition = command.Definition;
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
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorRetainedForRoute,
                stageStartedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actors retained for route. actors='{exitRecords.Count}'.");
            endpoint.EmitSnapshot(
                snapshots,
                "player_actor_retained_for_route",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actors retained for route. actors='{exitRecords.Count}'.");

            SessionActivityIdentity stageCompletedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.PlayerActorParticipationExitStageCompleted, entrySequence);
            endpoint.EmitFact(
                facts,
                SessionActivityFactKind.PlayerActorParticipationExitStageCompleted,
                stageCompletedIdentity,
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit stage completed.");
            endpoint.EmitSnapshot(
                snapshots,
                "player_actor_participation_exit_stage_completed",
                command.Source,
                command.Reason,
                $"'{definition.ActivityId}' player actor participation exit stage completed.");
        }
    }
}
