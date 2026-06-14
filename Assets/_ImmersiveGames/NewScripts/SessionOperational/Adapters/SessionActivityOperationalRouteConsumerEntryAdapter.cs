using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public sealed class SessionActivityOperationalRouteConsumerEntryAdapter : IOperationalRouteConsumerEntryPort
    {
        private readonly Func<ISessionActivityEntryHandoffReceiver> _receiverResolver;

        public SessionActivityOperationalRouteConsumerEntryAdapter(Func<ISessionActivityEntryHandoffReceiver> receiverResolver)
        {
            _receiverResolver = receiverResolver ?? throw new ArgumentNullException(nameof(receiverResolver));
        }

        public Task<OperationalRouteConsumerEntryResult> RequestEntryAsync(
            OperationalRouteConsumerEntryRequest request,
            CancellationToken cancellationToken)
        {
            if (!request.IsValid)
            {
                return Task.FromResult(new OperationalRouteConsumerEntryResult(
                    OperationalRouteConsumerEntryResultKind.Failed,
                    "operational_route_consumer_entry_request_invalid",
                    "OperationalRouteConsumerEntryRequest is invalid."));
            }

            var receiver = ResolveReceiverOrFail();
            if (!string.Equals(receiver.SessionId, request.SessionStateId, StringComparison.Ordinal))
            {
                return Task.FromResult(new OperationalRouteConsumerEntryResult(
                    OperationalRouteConsumerEntryResultKind.Failed,
                    "session_state_mismatch",
                    $"requestSessionStateId='{request.SessionStateId}' receiverSessionId='{receiver.SessionId}'."));
            }

            SessionActivityEntryHandoff handoff = new(
                string.Empty,
                0,
                0,
                request.SessionStateId,
                request.SessionParticipationContext,
                BuildActorMaterializationPlanEntries(request.ActorMaterializationSeedEntries, request.SessionParticipationContext),
                new SessionActivityRouteTransitionContext(
                    request.HasRouteFadeProfile && request.RouteFadeProfile != null,
                    request.RouteFadeProfile,
                    request.HasRouteLoadingProfile && request.RouteLoadingProfile != null,
                    request.RouteLoadingProfile),
                request.LoadedSnapshotPayloadContext,
                request.Source,
                request.Reason);

            var activityResult = receiver.StartFromPreparedHandoff(handoff, request.Source, request.Reason);
            if (!activityResult.IsValid)
            {
                return Task.FromResult(new OperationalRouteConsumerEntryResult(
                    OperationalRouteConsumerEntryResultKind.Failed,
                    "session_activity_result_invalid",
                    $"resultKind='{activityResult.Kind}' resultReason='{activityResult.Reason}'."));
            }

            if (activityResult.IsRejected)
            {
                return Task.FromResult(new OperationalRouteConsumerEntryResult(
                    OperationalRouteConsumerEntryResultKind.Rejected,
                    "session_activity_rejected_handoff",
                    $"resultKind='{activityResult.Kind}' resultReason='{activityResult.Reason}'."));
            }

            return Task.FromResult(new OperationalRouteConsumerEntryResult(
                OperationalRouteConsumerEntryResultKind.Completed,
                "completed",
                $"resultKind='{activityResult.Kind}' resultReason='{activityResult.Reason}'."));
        }

        private ISessionActivityEntryHandoffReceiver ResolveReceiverOrFail()
        {
            var receiver = _receiverResolver();
            if (receiver == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] ISessionActivityEntryHandoffReceiver obrigatorio ausente para consumer entry adapter operacional.");
            }

            return receiver;
        }

        private static IReadOnlyList<SessionActivityActorMaterializationPlanEntry> BuildActorMaterializationPlanEntries(
            IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> entries,
            SessionParticipationContext sessionParticipationContext)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<SessionActivityActorMaterializationPlanEntry>();
            }

            if (sessionParticipationContext == null || !sessionParticipationContext.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] SessionParticipationContext is required to build actor materialization plan entries.");
            }

            List<SessionActivityActorMaterializationPlanEntry> materializationPlanEntries = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                var participant = ResolveSessionParticipantForMaterializationSeedOrFail(entry, sessionParticipationContext);
                materializationPlanEntries.Add(new SessionActivityActorMaterializationPlanEntry(
                    participant.ParticipantId,
                    entry.Required,
                    entry.Prefab,
                    entry.PlacementMode,
                    entry.PlacementId,
                    entry.LocalPosition,
                    entry.LocalRotation));

                DebugUtility.LogVerbose(typeof(SessionActivityOperationalRouteConsumerEntryAdapter),
                    $"event='ActorMaterializationPlanEntryResolved' participantId='{participant.ParticipantId}' role='{participant.Role}' playerSlotId='{participant.PlayerSlotId}' actorDefinitionId='{participant.ActorDefinitionId}' actorId='{participant.ActorId}' seedPlayerSlotId='{entry.PlayerSlotId}' seedActorDefinitionId='{entry.ActorDefinitionId}' seedActorId='{entry.ActorId}' actorIdSource='PlayerSetDefinitionEntry' actorScope='{participant.ActorScope}' resolutionKey='PlayerSlotIdToSessionParticipantId'.");
            }

            return materializationPlanEntries;
        }

        private static SessionParticipantBinding ResolveSessionParticipantForMaterializationSeedOrFail(
            PlayerSetDefinitionAsset.PlayerActorResolvedEntry entry,
            SessionParticipationContext sessionParticipationContext)
        {
            var seedPlayerSlotId = entry.PlayerSlotId;
            if (!seedPlayerSlotId.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] Actor materialization seed playerSlotId is required.");
            }

            var seedActorDefinitionId = entry.ActorDefinitionId;
            if (!seedActorDefinitionId.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] Actor materialization seed actorDefinitionId is required.");
            }

            SessionParticipantBinding matchedParticipant = default;
            int matchCount = 0;
            IReadOnlyList<SessionParticipantBinding> participants = sessionParticipationContext.Participants ?? Array.Empty<SessionParticipantBinding>();
            for (int index = 0; index < participants.Count; index++)
            {
                var participant = participants[index];
                if (!participant.IsValid || !participant.ParticipantId.IsValid)
                {
                    continue;
                }

                if (participant.PlayerSlotId != seedPlayerSlotId)
                {
                    continue;
                }

                matchedParticipant = participant;
                matchCount++;
            }

            if (matchCount == 0)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] Missing SessionParticipantBinding for actor materialization seed entry seedPlayerSlotId='{seedPlayerSlotId}' seedActorDefinitionId='{seedActorDefinitionId}' routeOperationId='{sessionParticipationContext.RouteOperationId}'.");
            }

            if (matchCount > 1)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] Duplicate SessionParticipantBinding for actor materialization seed entry seedPlayerSlotId='{seedPlayerSlotId}' routeOperationId='{sessionParticipationContext.RouteOperationId}' matchCount='{matchCount}'.");
            }

            if (matchedParticipant.ActorDefinitionId != seedActorDefinitionId)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] SessionParticipantBinding actorDefinitionId mismatch for actor materialization seed entry playerSlotId='{seedPlayerSlotId}' seedActorDefinitionId='{seedActorDefinitionId}' participantActorDefinitionId='{matchedParticipant.ActorDefinitionId}' participantId='{matchedParticipant.ParticipantId}' routeOperationId='{sessionParticipationContext.RouteOperationId}'.");
            }

            return matchedParticipant;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
