using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
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

            var playerPreparationHandoff = BuildPlayerPreparationHandoff(request.PlayerPreparation, request.PlayerTechnicalEntries);
            SessionActivityEntryHandoff handoff = new(
                string.Empty,
                0,
                0,
                request.SessionStateId,
                playerPreparationHandoff,
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

        private static SessionActivityPlayerPreparationHandoff BuildPlayerPreparationHandoff(
            PlayerPreparationSnapshot snapshot,
            IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> technicalEntries)
        {
            return new SessionActivityPlayerPreparationHandoff(
                snapshot.Identity.PipelineId,
                snapshot.Identity.SessionId,
                snapshot.Identity.RouteIdentity,
                snapshot.Identity.RouteOperationId,
                snapshot.Identity.TransitionId,
                snapshot.Identity.RouteSequence,
                FormatPlayerPreparationOutcome(snapshot.Outcome),
                snapshot.ParticipationKind.ToString(),
                snapshot.PlannedPlayersCount,
                snapshot.RequiredPlayersCount,
                snapshot.OptionalPlayersCount,
                snapshot.MaterializedPlayersCount,
                snapshot.SkippedPlayersCount,
                snapshot.PendingRequiredPlayersCount,
                BuildParticipantIds(snapshot.PlannedEntries),
                BuildPlayerTechnicalPlanEntries(technicalEntries));
        }

        private static IReadOnlyList<SessionParticipantId> BuildParticipantIds(IReadOnlyList<PlayerPlannedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<SessionParticipantId>();
            }

            List<SessionParticipantId> participantIds = new(entries.Count);
            HashSet<string> unique = new(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                SessionParticipantId participantId = new(entries[i].PlayerId);
                if (!participantId.IsValid || !unique.Add(participantId.Value))
                {
                    continue;
                }

                participantIds.Add(participantId);
            }

            return participantIds;
        }

        private static IReadOnlyList<SessionActivityPlayerTechnicalPlanEntry> BuildPlayerTechnicalPlanEntries(
            IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<SessionActivityPlayerTechnicalPlanEntry>();
            }

            List<SessionActivityPlayerTechnicalPlanEntry> technicalEntries = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                technicalEntries.Add(new SessionActivityPlayerTechnicalPlanEntry(
                    entry.PlayerId,
                    entry.Required,
                    entry.Prefab,
                    entry.PlacementMode,
                    entry.PlacementId,
                    entry.LocalPosition,
                    entry.LocalRotation));
            }

            return technicalEntries;
        }

        private static string FormatPlayerPreparationOutcome(PlayerPreparationOutcome outcome)
        {
            return outcome switch
            {
                PlayerPreparationOutcome.ObservedNoOp => "observed_noop",
                PlayerPreparationOutcome.PlannedOnly => "planned_only",
                PlayerPreparationOutcome.Materialized => "materialized",
                _ => "unknown",
            };
        }
    }
}
