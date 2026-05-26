using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    internal static class NonPlayerActorParticipationStage
    {
        public static SessionActivityPipeline.NonPlayerActorParticipationEnterStageResult ExecuteEnter(
            SessionActivityIdentity identity,
            string activityId,
            ActivityNonPlayerActorRegistry registry,
            IReadOnlyDictionary<string, ActorPresentationRuntimeHandle> presentationHandles,
            IReadOnlyDictionary<string, SessionActivityPipeline.NonPlayerActorAttributeCapabilityState> attributeCapabilities)
        {
            IReadOnlyList<NonPlayerActorRuntimeEntry> entries = registry.GetActiveEntries(identity);
            List<SessionActivityPipeline.NonPlayerActorParticipationEnterOutcome> outcomes = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                NonPlayerActorRuntimeEntry entry = entries[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                if (!IsEligible(activityId, entry, out string eligibilityReason))
                {
                    outcomes.Add(new SessionActivityPipeline.NonPlayerActorParticipationEnterOutcome(entry, entered: false, ready: false, reasonCode: eligibilityReason));
                    continue;
                }

                if (!IsReady(identity, entry, registry, presentationHandles, attributeCapabilities, out string readinessReason))
                {
                    outcomes.Add(new SessionActivityPipeline.NonPlayerActorParticipationEnterOutcome(entry, entered: false, ready: false, reasonCode: readinessReason));
                    continue;
                }

                registry.MarkParticipationEntered(identity, entry.ActorIdentity.NonPlayerActorId);
                outcomes.Add(new SessionActivityPipeline.NonPlayerActorParticipationEnterOutcome(entry, entered: true, ready: true, reasonCode: "ready"));
            }

            return new SessionActivityPipeline.NonPlayerActorParticipationEnterStageResult(outcomes);
        }

        public static SessionActivityPipeline.NonPlayerActorParticipationExitStageResult ExecuteExit(
            SessionActivityIdentity identity,
            ActivityNonPlayerActorRegistry registry)
        {
            IReadOnlyList<NonPlayerActorRuntimeEntry> entries = registry.GetActiveEntries(identity);
            List<SessionActivityPipeline.NonPlayerActorParticipationExitOutcome> outcomes = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                NonPlayerActorRuntimeEntry entry = entries[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                registry.MarkParticipationExited(identity, entry.ActorIdentity.NonPlayerActorId);
                outcomes.Add(new SessionActivityPipeline.NonPlayerActorParticipationExitOutcome(entry, exited: true));
            }

            return new SessionActivityPipeline.NonPlayerActorParticipationExitStageResult(outcomes);
        }

        private static bool IsEligible(string activityId, NonPlayerActorRuntimeEntry entry, out string reasonCode)
        {
            switch (entry.ActorIdentity.ParticipationPolicy)
            {
                case NonPlayerActorParticipationPolicy.Disabled:
                    reasonCode = "policy_disabled";
                    return false;
                case NonPlayerActorParticipationPolicy.AllActivitiesInRoute:
                    reasonCode = "eligible";
                    return true;
                case NonPlayerActorParticipationPolicy.ExplicitActivityIds:
                    {
                        IReadOnlyList<string> activityIds = entry.ActorIdentity.ParticipatingActivityIds;
                        for (int index = 0; index < activityIds.Count; index++)
                        {
                            if (string.Equals(Normalize(activityIds[index]), Normalize(activityId), StringComparison.Ordinal))
                            {
                                reasonCode = "eligible";
                                return true;
                            }
                        }

                        reasonCode = "activity_not_listed";
                        return false;
                    }
                default:
                    reasonCode = "unsupported_participation_policy";
                    return false;
            }
        }

        private static bool IsReady(
            SessionActivityIdentity identity,
            NonPlayerActorRuntimeEntry entry,
            ActivityNonPlayerActorRegistry registry,
            IReadOnlyDictionary<string, ActorPresentationRuntimeHandle> presentationHandles,
            IReadOnlyDictionary<string, SessionActivityPipeline.NonPlayerActorAttributeCapabilityState> attributeCapabilities,
            out string reasonCode)
        {
            if (entry.Endpoint == null || !entry.Endpoint.IsValid)
            {
                reasonCode = "endpoint_invalid";
                return false;
            }

            if (!registry.TryGetActive(identity, entry.ActorIdentity.NonPlayerActorId, out NonPlayerActorRuntimeEntry registryEntry) || !registryEntry.IsValid)
            {
                reasonCode = "registry_entry_missing";
                return false;
            }

            ActorPresentationProfileAsset profile = entry.Endpoint.PresentationProfile;
            if (profile == null)
            {
                reasonCode = "presentation_profile_missing";
                return false;
            }

            if (profile.IsRequired)
            {
                if (presentationHandles == null ||
                    !presentationHandles.TryGetValue(entry.ActorIdentity.NonPlayerActorId, out ActorPresentationRuntimeHandle handle) ||
                    !handle.IsValid)
                {
                    reasonCode = "required_presentation_not_ready";
                    return false;
                }
            }

            ActorAttributeEndpoint attributeEndpoint = entry.ActorInstance.GetComponent<ActorAttributeEndpoint>();
            if (attributeEndpoint != null)
            {
                if (attributeCapabilities == null ||
                    !attributeCapabilities.TryGetValue(entry.ActorIdentity.NonPlayerActorId, out SessionActivityPipeline.NonPlayerActorAttributeCapabilityState capabilityState))
                {
                    reasonCode = "required_attribute_not_ready";
                    return false;
                }

                if (!capabilityState.IsValid || capabilityState.Endpoint != attributeEndpoint)
                {
                    reasonCode = "required_attribute_capability_invalid";
                    return false;
                }
            }

            reasonCode = "ready";
            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
