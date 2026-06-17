using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Damage.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Stages
{
    internal static class ActivityEntryActorAttributeStage
    {
        public static ActivityEntryActorAttributeSetupResult Execute(
            ActivityEntryActorAttributeSetupCommand command,
            IActivityEntryIdentityRuntimeBridge identityBridge,
            IActivityEntryFactRuntimeBridge factBridge,
            ActivityActorExitRuntimeState runtimeState,
            List<SessionActivityFact> facts,
            List<SessionActivitySnapshot> snapshots)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ActivityEntryActorAttributeSetupCommand is invalid.");
            }

            if (identityBridge == null)
            {
                throw new ArgumentNullException(nameof(identityBridge));
            }

            if (factBridge == null)
            {
                throw new ArgumentNullException(nameof(factBridge));
            }

            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            IReadOnlyList<ActorAttributeSetupContribution> attributeContributions = command.AttributeSetupContributions ?? Array.Empty<ActorAttributeSetupContribution>();
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity startedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupStarted);
            identityBridge.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorAttributeSetupStarted);
            factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupStarted, startedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup started mode='SetupContributions'.");
            factBridge.EmitSnapshot(snapshots, "actor_attribute_setup_started", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup started.");
            int totalCount = attributeContributions.Count;
            int resolvedCount = 0;
            int readyCount = 0;
            int skippedCount = 0;
            int failedCount = 0;

            if (attributeContributions.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupSkipped);
                identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped reason='no_attribute_setup_contributions'.");
                factBridge.EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped reason='no_attribute_setup_contributions'.");
                DebugUtility.LogVerbose(typeof(ActivityEntryActorAttributeStage), $"event='ActorAttributeSetupSkipped' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryActorAttributeStage' entryPipelineOwner='ActivityEntryPipeline' reason='no_attribute_setup_contributions' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);
                skippedCount += 1;
            }
            else
            {
                for (int index = 0; index < attributeContributions.Count; index++)
                {
                    ActorAttributeSetupContribution attributeContribution = attributeContributions[index];
                    if (!attributeContribution.IsValid)
                    {
                        SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupFailed);
                        identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' reason='attribute_contribution_invalid'.");
                        factBridge.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='attribute_contribution_invalid'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Invalid attribute contribution actorId='{attributeContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    if (!IsSameAttributeScope(startedIdentity, attributeContribution.Identity))
                    {
                        SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupFailed);
                        identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='attribute_contribution_identity_mismatch'.");
                        factBridge.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='attribute_contribution_identity_mismatch'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Attribute contribution identity mismatch actorId='{attributeContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    ActorAttributeEndpoint attributeEndpoint = attributeContribution.Endpoint;
                    if (attributeEndpoint == null)
                    {
                        SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupFailed);
                        identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='actor_attribute_endpoint_missing'.");
                        factBridge.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='actor_attribute_endpoint_missing'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Missing ActorAttributeEndpoint actorId='{attributeContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    resolvedCount += 1;
                    ActorAttributeProfileAsset profile = attributeContribution.Profile ?? attributeEndpoint.AttributeProfile;
                    SessionActivityIdentity profileResolvedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeProfileResolved);
                    identityBridge.SetCurrentIdentity(profileResolvedIdentity, SessionActivityStage.ActorAttributeProfileResolved);
                    factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeProfileResolved, profileResolvedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute profile resolved actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' profileId='{(profile != null ? profile.ProfileId : "<none>")}'.");
                    factBridge.EmitSnapshot(snapshots, "actor_attribute_profile_resolved", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute profile resolved actorId='{attributeContribution.ActorId}' profileId='{(profile != null ? profile.ProfileId : "<none>")}'.");
                    DebugUtility.LogVerbose(typeof(ActivityEntryActorAttributeStage), $"event='ActorAttributeProfileResolved' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryActorAttributeStage' entryPipelineOwner='ActivityEntryPipeline' actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' profileId='{(profile != null ? profile.ProfileId : "<none>")}' resetIntent='{command.ResetIntent}' resetStateProfile='{command.StateProfileKind}' stateProfileSource='attribute_setup_state_profile' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

                    if (profile == null)
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupFailed);
                        identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' reason='attribute_profile_missing'.");
                        factBridge.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='attribute_profile_missing'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Missing ActorAttributeProfileAsset actorId='{attributeContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    if (!attributeEndpoint.TryInitialize(attributeContribution.ActorInstanceRuntimeId, startedIdentity, out ActorAttributeSetupResult setupResult))
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupFailed);
                        identityBridge.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' reason='{setupResult.Reason}'.");
                        factBridge.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='{setupResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Initialization failed actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' reason='{setupResult.Reason}'.");
                    }

                    if (setupResult.IsSkippedNoContent)
                    {
                        skippedCount += 1;
                        SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupSkipped);
                        identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                        factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' reason='{setupResult.Reason}'.");
                        factBridge.EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped actorId='{attributeContribution.ActorId}' reason='{setupResult.Reason}'.");
                        runtimeState.RemoveActiveActorAttributeCapability(
                            attributeContribution.ActorInstanceRuntimeId,
                            startedIdentity.ActivityId,
                            entrySequence,
                            "ActivityEntryActorAttributeStage",
                            "remove_active_actor_attribute_capability");
                        continue;
                    }

                    ActorAttributeMutationReceiverEndpoint mutationReceiver = ConfigureOptionalMutationReceiver(
                        attributeContribution,
                        attributeEndpoint,
                        startedIdentity,
                        command.Source,
                        command.Reason);

                    ActorDamageableEndpoint damageableEndpoint = ConfigureOptionalDamageableEndpoint(
                        attributeContribution,
                        attributeEndpoint,
                        mutationReceiver,
                        startedIdentity,
                        command.Source,
                        command.Reason);

                    ActorDamageSourceEndpoint damageSourceEndpoint = ConfigureOptionalDamageSourceEndpoint(
                        attributeContribution,
                        attributeEndpoint,
                        startedIdentity,
                        command.Source,
                        command.Reason);

                    runtimeState.StoreActiveActorAttributeCapability(
                        new SessionActivityPipeline.ActorAttributeCapabilityState(
                            attributeContribution.ActorInstanceRuntimeId,
                            attributeContribution.ActorId.Value,
                            attributeEndpoint,
                            mutationReceiver,
                            damageableEndpoint,
                            damageSourceEndpoint),
                        startedIdentity.ActivityId,
                        entrySequence,
                        "ActivityEntryActorAttributeStage",
                        "store_active_actor_attribute_capability");

                    SessionActivityIdentity readyIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeReady);
                    identityBridge.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActorAttributeReady);
                    factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeReady, readyIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute ready actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' attributeCount='{setupResult.AttributeCount}' attributeIds='{BuildActorAttributeIdList(attributeEndpoint)}'.");
                    factBridge.EmitSnapshot(snapshots, "actor_attribute_ready", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute ready actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' attributeCount='{setupResult.AttributeCount}'.");
                    DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"event='ActorAttributeReady' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryActorAttributeStage' entryPipelineOwner='ActivityEntryPipeline' actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' attributeCount='{setupResult.AttributeCount}' attributeIds='{BuildActorAttributeIdList(attributeEndpoint)}' resetIntent='{command.ResetIntent}' resetStateProfile='{command.StateProfileKind}' stateProfileSource='attribute_setup_state_profile' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                    readyCount += 1;
                }

                if (resolvedCount == 0 || readyCount == 0)
                {
                    skippedCount += 1;
                    SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupSkipped);
                    identityBridge.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                    factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_ready'.");
                    factBridge.EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_ready'.");
                }
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupCompleted);
            identityBridge.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorAttributeSetupCompleted);
            factBridge.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupCompleted, completedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup completed total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}' mode='SetupContributions'.");
            factBridge.EmitSnapshot(snapshots, "actor_attribute_setup_completed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup completed total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}'.");
            DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"event='ActorAttributeSetupCompleted' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryActorAttributeStage' entryPipelineOwner='ActivityEntryPipeline' resetIntent='{command.ResetIntent}' resetStateProfile='{command.StateProfileKind}' stateProfileSource='attribute_setup_state_profile' total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
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


        private static ActorAttributeMutationReceiverEndpoint ConfigureOptionalMutationReceiver(
            ActorAttributeSetupContribution attributeContribution,
            ActorAttributeEndpoint attributeEndpoint,
            SessionActivityIdentity startedIdentity,
            string source,
            string reason)
        {
            if (attributeEndpoint == null)
            {
                return null;
            }

            ActorAttributeMutationReceiverEndpoint mutationReceiver = ResolveMutationReceiver(attributeEndpoint);
            if (mutationReceiver == null)
            {
                return null;
            }

            mutationReceiver.Configure(
                attributeContribution.ActorId,
                attributeContribution.ActorInstanceRuntimeId,
                startedIdentity,
                attributeEndpoint,
                source,
                reason);

            return mutationReceiver;
        }

        private static ActorAttributeMutationReceiverEndpoint ResolveMutationReceiver(ActorAttributeEndpoint attributeEndpoint)
        {
            ActorAttributeMutationReceiverEndpoint mutationReceiver = attributeEndpoint.GetComponent<ActorAttributeMutationReceiverEndpoint>();
            if (mutationReceiver != null)
            {
                return mutationReceiver;
            }

            Actor actorRoot = attributeEndpoint.GetComponentInParent<Actor>(true);
            return actorRoot == null
                ? null
                : actorRoot.GetComponentInChildren<ActorAttributeMutationReceiverEndpoint>(true);
        }

        private static ActorDamageableEndpoint ConfigureOptionalDamageableEndpoint(
            ActorAttributeSetupContribution attributeContribution,
            ActorAttributeEndpoint attributeEndpoint,
            ActorAttributeMutationReceiverEndpoint mutationReceiver,
            SessionActivityIdentity startedIdentity,
            string source,
            string reason)
        {
            if (attributeEndpoint == null)
            {
                return null;
            }

            ActorDamageableEndpoint damageableEndpoint = ResolveDamageableEndpoint(attributeEndpoint);
            if (damageableEndpoint == null)
            {
                return null;
            }

            if (mutationReceiver == null || !mutationReceiver.IsConfigured)
            {
                throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorDamageableSetup] ActorDamageableEndpoint requires configured ActorAttributeMutationReceiverEndpoint actorId='{attributeContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{startedIdentity.EntrySequence}'.");
            }

            damageableEndpoint.Configure(
                attributeContribution.ActorId,
                attributeContribution.ActorInstanceRuntimeId,
                startedIdentity,
                mutationReceiver,
                source,
                reason);

            return damageableEndpoint;
        }

        private static ActorDamageableEndpoint ResolveDamageableEndpoint(ActorAttributeEndpoint attributeEndpoint)
        {
            ActorDamageableEndpoint damageableEndpoint = attributeEndpoint.GetComponent<ActorDamageableEndpoint>();
            if (damageableEndpoint != null)
            {
                return damageableEndpoint;
            }

            Actor actorRoot = attributeEndpoint.GetComponentInParent<Actor>(true);
            return actorRoot == null
                ? null
                : actorRoot.GetComponentInChildren<ActorDamageableEndpoint>(true);
        }


        private static ActorDamageSourceEndpoint ConfigureOptionalDamageSourceEndpoint(
            ActorAttributeSetupContribution attributeContribution,
            ActorAttributeEndpoint attributeEndpoint,
            SessionActivityIdentity startedIdentity,
            string source,
            string reason)
        {
            if (attributeEndpoint == null)
            {
                return null;
            }

            ActorDamageSourceEndpoint damageSourceEndpoint = ResolveDamageSourceEndpoint(attributeEndpoint);
            if (damageSourceEndpoint == null)
            {
                return null;
            }

            damageSourceEndpoint.Configure(
                attributeContribution.ActorId,
                attributeContribution.ActorInstanceRuntimeId,
                startedIdentity,
                source,
                reason);
            return damageSourceEndpoint;
        }

        private static ActorDamageSourceEndpoint ResolveDamageSourceEndpoint(ActorAttributeEndpoint attributeEndpoint)
        {
            ActorDamageSourceEndpoint damageSourceEndpoint = attributeEndpoint.GetComponent<ActorDamageSourceEndpoint>();
            if (damageSourceEndpoint != null)
            {
                return damageSourceEndpoint;
            }

            Actor actorRoot = attributeEndpoint.GetComponentInParent<Actor>(true);
            return actorRoot == null
                ? null
                : actorRoot.GetComponentInChildren<ActorDamageSourceEndpoint>(true);
        }

        private static SessionActivityIdentity BuildIdentity(
            ActivityEntryActorAttributeSetupCommand command,
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

        private static bool IsSameAttributeScope(
            SessionActivityIdentity startedIdentity,
            SessionActivityIdentity contributionIdentity)
        {
            return contributionIdentity.IsValid &&
                   string.Equals(startedIdentity.PipelineId, contributionIdentity.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(startedIdentity.SessionId, contributionIdentity.SessionId, StringComparison.Ordinal) &&
                   string.Equals(startedIdentity.ActivityId, contributionIdentity.ActivityId, StringComparison.Ordinal) &&
                   startedIdentity.EntrySequence == contributionIdentity.EntrySequence;
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
