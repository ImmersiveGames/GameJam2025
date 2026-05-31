using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Adapters;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryActorPresentationStage
    {
        public static ActivityEntryActorPresentationSetupResult Execute(
            ActivityEntryActorPresentationSetupCommand command,
            IActivityEntryRuntimeEndpoint endpoint,
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

            if (planResolver == null)
            {
                throw new ArgumentNullException(nameof(planResolver));
            }

            if (materializationAdapter == null)
            {
                throw new ArgumentNullException(nameof(materializationAdapter));
            }

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorPresentationSetupStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup started mode='inventory_references'.");
            endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup started.");
            DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationSetupFromInventoryStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}' mode='InventoryReferences'.", DebugUtility.Colors.Info);

            ActivityCapabilityInventory inventory = bridge.GetCurrentActivityCapabilityInventoryPreview();
            if (!inventory.IsValid ||
                !string.Equals(inventory.Id.PipelineId, startedIdentity.PipelineId, StringComparison.Ordinal) ||
                !string.Equals(inventory.Id.SessionStateId, startedIdentity.SessionId, StringComparison.Ordinal) ||
                !string.Equals(inventory.Id.ActivityId, startedIdentity.ActivityId, StringComparison.Ordinal) ||
                inventory.Id.EntrySequence != startedIdentity.EntrySequence)
            {
                SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupFailed, entrySequence);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed reason='inventory_missing_or_foreign'.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed reason='inventory_missing_or_foreign'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryActorPresentationStage][ActorPresentationSetup] Missing or foreign ActivityCapabilityInventory activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            IReadOnlyList<ActorPresentationEndpointReference> presentationReferences = ResolveActorPresentationReferencesFromInventory(inventory);
            int totalResolved = 0;
            int totalMaterialized = 0;
            int totalRetained = 0;
            int totalReady = 0;
            int totalSkipped = 0;

            if (presentationReferences.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupSkippedOptional, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupSkippedOptional, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped reason='no_presentation_endpoint_references'.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_skipped_optional", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped reason='no_presentation_endpoint_references'.");
                DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationSetupSkippedOptional' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reason='no_presentation_endpoint_references' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);

                SessionActivityIdentity completedAfterSkipIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupCompleted, entrySequence);
                endpoint.SetCurrentIdentity(completedAfterSkipIdentity, SessionActivityStage.ActorPresentationSetupCompleted);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupCompleted, completedAfterSkipIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup completed with skip.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup completed with skip.");
                DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationSetupCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' mode='SkippedNoReferences' total='0' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

                return new ActivityEntryActorPresentationSetupResult(
                    completed: true,
                    completedAfterSkipIdentity,
                    total: 0,
                    resolved: 0,
                    materialized: 0,
                    retained: 0,
                    skipped: 0,
                    reason: "actor_presentation_setup_skipped_no_references");
            }

            for (int index = 0; index < presentationReferences.Count; index++)
            {
                ActorPresentationEndpointReference presentationReference = presentationReferences[index];
                ActorPresentationEndpoint endpointReference = presentationReference.Endpoint;
                if (endpointReference == null)
                {
                    continue;
                }

                endpointReference.ValidateOrThrow($"{nameof(ActivityEntryActorPresentationStage)}/ActorPresentationSetupFromInventory");
                if (endpointReference.Profile == null)
                {
                    SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupFailed, entrySequence);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' reason='actor_presentation_profile_missing'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' reason='actor_presentation_profile_missing'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryActorPresentationStage][ActorPresentationSetup] Missing ActorPresentationProfileAsset actorId='{presentationReference.ActorId}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                }

                ActorPresentationPlanResolutionResult planResult = ActorPresentationSetupStage.ResolvePlan(
                    planResolver,
                    endpointReference.Profile,
                    endpointReference,
                    startedIdentity.ActivityId,
                    presentationReference.ActorId,
                    presentationReference.ActorKind.ToString(),
                    nameof(ActivityEntryActorPresentationStage),
                    command.Reason);

                if (planResult.IsFailed)
                {
                    SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupFailed, entrySequence);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' reason='{planResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' reason='{planResult.ReasonCode}'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryActorPresentationStage][ActorPresentationSetup] Plan resolution failed actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' reason='{planResult.ReasonCode}' message='{planResult.Message}'.");
                }

                if (planResult.IsSkippedOptional)
                {
                    SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupSkippedOptional, entrySequence);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupSkippedOptional, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped optional actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' reason='{planResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_skipped_optional", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped optional actorId='{presentationReference.ActorId}' reason='{planResult.ReasonCode}'.");
                    continue;
                }

                SessionActivityIdentity planResolvedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationPlanResolved, entrySequence);
                endpoint.SetCurrentIdentity(planResolvedIdentity, SessionActivityStage.ActorPresentationPlanResolved);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationPlanResolved, planResolvedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation plan resolved actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' profileId='{planResult.ResolvedPlan.ProfileId}' componentPath='{presentationReference.ComponentPath}'.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_plan_resolved", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation plan resolved actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' profileId='{planResult.ResolvedPlan.ProfileId}'.");
                DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationPlanResolved' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' profileId='{planResult.ResolvedPlan.ProfileId}' componentPath='{presentationReference.ComponentPath}' mode='Planned' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);
                totalResolved += 1;

                if (bridge.TryGetActiveActorPresentationHandle(presentationReference, out ActorPresentationRuntimeHandle activeHandle))
                {
                    if (CanRetainPresentationHandle(presentationReference, activeHandle, planResult.ResolvedPlan))
                    {
                        SessionActivityIdentity retainedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationRetained, entrySequence);
                        endpoint.SetCurrentIdentity(retainedIdentity, SessionActivityStage.ActorPresentationRetained);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationRetained, retainedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation retained actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' policy='{activeHandle.ResolvedPlan.ReleasePolicy}' profileId='{activeHandle.ResolvedPlan.ProfileId}'.");
                        DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationRetained' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' profileId='{activeHandle.ResolvedPlan.ProfileId}' mode='Retained' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                        totalRetained += 1;

                        SessionActivityIdentity readyRetainedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReady, entrySequence);
                        endpoint.SetCurrentIdentity(readyRetainedIdentity, SessionActivityStage.ActorPresentationReady);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReady, readyRetainedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation ready retained actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' instance='{activeHandle.PresentationInstance.name}'.");
                        DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationReady' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' profileId='{activeHandle.ResolvedPlan.ProfileId}' mode='Retained' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                        bridge.SyncActiveActorPresentationHandle(startedIdentity, presentationReference, activeHandle);
                        totalReady += 1;
                        continue;
                    }

                    bridge.ReleaseActorPresentationBeforeRematerialization(command, presentationReference.ActorInstanceRuntimeId, facts, snapshots);
                }

                ActorPresentationResult materializationResult = ActorPresentationSetupStage.Materialize(
                    materializationAdapter,
                    planResult.ResolvedPlan,
                    command.Source,
                    command.Reason);
                if (materializationResult.IsFailed)
                {
                    SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupFailed, entrySequence);
                    endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorPresentationSetupFailed);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' reason='{materializationResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup failed actorId='{presentationReference.ActorId}' reason='{materializationResult.ReasonCode}'.");
                    throw new InvalidOperationException($"[FATAL][ActivityEntryActorPresentationStage][ActorPresentationSetup] Materialization failed actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' reason='{materializationResult.ReasonCode}' message='{materializationResult.Message}'.");
                }

                if (materializationResult.IsSkippedOptional)
                {
                    SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupSkippedOptional, entrySequence);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorPresentationSetupSkippedOptional);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupSkippedOptional, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped optional actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' reason='{materializationResult.ReasonCode}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_skipped_optional", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup skipped optional actorId='{presentationReference.ActorId}' reason='{materializationResult.ReasonCode}'.");
                    DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationSetupSkippedOptional' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' mode='SkippedOptional' reasonCode='{materializationResult.ReasonCode}' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);
                    totalSkipped += 1;
                    continue;
                }

                SessionActivityIdentity materializedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationMaterialized, entrySequence);
                endpoint.SetCurrentIdentity(materializedIdentity, SessionActivityStage.ActorPresentationMaterialized);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationMaterialized, materializedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation materialized actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' profileId='{materializationResult.ReadyFact.ResolvedPlan.ProfileId}' componentPath='{presentationReference.ComponentPath}'.");
                DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationMaterialized' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' profileId='{materializationResult.ReadyFact.ResolvedPlan.ProfileId}' mode='Materialized' componentPath='{presentationReference.ComponentPath}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                totalMaterialized += 1;

                SessionActivityIdentity readyIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationReady, entrySequence);
                endpoint.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActorPresentationReady);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationReady, readyIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation ready actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}' instance='{materializationResult.ReadyFact.PresentationInstance.name}'.");
                endpoint.EmitSnapshot(snapshots, "actor_presentation_ready", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation ready actorId='{presentationReference.ActorId}' actorKind='{presentationReference.ActorKind}' actorScope='{presentationReference.ActorScope}'.");
                DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationReady' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{presentationReference.ActorId}' actorInstanceRuntimeId='{presentationReference.ActorInstanceRuntimeId}' actorKind='{presentationReference.ActorKind}' actorRole='{presentationReference.ActorRole}' actorScope='{presentationReference.ActorScope}' profileId='{materializationResult.ReadyFact.ResolvedPlan.ProfileId}' mode='Materialized' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

                bridge.StoreActiveActorPresentationHandle(startedIdentity, presentationReference, materializationResult.ReadyFact.RuntimeHandle);
                totalReady += 1;
            }

            SessionActivityIdentity completedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorPresentationSetupCompleted, entrySequence);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorPresentationSetupCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorPresentationSetupCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup completed total='{totalReady}' resolved='{totalResolved}' materialized='{totalMaterialized}' retained='{totalRetained}' skipped='{totalSkipped}' mode='inventory_references'.");
            endpoint.EmitSnapshot(snapshots, "actor_presentation_setup_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor presentation setup completed total='{totalReady}' resolved='{totalResolved}' materialized='{totalMaterialized}' retained='{totalRetained}' skipped='{totalSkipped}'.");
            DebugUtility.Log(typeof(ActivityEntryActorPresentationStage), $"[OBS][ActivityEntryPipeline][ActorPresentation] event='ActorPresentationSetupCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' total='{totalReady}' resolved='{totalResolved}' materialized='{totalMaterialized}' retained='{totalRetained}' skipped='{totalSkipped}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);

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

        private static IReadOnlyList<ActorPresentationEndpointReference> ResolveActorPresentationReferencesFromInventory(ActivityCapabilityInventory inventory)
        {
            List<ActorPresentationEndpointReference> references = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.PresentationEndpoint)
                {
                    continue;
                }

                if (inventory.TryGetRuntimeReference(capability.CapabilityId, out ActorPresentationEndpointReference reference) &&
                    reference != null &&
                    reference.IsValid)
                {
                    references.Add(reference);
                }
            }

            return references;
        }

        private static bool CanRetainPresentationHandle(
            ActorPresentationEndpointReference presentationReference,
            ActorPresentationRuntimeHandle activeHandle,
            ActorPresentationResolvedPlan resolvedPlan)
        {
            if (!resolvedPlan.IsValid || !activeHandle.IsValid || !activeHandle.ResolvedPlan.IsValid)
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
                string.Equals(activeHandle.ResolvedPlan.ActorId, presentationReference.ActorId, StringComparison.Ordinal);
        }
    }
}
