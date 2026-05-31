using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;

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
                BuildPlayerTechnicalPlanEntries(request.PlayerTechnicalEntries, request.SessionParticipationContext),
                new SessionActivityRouteTransitionContext(
                    request.HasRouteFadeProfile && request.RouteFadeProfile != null,
                    request.RouteFadeProfile,
                    request.HasRouteLoadingProfile && request.RouteLoadingProfile != null,
                    request.RouteLoadingProfile),
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

        private static IReadOnlyList<SessionActivityPlayerTechnicalPlanEntry> BuildPlayerTechnicalPlanEntries(
            IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> entries,
            SessionParticipationContext sessionParticipationContext)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<SessionActivityPlayerTechnicalPlanEntry>();
            }

            if (sessionParticipationContext == null || !sessionParticipationContext.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] SessionParticipationContext is required to build player technical plan entries.");
            }

            List<SessionActivityPlayerTechnicalPlanEntry> technicalEntries = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                SessionParticipantBinding participant = ResolveSessionParticipantForTechnicalEntryOrFail(entry, sessionParticipationContext);
                string participantId = participant.ParticipantId.Value;
                technicalEntries.Add(new SessionActivityPlayerTechnicalPlanEntry(
                    participantId,
                    entry.Required,
                    entry.Prefab,
                    entry.PlacementMode,
                    entry.PlacementId,
                    entry.LocalPosition,
                    entry.LocalRotation));

                DebugUtility.Log(typeof(SessionActivityOperationalRouteConsumerEntryAdapter),
                    $"[OBS][SessionOperationalPipeline][PlayerParticipation] event='PlayerActorTechnicalPlanEntryResolved' participantId='{participant.ParticipantId}' role='{participant.Role}' playerSlotId='{participant.PlayerSlotId}' actorDefinitionId='{participant.ActorDefinitionId}' actorId='{participant.ActorId}' seedEntryId='{entry.PlayerId}' resolutionKey='SessionParticipantId'.");
            }

            return technicalEntries;
        }

        private static SessionParticipantBinding ResolveSessionParticipantForTechnicalEntryOrFail(
            PlayerSetDefinitionAsset.PlayerActorResolvedEntry entry,
            SessionParticipationContext sessionParticipationContext)
        {
            string seedEntryId = Normalize(entry.PlayerId);
            if (string.IsNullOrWhiteSpace(seedEntryId))
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] Player technical seed entry id is required.");
            }

            IReadOnlyList<SessionParticipantBinding> participants = sessionParticipationContext.Participants ?? Array.Empty<SessionParticipantBinding>();
            for (int index = 0; index < participants.Count; index++)
            {
                SessionParticipantBinding participant = participants[index];
                if (!participant.IsValid || !participant.ParticipantId.IsValid)
                {
                    continue;
                }

                string actorDefinitionId = participant.ActorDefinitionId.IsValid ? Normalize(participant.ActorDefinitionId.Value) : string.Empty;
                string actorId = participant.ActorId.IsValid ? Normalize(participant.ActorId.Value) : string.Empty;
                if (string.Equals(actorDefinitionId, seedEntryId, StringComparison.Ordinal) ||
                    string.Equals(actorId, seedEntryId, StringComparison.Ordinal))
                {
                    return participant;
                }
            }

            throw new InvalidOperationException(
                $"[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] Missing SessionParticipantBinding for player technical seed entry seedEntryId='{seedEntryId}' routeOperationId='{sessionParticipationContext.RouteOperationId}'.");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
