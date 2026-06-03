using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryActorAttributeStage
    {
        public static ActivityEntryActorAttributeSetupResult Execute(
            ActivityEntryActorAttributeSetupCommand command,
            IActivityEntryRuntimeBridge endpoint,
            IActivityEntryActorAttributeRuntimeBridge bridge,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryActorAttributeSetupCommand is invalid.");
            }

            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (bridge == null)
            {
                throw new ArgumentNullException(nameof(bridge));
            }

            SessionActivityDefinition definition = command.Definition;
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity startedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupStarted, entrySequence);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorAttributeSetupStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupStarted, startedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup started mode='inventory_references'.");
            endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_started", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup started.");
            DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"[OBS][ActivityEntryPipeline][ActorAttribute] event='ActorAttributeSetupStarted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}' mode='InventoryReferences'.", DebugUtility.Colors.Info);

            ActivityCapabilityInventory inventory = bridge.GetCurrentActivityCapabilityInventoryPreview();
            if (!inventory.IsValid ||
                !string.Equals(inventory.Id.PipelineId, startedIdentity.PipelineId, StringComparison.Ordinal) ||
                !string.Equals(inventory.Id.SessionStateId, startedIdentity.SessionId, StringComparison.Ordinal) ||
                !string.Equals(inventory.Id.ActivityId, startedIdentity.ActivityId, StringComparison.Ordinal) ||
                inventory.Id.EntrySequence != startedIdentity.EntrySequence)
            {
                SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupFailed, entrySequence);
                endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed reason='inventory_missing_or_foreign'.");
                endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed reason='inventory_missing_or_foreign'.");
                throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Missing or foreign ActivityCapabilityInventory activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
            }

            IReadOnlyList<ActorAttributeEndpointReference> attributeReferences = ResolveActorAttributeReferencesFromInventory(inventory);
            int totalCount = attributeReferences.Count;
            int resolvedCount = 0;
            int readyCount = 0;
            int skippedCount = 0;
            int failedCount = 0;
            if (attributeReferences.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupSkipped, entrySequence);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_references'.");
                endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_references'.");
                DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"[OBS][ActivityEntryPipeline][ActorAttribute] event='ActorAttributeSetupSkipped' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reason='no_attribute_endpoint_references' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);
                skippedCount += 1;
            }
            else
            {
                for (int index = 0; index < attributeReferences.Count; index++)
                {
                    ActorAttributeEndpointReference attributeReference = attributeReferences[index];
                    if (attributeReference is not { IsValid: true } || attributeReference.Endpoint == null)
                    {
                        continue;
                    }

                    resolvedCount += 1;
                    ActorAttributeEndpoint attributeEndpoint = attributeReference.Endpoint;
                    ActorAttributeProfileAsset profile = attributeEndpoint.AttributeProfile;
                    SessionActivityIdentity profileResolvedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeProfileResolved, entrySequence);
                    endpoint.SetCurrentIdentity(profileResolvedIdentity, SessionActivityStage.ActorAttributeProfileResolved);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeProfileResolved, profileResolvedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute profile resolved actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' profileId='{(profile != null ? profile.ProfileId : "<none>")}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_attribute_profile_resolved", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute profile resolved actorId='{attributeReference.ActorId}' profileId='{(profile != null ? profile.ProfileId : "<none>")}'.");
                    DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"[OBS][ActivityEntryPipeline][ActorAttribute] event='ActorAttributeProfileResolved' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' profileId='{(profile != null ? profile.ProfileId : "<none>")}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

                    if (profile == null)
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' reason='attribute_profile_missing'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed actorId='{attributeReference.ActorId}' reason='attribute_profile_missing'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Missing ActorAttributeProfileAsset actorId='{attributeReference.ActorId}' activityId='{definition.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    if (!attributeEndpoint.TryInitialize(new Actors.Foundation.ActorInstanceRuntimeId(attributeReference.ActorInstanceRuntimeId.Value), startedIdentity, out ActorAttributeSetupResult setupResult))
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupFailed, entrySequence);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' reason='{setupResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup failed actorId='{attributeReference.ActorId}' reason='{setupResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Initialization failed actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' reason='{setupResult.Reason}'.");
                    }

                    if (setupResult.IsSkippedNoContent)
                    {
                        skippedCount += 1;
                        SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupSkipped, entrySequence);
                        endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' reason='{setupResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped actorId='{attributeReference.ActorId}' reason='{setupResult.Reason}'.");
                        bridge.RemoveActiveActorAttributeCapability(attributeReference.ActorInstanceRuntimeId);
                        continue;
                    }

                    bridge.StoreActiveActorAttributeCapability(
                        startedIdentity,
                        attributeReference,
                        attributeEndpoint);

                    SessionActivityIdentity readyIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeReady, entrySequence);
                    endpoint.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActorAttributeReady);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReady, readyIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute ready actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' attributeCount='{setupResult.AttributeCount}' attributeIds='{BuildActorAttributeIdList(attributeEndpoint)}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_attribute_ready", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute ready actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' attributeCount='{setupResult.AttributeCount}'.");
                    DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"[OBS][ActivityEntryPipeline][ActorAttribute] event='ActorAttributeReady' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{attributeReference.ActorId}' actorKind='{attributeReference.ActorKind}' attributeCount='{setupResult.AttributeCount}' attributeIds='{BuildActorAttributeIdList(attributeEndpoint)}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                    readyCount += 1;
                }

                if (resolvedCount == 0 || readyCount == 0)
                {
                    skippedCount += 1;
                    SessionActivityIdentity skippedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupSkipped, entrySequence);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_ready'.");
                    endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_ready'.");
                }
            }

            SessionActivityIdentity completedIdentity = endpoint.BuildIdentity(definition, SessionActivityStage.ActorAttributeSetupCompleted, entrySequence);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorAttributeSetupCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupCompleted, completedIdentity, command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup completed total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}' mode='inventory_references'.");
            endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_completed", command.Source, command.Reason, $"'{definition.ActivityId}' actor attribute setup completed total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}'.");
            DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"[OBS][ActivityEntryPipeline][ActorAttribute] event='ActorAttributeSetupCompleted' activityId='{definition.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
            return new ActivityEntryActorAttributeSetupResult(
                completed: true,
                completedIdentity,
                totalCount,
                resolvedCount,
                readyCount,
                skippedCount,
                failedCount,
                "actor_attribute_setup_completed");
        }

        private static IReadOnlyList<ActorAttributeEndpointReference> ResolveActorAttributeReferencesFromInventory(ActivityCapabilityInventory inventory)
        {
            List<ActorAttributeEndpointReference> references = new();
            for (int index = 0; index < inventory.Capabilities.Count; index++)
            {
                ActivityCapabilityDescriptor capability = inventory.Capabilities[index];
                if (capability.CapabilityKind != ActivityCapabilityKind.AttributeEndpoint)
                {
                    continue;
                }

                if (inventory.TryGetRuntimeReference(capability.CapabilityId, out ActorAttributeEndpointReference reference) &&
                    reference != null &&
                    reference.IsValid)
                {
                    references.Add(reference);
                }
            }

            return references;
        }

        private static string BuildActorAttributeActivityIdentity(SessionActivityIdentity identity)
        {
            if (!identity.IsValid)
            {
                return string.Empty;
            }

            return $"{identity.PipelineId}|{identity.SessionId}|{identity.ActivityId}|{identity.ActivityOrdinal}|{identity.EntrySequence}";
        }

        private static string BuildActorAttributeIdList(ActorAttributeEndpoint endpoint)
        {
            if (endpoint == null || endpoint.RuntimeStates == null || endpoint.RuntimeStates.Count == 0)
            {
                return "<none>";
            }

            List<string> ids = new(endpoint.RuntimeStates.Count);
            for (int index = 0; index < endpoint.RuntimeStates.Count; index++)
            {
                ActorAttributeState state = endpoint.RuntimeStates[index];
                if (state == null || !state.AttributeId.IsValid)
                {
                    continue;
                }

                ids.Add(state.AttributeId.ToString());
            }

            if (ids.Count == 0)
            {
                return "<none>";
            }

            ids.Sort(StringComparer.Ordinal);
            return string.Join(",", ids);
        }
    }
}
