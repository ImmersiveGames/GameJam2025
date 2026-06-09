using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Adapters;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryActorPresentationStage
    {
        public static ActivityEntryActorPresentationSetupResult Execute(
            ActivityEntryActorPresentationSetupCommand command,
            IActivityEntryRuntimeBridge endpoint,
            IReadOnlyList<ActorPresentationSetupContribution> presentationSetupContributions,
            ActivityActorExitRuntimeState runtimeState,
            IActivityEntryActorPresentationRuntimeBridge bridge,
            ActorPresentationPlanResolver planResolver,
            IActorPresentationMaterializationAdapter materializationAdapter,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryActorPresentationSetupCommand is invalid.");
            }

            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (bridge == null)
            {
                throw new ArgumentNullException(nameof(bridge));
            }

            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            if (planResolver == null)
            {
                throw new ArgumentNullException(nameof(planResolver));
            }

            if (materializationAdapter == null)
            {
                throw new ArgumentNullException(nameof(materializationAdapter));
            }

            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity startedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationSetupStarted);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorPresentationSetupStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupStarted, startedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup started mode='SetupContributions'.");
            endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_started", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup started.");
            DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationSetupFromSetupContributionsStarted' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}' mode='SetupContributions'.", DebugUtility.Colors.Info);

            IReadOnlyList<ActorPresentationSetupContribution> presentationContributions = presentationSetupContributions ?? Array.Empty<ActorPresentationSetupContribution>();
            int totalResolved = 0;
            int totalMaterialized = 0;
            int totalRetained = 0;
            int totalReady = 0;
            int totalSkipped = 0;

            if (presentationContributions.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupSkippedOptional, skippedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup skipped reason='no_presentation_setup_contributions'.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_skipped_optional", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup skipped reason='no_presentation_setup_contributions'.");
                DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationSetupSkippedOptional' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reason='no_presentation_setup_contributions' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);

                SessionActivityIdentity completedAfterSkipIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationSetupCompleted);
                endpoint.SetCurrentIdentity(completedAfterSkipIdentity, SessionActivityStage.ActorPresentationSetupCompleted);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupCompleted, completedAfterSkipIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup completed with skip.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_completed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup completed with skip.");
                DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationSetupCompleted' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' mode='SkippedNoContributions' total='0' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

                return new ActivityEntryActorPresentationSetupResult(
                    completed: true,
                    completedAfterSkipIdentity,
                    total: 0,
                    resolved: 0,
                    materialized: 0,
                    retained: 0,
                    skipped: 0,
                    reason: "actor_presentation_setup_skipped_no_contributions");
            }

            for (int index = 0; index < presentationContributions.Count; index++)
            {
                ActorPresentationSetupContribution presentationContribution = presentationContributions[index];
                if (!presentationContribution.IsValid)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup failed actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' reason='presentation_contribution_invalid'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup failed actorId='{presentationContribution.ActorId}' reason='presentation_contribution_invalid'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryActorPresentationStage][ActorPresentationSetup] Invalid presentation contribution actorId='{presentationContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}'.");
                }

                if (!IsSamePresentationScope(startedIdentity, presentationContribution.Identity))
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup failed actorId='{presentationContribution.ActorId}' reason='presentation_contribution_identity_mismatch'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup failed actorId='{presentationContribution.ActorId}' reason='presentation_contribution_identity_mismatch'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryActorPresentationStage][ActorPresentationSetup] Presentation contribution identity mismatch actorId='{presentationContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}'.");
                }

                ActorPresentationEndpoint endpointReference = presentationContribution.Endpoint;
                if (endpointReference == null)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup failed actorId='{presentationContribution.ActorId}' reason='actor_presentation_endpoint_missing'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup failed actorId='{presentationContribution.ActorId}' reason='actor_presentation_endpoint_missing'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryActorPresentationStage][ActorPresentationSetup] Missing ActorPresentationEndpoint actorId='{presentationContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}'.");
                }

                endpointReference.ValidateOrThrow($"{nameof(ActivityEntryActorPresentationStage)}/ActorPresentationSetupFromSetupContributions");
                ActorPresentationProfileAsset profile = presentationContribution.Profile ?? endpointReference.Profile;

                ActorPresentationPlanResolutionResult planResult = ActorPresentationSetupStage.ResolvePlan(
                    planResolver,
                    profile,
                    endpointReference,
                    startedIdentity.ActivityId,
                    presentationContribution.ActorId.Value,
                    presentationContribution.ActorKind.ToString(),
                    nameof(ActivityEntryActorPresentationStage),
                    command.Reason);

                if (planResult.IsFailed)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup failed actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' actorScope='{presentationContribution.ActorScope}' reason='{planResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup failed actorId='{presentationContribution.ActorId}' reason='{planResult.ReasonCode}'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryActorPresentationStage][ActorPresentationSetup] Plan resolution failed actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' reason='{planResult.ReasonCode}' message='{planResult.Message}'.");
                }

                if (planResult.IsSkippedOptional)
                {
                    SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupSkippedOptional, skippedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup skipped optional actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' actorScope='{presentationContribution.ActorScope}' reason='{planResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_skipped_optional", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup skipped optional actorId='{presentationContribution.ActorId}' reason='{planResult.ReasonCode}'.");
                    continue;
                }

                SessionActivityIdentity planResolvedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationPlanResolved);
                endpoint.SetCurrentIdentity(planResolvedIdentity, SessionActivityStage.ActorPresentationPlanResolved);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationPlanResolved, planResolvedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation plan resolved actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' actorScope='{presentationContribution.ActorScope}' profileId='{planResult.ResolvedPlan.ProfileId}' componentPath='{presentationContribution.ComponentPath}'.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_plan_resolved", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation plan resolved actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' profileId='{planResult.ResolvedPlan.ProfileId}'.");
                DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationPlanResolved' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationContribution.ActorId}' actorInstanceRuntimeId='{presentationContribution.ActorInstanceRuntimeId}' actorKind='{presentationContribution.ActorKind}' actorRole='{presentationContribution.ActorRole}' actorScope='{presentationContribution.ActorScope}' profileId='{planResult.ResolvedPlan.ProfileId}' componentPath='{presentationContribution.ComponentPath}' mode='SetupContributions' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);
                totalResolved += 1;

                if (bridge.TryGetActiveActorPresentationHandle(presentationContribution.ActorInstanceRuntimeId, out ActorPresentationRuntimeHandle activeHandle))
                {
                    if (CanRetainPresentationHandle(presentationContribution, activeHandle, planResult.ResolvedPlan))
                    {
                        SessionActivityIdentity retainedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationRetained);
                        endpoint.SetCurrentIdentity(retainedIdentity, SessionActivityStage.ActorPresentationRetained);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationRetained, retainedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation retained actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' actorScope='{presentationContribution.ActorScope}' policy='{activeHandle.ResolvedPlan.ReleasePolicy}' profileId='{activeHandle.ResolvedPlan.ProfileId}'.");
                        DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationRetained' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationContribution.ActorId}' actorInstanceRuntimeId='{presentationContribution.ActorInstanceRuntimeId}' actorKind='{presentationContribution.ActorKind}' actorRole='{presentationContribution.ActorRole}' actorScope='{presentationContribution.ActorScope}' profileId='{activeHandle.ResolvedPlan.ProfileId}' mode='Retained' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                        totalRetained += 1;

                        SessionActivityIdentity readyRetainedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationReady);
                        endpoint.SetCurrentIdentity(readyRetainedIdentity, SessionActivityStage.ActorPresentationReady);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReady, readyRetainedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation ready retained actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' actorScope='{presentationContribution.ActorScope}' instance='{activeHandle.PresentationInstance.name}'.");
                        DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationReady' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationContribution.ActorId}' actorInstanceRuntimeId='{presentationContribution.ActorInstanceRuntimeId}' actorKind='{presentationContribution.ActorKind}' actorRole='{presentationContribution.ActorRole}' actorScope='{presentationContribution.ActorScope}' profileId='{activeHandle.ResolvedPlan.ProfileId}' mode='Retained' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                        totalReady += 1;
                        continue;
                    }

                    bridge.ReleaseActorPresentationBeforeRematerialization(command.Identity, command.Source, command.Reason, presentationContribution.ActorInstanceRuntimeId, facts, snapshots);
                }

                ActorPresentationResult materializationResult = ActorPresentationSetupStage.Materialize(
                    materializationAdapter,
                    planResult.ResolvedPlan,
                    command.Source,
                    command.Reason);
                if (materializationResult.IsFailed)
                {
                    SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup failed actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' actorScope='{presentationContribution.ActorScope}' reason='{materializationResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup failed actorId='{presentationContribution.ActorId}' reason='{materializationResult.ReasonCode}'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryActorPresentationStage][ActorPresentationSetup] Materialization failed actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' reason='{materializationResult.ReasonCode}' message='{materializationResult.Message}'.");
                }

                if (materializationResult.IsSkippedOptional)
                {
                    SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupSkippedOptional, skippedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup skipped optional actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' actorScope='{presentationContribution.ActorScope}' reason='{materializationResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_skipped_optional", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup skipped optional actorId='{presentationContribution.ActorId}' reason='{materializationResult.ReasonCode}'.");
                    DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationSetupSkippedOptional' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationContribution.ActorId}' actorInstanceRuntimeId='{presentationContribution.ActorInstanceRuntimeId}' actorKind='{presentationContribution.ActorKind}' actorRole='{presentationContribution.ActorRole}' actorScope='{presentationContribution.ActorScope}' mode='SkippedOptional' reasonCode='{materializationResult.ReasonCode}' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);
                    totalSkipped += 1;
                    continue;
                }

                SessionActivityIdentity materializedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationMaterialized);
                endpoint.SetCurrentIdentity(materializedIdentity, SessionActivityStage.ActorPresentationMaterialized);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationMaterialized, materializedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation materialized actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' actorScope='{presentationContribution.ActorScope}' profileId='{materializationResult.ReadyFact.ResolvedPlan.ProfileId}' componentPath='{presentationContribution.ComponentPath}'.");
                DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationMaterialized' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationContribution.ActorId}' actorInstanceRuntimeId='{presentationContribution.ActorInstanceRuntimeId}' actorKind='{presentationContribution.ActorKind}' actorRole='{presentationContribution.ActorRole}' actorScope='{presentationContribution.ActorScope}' profileId='{materializationResult.ReadyFact.ResolvedPlan.ProfileId}' mode='Materialized' componentPath='{presentationContribution.ComponentPath}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                totalMaterialized += 1;

                SessionActivityIdentity readyIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationReady);
                endpoint.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActorPresentationReady);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReady, readyIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation ready actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' actorScope='{presentationContribution.ActorScope}' instance='{materializationResult.ReadyFact.PresentationInstance.name}'.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_ready", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation ready actorId='{presentationContribution.ActorId}' actorKind='{presentationContribution.ActorKind}' actorScope='{presentationContribution.ActorScope}'.");
                DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationReady' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationContribution.ActorId}' actorInstanceRuntimeId='{presentationContribution.ActorInstanceRuntimeId}' actorKind='{presentationContribution.ActorKind}' actorRole='{presentationContribution.ActorRole}' actorScope='{presentationContribution.ActorScope}' profileId='{materializationResult.ReadyFact.ResolvedPlan.ProfileId}' mode='Materialized' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

                StoreActiveActorPresentationMirror(
                    runtimeState,
                    startedIdentity,
                    presentationContribution,
                    materializationResult.ReadyFact.RuntimeHandle);
                totalReady += 1;
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(command, SessionActivityStage.ActorPresentationSetupCompleted);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorPresentationSetupCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupCompleted, completedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup completed total='{totalReady}' resolved='{totalResolved}' materialized='{totalMaterialized}' retained='{totalRetained}' skipped='{totalSkipped}' mode='SetupContributions'.");
            endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_completed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor presentation setup completed total='{totalReady}' resolved='{totalResolved}' materialized='{totalMaterialized}' retained='{totalRetained}' skipped='{totalSkipped}'.");
            DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationSetupCompleted' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' total='{totalReady}' resolved='{totalResolved}' materialized='{totalMaterialized}' retained='{totalRetained}' skipped='{totalSkipped}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

            return new ActivityEntryActorPresentationSetupResult(
                completed: true,
                completedIdentity,
                totalReady,
                totalResolved,
                totalMaterialized,
                totalRetained,
                totalSkipped,
                "actor_presentation_setup_applied");
        }

        private static SessionActivityIdentity BuildIdentity(
            ActivityEntryActorPresentationSetupCommand command,
            SessionActivityStage stage)
        {
            return new SessionActivityIdentity(
                command.Identity.PipelineId,
                command.Identity.SessionId,
                command.Identity.ActivityId,
                command.Identity.ActivityOrdinal,
                command.Identity.EntrySequence,
                stage,
                command.Source);
        }

        private static void StoreActiveActorPresentationMirror(
            ActivityActorExitRuntimeState runtimeState,
            SessionActivityIdentity identity,
            ActorPresentationSetupContribution presentationContribution,
            ActorPresentationRuntimeHandle handle)
        {
            if (runtimeState == null || !presentationContribution.IsValid || !handle.IsValid)
            {
                return;
            }

            runtimeState.StoreActiveActorPresentation(
                new ActivityActorExitRuntimeState.ActorPresentationCapabilityState(
                    presentationContribution.ActorInstanceRuntimeId,
                    presentationContribution.ActorId.Value,
                    presentationContribution.Endpoint,
                    handle,
                    identity.PipelineId,
                    BuildActorAttributeActivityIdentity(identity)),
                identity.ActivityId,
                identity.EntrySequence,
                nameof(ActivityEntryActorPresentationStage),
                "store_active_actor_presentation");
        }

        private static string BuildActorAttributeActivityIdentity(SessionActivityIdentity identity)
        {
            if (!identity.IsValid)
            {
                return string.Empty;
            }

            return $"{identity.PipelineId}|{identity.SessionId}|{identity.ActivityId}|{identity.ActivityOrdinal}|{identity.EntrySequence}";
        }

        private static bool CanRetainPresentationHandle(
            ActorPresentationSetupContribution presentationContribution,
            ActorPresentationRuntimeHandle activeHandle,
            ActorPresentationResolvedPlan resolvedPlan)
        {
            if (!presentationContribution.IsValid || !resolvedPlan.IsValid || !activeHandle.IsValid || !activeHandle.ResolvedPlan.IsValid)
            {
                return false;
            }

            if (resolvedPlan.ReleasePolicy == ActorPresentationReleasePolicy.ReleaseOnActivityExit)
            {
                return false;
            }

            return activeHandle.IsValid &&
                activeHandle.ResolvedPlan.IsValid &&
                activeHandle.PresentationInstance != null &&
                activeHandle.ResolvedPlan.ReleasePolicy == resolvedPlan.ReleasePolicy &&
                string.Equals(activeHandle.ResolvedPlan.ProfileId, resolvedPlan.ProfileId, StringComparison.Ordinal) &&
                string.Equals(activeHandle.ResolvedPlan.ActorId, presentationContribution.ActorId.Value, StringComparison.Ordinal);
        }

        private static bool IsSamePresentationScope(
            SessionActivityIdentity identity,
            SessionActivityIdentity contributionIdentity)
        {
            return identity.IsValid &&
                   contributionIdentity.IsValid &&
                   string.Equals(identity.PipelineId, contributionIdentity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(identity.SessionId, contributionIdentity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(identity.ActivityId, contributionIdentity.ActivityId, StringComparison.Ordinal) &&
                   identity.EntrySequence == contributionIdentity.EntrySequence;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
