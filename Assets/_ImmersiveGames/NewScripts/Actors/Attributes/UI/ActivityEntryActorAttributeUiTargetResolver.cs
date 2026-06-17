using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public sealed class ActivityEntryActorAttributeUiTargetResolver : IActorAttributeUiTargetResolver
    {
        public ActorAttributeUiTargetResolveResult Resolve(
            ActorAttributeUiBindingRequest request,
            ActivityParticipationContext participationContext,
            ActivityPlayerActorRegistry registry,
            string source,
            string reason)
        {
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);
            ActorAttributeUiTargetSelector selector = request.Selector;
            ActorAttributeUiTargetSelectorKind selectorKind = selector?.Kind ?? default;
            string selectorKindLabel = DescribeSelectorKind(selector);

            DebugUtility.LogVerbose(
                typeof(ActivityEntryActorAttributeUiTargetResolver),
                $"event='ActorAttributeUiTargetResolveStarted' selectorKind='{selectorKindLabel}' actorId='{DescribeSelectorActorId(selector)}' actorInstanceRuntimeId='{DescribeSelectorActorInstanceRuntimeId(selector)}' attributeId='{request.AttributeId}' source='{normalizedSource}' reason='{normalizedReason}'",
                DebugUtility.Colors.Info);

            if (!request.IsValid)
            {
                return Reject(
                    ActorAttributeUiTargetResolveResultKind.RejectedInvalidRequest,
                    selectorKind,
                    selectorKindLabel,
                    request,
                    normalizedSource,
                    normalizedReason,
                    request.GetInvalidReason());
            }

            if (participationContext == null || !participationContext.IsValid)
            {
                return Reject(
                    ActorAttributeUiTargetResolveResultKind.RejectedParticipationContextMissing,
                    selectorKind,
                    selectorKindLabel,
                    request,
                    normalizedSource,
                    normalizedReason,
                    "participation_context_missing");
            }

            if (registry == null)
            {
                return Reject(
                    ActorAttributeUiTargetResolveResultKind.RejectedRegistryMissing,
                    selectorKind,
                    selectorKindLabel,
                    request,
                    normalizedSource,
                    normalizedReason,
                    "registry_missing");
            }

            switch (selectorKind)
            {
                case ActorAttributeUiTargetSelectorKind.PrimaryPlayer:
                    return ResolvePrimaryPlayer(
                        request,
                        participationContext,
                        registry,
                        normalizedSource,
                        normalizedReason,
                        selectorKindLabel);
                case ActorAttributeUiTargetSelectorKind.ExplicitActorId:
                    return ResolveExplicitActorId(
                        request,
                        registry,
                        normalizedSource,
                        normalizedReason,
                        selectorKindLabel);
                case ActorAttributeUiTargetSelectorKind.ExplicitActorInstanceRuntimeId:
                    return ResolveExplicitActorInstanceRuntimeId(
                        request,
                        registry,
                        normalizedSource,
                        normalizedReason,
                        selectorKindLabel);
                default:
                    return Reject(
                        ActorAttributeUiTargetResolveResultKind.RejectedInvalidRequest,
                        selectorKind,
                        selectorKindLabel,
                        request,
                        normalizedSource,
                        normalizedReason,
                        "selector_kind_invalid");
            }
        }

        private static ActorAttributeUiTargetResolveResult ResolvePrimaryPlayer(
            ActorAttributeUiBindingRequest request,
            ActivityParticipationContext participationContext,
            ActivityPlayerActorRegistry registry,
            string source,
            string reason,
            string selectorKindLabel)
        {
            SessionParticipantId participantId = default;
            bool foundPrimaryPlayer = false;
            bool ambiguousPrimaryPlayer = false;

            IReadOnlyList<ActivityParticipantBinding> participants = participationContext.Participants;
            for (int index = 0; index < participants.Count; index++)
            {
                ActivityParticipantBinding participant = participants[index];
                if (!participant.IsValid ||
                    participant.Role != SessionParticipantRole.PrimaryPlayer)
                {
                    continue;
                }

                if (foundPrimaryPlayer)
                {
                    ambiguousPrimaryPlayer = true;
                    break;
                }

                foundPrimaryPlayer = true;
                participantId = participant.ParticipantId;
            }

            if (!foundPrimaryPlayer)
            {
                return Reject(
                    ActorAttributeUiTargetResolveResultKind.RejectedPrimaryPlayerMissing,
                    ActorAttributeUiTargetSelectorKind.PrimaryPlayer,
                    selectorKindLabel,
                    request,
                    source,
                    reason,
                    "primary_player_participant_missing");
            }

            if (ambiguousPrimaryPlayer)
            {
                return Reject(
                    ActorAttributeUiTargetResolveResultKind.RejectedPrimaryPlayerAmbiguous,
                    ActorAttributeUiTargetSelectorKind.PrimaryPlayer,
                    selectorKindLabel,
                    request,
                    source,
                    reason,
                    "primary_player_participant_ambiguous");
            }

            if (!registry.TryGetActiveHandleByParticipant(participantId, out PlayerActorRuntimeHandle handle) ||
                !handle.IsValid)
            {
                return Reject(
                    ActorAttributeUiTargetResolveResultKind.RejectedPrimaryPlayerMissing,
                    ActorAttributeUiTargetSelectorKind.PrimaryPlayer,
                    selectorKindLabel,
                    request,
                    source,
                    reason,
                    "primary_player_handle_missing");
            }

            return ResolveTarget(
                ActorAttributeUiTargetSelectorKind.PrimaryPlayer,
                request.AttributeId,
                handle.ActorId,
                handle.ActorInstanceRuntimeId,
                source,
                reason);
        }

        private static ActorAttributeUiTargetResolveResult ResolveExplicitActorId(
            ActorAttributeUiBindingRequest request,
            ActivityPlayerActorRegistry registry,
            string source,
            string reason,
            string selectorKindLabel)
        {
            ActorId explicitActorId = request.Selector.ExplicitActorId;
            if (!explicitActorId.IsValid)
            {
                return Reject(
                    ActorAttributeUiTargetResolveResultKind.RejectedExplicitActorIdMissing,
                    ActorAttributeUiTargetSelectorKind.ExplicitActorId,
                    selectorKindLabel,
                    request,
                    source,
                    reason,
                    "explicit_actor_id_missing");
            }

            bool found = false;
            PlayerActorRuntimeHandle resolvedHandle = default;
            IReadOnlyList<PlayerActorRuntimeHandle> handles = registry.GetIndexedActiveHandles();
            for (int index = 0; index < handles.Count; index++)
            {
                PlayerActorRuntimeHandle candidate = handles[index];
                if (!candidate.IsValid ||
                    candidate.ActorId != explicitActorId)
                {
                    continue;
                }

                if (found)
                {
                    return Reject(
                        ActorAttributeUiTargetResolveResultKind.RejectedExplicitActorIdAmbiguous,
                        ActorAttributeUiTargetSelectorKind.ExplicitActorId,
                        selectorKindLabel,
                        request,
                        source,
                        reason,
                        "explicit_actor_id_ambiguous");
                }

                found = true;
                resolvedHandle = candidate;
            }

            if (!found)
            {
                return Reject(
                    ActorAttributeUiTargetResolveResultKind.RejectedExplicitActorIdMissing,
                    ActorAttributeUiTargetSelectorKind.ExplicitActorId,
                    selectorKindLabel,
                    request,
                    source,
                    reason,
                    "explicit_actor_id_not_found");
            }

            return ResolveTarget(
                ActorAttributeUiTargetSelectorKind.ExplicitActorId,
                request.AttributeId,
                resolvedHandle.ActorId,
                resolvedHandle.ActorInstanceRuntimeId,
                source,
                reason);
        }

        private static ActorAttributeUiTargetResolveResult ResolveExplicitActorInstanceRuntimeId(
            ActorAttributeUiBindingRequest request,
            ActivityPlayerActorRegistry registry,
            string source,
            string reason,
            string selectorKindLabel)
        {
            ActorInstanceRuntimeId explicitActorInstanceRuntimeId = request.Selector.ExplicitActorInstanceRuntimeId;
            if (!explicitActorInstanceRuntimeId.IsValid)
            {
                return Reject(
                    ActorAttributeUiTargetResolveResultKind.RejectedExplicitActorInstanceRuntimeIdMissing,
                    ActorAttributeUiTargetSelectorKind.ExplicitActorInstanceRuntimeId,
                    selectorKindLabel,
                    request,
                    source,
                    reason,
                    "explicit_actor_instance_runtime_id_missing");
            }

            if (!registry.TryGetActiveHandleByActorInstance(explicitActorInstanceRuntimeId, out PlayerActorRuntimeHandle handle) ||
                !handle.IsValid)
            {
                return Reject(
                    ActorAttributeUiTargetResolveResultKind.RejectedExplicitActorInstanceRuntimeIdNotFound,
                    ActorAttributeUiTargetSelectorKind.ExplicitActorInstanceRuntimeId,
                    selectorKindLabel,
                    request,
                    source,
                    reason,
                    "explicit_actor_instance_runtime_id_not_found");
            }

            return ResolveTarget(
                ActorAttributeUiTargetSelectorKind.ExplicitActorInstanceRuntimeId,
                request.AttributeId,
                handle.ActorId,
                handle.ActorInstanceRuntimeId,
                source,
                reason);
        }

        private static ActorAttributeUiTargetResolveResult ResolveTarget(
            ActorAttributeUiTargetSelectorKind selectorKind,
            ActorAttributeId attributeId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string source,
            string reason)
        {
            ActorAttributeUiBindingTarget target = new(
                actorId,
                actorInstanceRuntimeId,
                attributeId,
                string.Empty);

            DebugUtility.LogVerbose(
                typeof(ActivityEntryActorAttributeUiTargetResolver),
                $"event='ActorAttributeUiTargetResolved' selectorKind='{selectorKind}' actorId='{actorId}' actorInstanceRuntimeId='{actorInstanceRuntimeId}' attributeId='{attributeId}' source='{source}' reason='{reason}' resolutionKind='{selectorKind}'",
                DebugUtility.Colors.Success);

            return ActorAttributeUiTargetResolveResult.Resolved(selectorKind, target, source, reason);
        }

        private static ActorAttributeUiTargetResolveResult Reject(
            ActorAttributeUiTargetResolveResultKind kind,
            ActorAttributeUiTargetSelectorKind selectorKind,
            string selectorKindLabel,
            ActorAttributeUiBindingRequest request,
            string source,
            string reason,
            string failureReason)
        {
            string normalizedFailureReason = Normalize(failureReason);
            DebugUtility.LogWarning(
                typeof(ActivityEntryActorAttributeUiTargetResolver),
                $"event='ActorAttributeUiTargetResolveRejected' selectorKind='{selectorKindLabel}' actorId='{DescribeSelectorActorId(request.Selector)}' actorInstanceRuntimeId='{DescribeSelectorActorInstanceRuntimeId(request.Selector)}' attributeId='{request.AttributeId}' source='{source}' reason='{reason}' failureReason='{normalizedFailureReason}'");

            return ActorAttributeUiTargetResolveResult.Rejected(
                kind,
                selectorKind,
                source,
                reason,
                normalizedFailureReason);
        }

        private static string DescribeSelectorActorId(ActorAttributeUiTargetSelector selector)
        {
            if (selector == null ||
                selector.Kind != ActorAttributeUiTargetSelectorKind.ExplicitActorId)
            {
                return string.Empty;
            }

            return selector.ExplicitActorId.IsValid ? selector.ExplicitActorId.ToString() : string.Empty;
        }

        private static string DescribeSelectorActorInstanceRuntimeId(ActorAttributeUiTargetSelector selector)
        {
            if (selector == null ||
                selector.Kind != ActorAttributeUiTargetSelectorKind.ExplicitActorInstanceRuntimeId)
            {
                return string.Empty;
            }

            return selector.ExplicitActorInstanceRuntimeId.IsValid
                ? selector.ExplicitActorInstanceRuntimeId.ToString()
                : string.Empty;
        }

        private static string DescribeSelectorKind(ActorAttributeUiTargetSelector selector)
        {
            return selector == null ? "missing" : selector.Kind.ToString();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
