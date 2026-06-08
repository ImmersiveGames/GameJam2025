using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Authoring;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
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
            IActivityEntryRuntimeBridge endpoint,
            ActivityActorExitRuntimeState runtimeState,
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

            if (runtimeState == null)
            {
                throw new ArgumentNullException(nameof(runtimeState));
            }

            IReadOnlyList<ActorAttributeSetupContribution> attributeContributions = command.AttributeSetupContributions ?? Array.Empty<ActorAttributeSetupContribution>();
            int entrySequence = command.Identity.EntrySequence;
            SessionActivityIdentity startedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupStarted);
            endpoint.SetCurrentIdentity(startedIdentity, SessionActivityStage.ActorAttributeSetupStarted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupStarted, startedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup started mode='SetupContributions'.");
            endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_started", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup started.");
            DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"[OBS][ActivityEntryPipeline][ActorAttribute] event='ActorAttributeSetupStarted' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' source='{command.Source}' reason='{command.Reason}' mode='SetupContributions'.", DebugUtility.Colors.Info);

            int totalCount = attributeContributions.Count;
            int resolvedCount = 0;
            int readyCount = 0;
            int skippedCount = 0;
            int failedCount = 0;

            if (attributeContributions.Count == 0)
            {
                SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupSkipped);
                endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped reason='no_attribute_setup_contributions'.");
                endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped reason='no_attribute_setup_contributions'.");
                DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"[OBS][ActivityEntryPipeline][ActorAttribute] event='ActorAttributeSetupSkipped' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' reason='no_attribute_setup_contributions' source='{command.Source}' reasonDetail='{command.Reason}'.", DebugUtility.Colors.Info);
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
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' reason='attribute_contribution_invalid'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='attribute_contribution_invalid'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Invalid attribute contribution actorId='{attributeContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    if (!IsSameAttributeScope(startedIdentity, attributeContribution.Identity))
                    {
                        SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupFailed);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='attribute_contribution_identity_mismatch'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='attribute_contribution_identity_mismatch'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Attribute contribution identity mismatch actorId='{attributeContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    ActorAttributeEndpoint attributeEndpoint = attributeContribution.Endpoint;
                    if (attributeEndpoint == null)
                    {
                        SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupFailed);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='actor_attribute_endpoint_missing'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='actor_attribute_endpoint_missing'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Missing ActorAttributeEndpoint actorId='{attributeContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    resolvedCount += 1;
                    ActorAttributeProfileAsset profile = attributeContribution.Profile ?? attributeEndpoint.AttributeProfile;
                    SessionActivityIdentity profileResolvedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeProfileResolved);
                    endpoint.SetCurrentIdentity(profileResolvedIdentity, SessionActivityStage.ActorAttributeProfileResolved);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeProfileResolved, profileResolvedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute profile resolved actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' profileId='{(profile != null ? profile.ProfileId : "<none>")}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_attribute_profile_resolved", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute profile resolved actorId='{attributeContribution.ActorId}' profileId='{(profile != null ? profile.ProfileId : "<none>")}'.");
                    DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"[OBS][ActivityEntryPipeline][ActorAttribute] event='ActorAttributeProfileResolved' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' profileId='{(profile != null ? profile.ProfileId : "<none>")}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Info);

                    if (profile == null)
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupFailed);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' reason='attribute_profile_missing'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='attribute_profile_missing'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Missing ActorAttributeProfileAsset actorId='{attributeContribution.ActorId}' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}'.");
                    }

                    if (!attributeEndpoint.TryInitialize(attributeContribution.ActorInstanceRuntimeId, startedIdentity, out ActorAttributeSetupResult setupResult))
                    {
                        failedCount += 1;
                        SessionActivityIdentity failedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupFailed);
                        endpoint.SetCurrentIdentity(failedIdentity, SessionActivityStage.ActorAttributeSetupFailed);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupFailed, failedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' reason='{setupResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_failed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup failed actorId='{attributeContribution.ActorId}' reason='{setupResult.Reason}'.");
                        throw new InvalidOperationException($"[FATAL][ActivityEntryActorAttributeStage][ActorAttributeSetup] Initialization failed actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' reason='{setupResult.Reason}'.");
                    }

                    if (setupResult.IsSkippedNoContent)
                    {
                        skippedCount += 1;
                        SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupSkipped);
                        endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                        endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' reason='{setupResult.Reason}'.");
                        endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped actorId='{attributeContribution.ActorId}' reason='{setupResult.Reason}'.");
                        runtimeState.RemoveActiveActorAttributeCapability(
                            attributeContribution.ActorInstanceRuntimeId,
                            startedIdentity.ActivityId,
                            entrySequence,
                            "ActivityEntryActorAttributeStage",
                            "remove_active_actor_attribute_capability");
                        continue;
                    }

                    runtimeState.StoreActiveActorAttributeCapability(
                        new SessionActivityPipeline.ActorAttributeCapabilityState(
                            attributeContribution.ActorInstanceRuntimeId,
                            attributeContribution.ActorId.Value,
                            attributeEndpoint),
                        startedIdentity.ActivityId,
                        entrySequence,
                        "ActivityEntryActorAttributeStage",
                        "store_active_actor_attribute_capability");

                    SessionActivityIdentity readyIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeReady);
                    endpoint.SetCurrentIdentity(readyIdentity, SessionActivityStage.ActorAttributeReady);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeReady, readyIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute ready actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' attributeCount='{setupResult.AttributeCount}' attributeIds='{BuildActorAttributeIdList(attributeEndpoint)}'.");
                    endpoint.EmitSnapshot(snapshots, "actor_attribute_ready", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute ready actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' attributeCount='{setupResult.AttributeCount}'.");
                    DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"[OBS][ActivityEntryPipeline][ActorAttribute] event='ActorAttributeReady' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' actorId='{attributeContribution.ActorId}' actorKind='{attributeContribution.ActorKind}' attributeCount='{setupResult.AttributeCount}' attributeIds='{BuildActorAttributeIdList(attributeEndpoint)}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
                    readyCount += 1;
                }

                if (resolvedCount == 0 || readyCount == 0)
                {
                    skippedCount += 1;
                    SessionActivityIdentity skippedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupSkipped);
                    endpoint.SetCurrentIdentity(skippedIdentity, SessionActivityStage.ActorAttributeSetupSkipped);
                    endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupSkipped, skippedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_ready'.");
                    endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_skipped", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup skipped reason='no_attribute_endpoint_ready'.");
                }
            }

            SessionActivityIdentity completedIdentity = BuildIdentity(command, SessionActivityStage.ActorAttributeSetupCompleted);
            endpoint.SetCurrentIdentity(completedIdentity, SessionActivityStage.ActorAttributeSetupCompleted);
            endpoint.EmitFact(facts, SessionActivityFactKind.ActorAttributeSetupCompleted, completedIdentity, command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup completed total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}' mode='SetupContributions'.");
            endpoint.EmitSnapshot(snapshots, "actor_attribute_setup_completed", command.Source, command.Reason, $"'{startedIdentity.ActivityId}' actor attribute setup completed total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}'.");
            DebugUtility.Log(typeof(ActivityEntryActorAttributeStage), $"[OBS][ActivityEntryPipeline][ActorAttribute] event='ActorAttributeSetupCompleted' activityId='{startedIdentity.ActivityId}' entrySequence='{entrySequence}' owner='ActivityEntryPipeline' total='{totalCount}' resolved='{resolvedCount}' ready='{readyCount}' skipped='{skippedCount}' failed='{failedCount}' source='{command.Source}' reason='{command.Reason}'.", DebugUtility.Colors.Success);
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
