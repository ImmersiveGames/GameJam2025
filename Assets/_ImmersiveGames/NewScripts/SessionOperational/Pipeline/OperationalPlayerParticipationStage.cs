using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.PlayerParticipation.Runtime;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalPlayerParticipationResultKind
    {
        Unknown = 0,
        Completed = 1,
        SkippedNoHandoff = 2,
        Failed = 3,
    }

    public readonly struct OperationalPlayerParticipationResult
    {
        public OperationalPlayerParticipationResult(
            OperationalPlayerParticipationResultKind kind,
            PlayerParticipationResult playerParticipationResult,
            SessionParticipationContext sessionParticipationContext)
        {
            Kind = kind;
            PlayerParticipationResult = playerParticipationResult;
            SessionParticipationContext = sessionParticipationContext;
        }

        public OperationalPlayerParticipationResultKind Kind { get; }
        public PlayerParticipationResult PlayerParticipationResult { get; }
        public SessionParticipationContext SessionParticipationContext { get; }
        public bool HasSessionParticipationContext => SessionParticipationContext is { IsValid: true };
        public bool IsCompleted =>
            Kind == OperationalPlayerParticipationResultKind.Completed &&
            PlayerParticipationResult.IsValid &&
            HasSessionParticipationContext;
        public bool IsSkipped => Kind == OperationalPlayerParticipationResultKind.SkippedNoHandoff;
        public bool IsAccepted => IsCompleted || IsSkipped;
    }

    public readonly struct OperationalPlayerParticipationCommand
    {
        public OperationalPlayerParticipationCommand(
            string pipelineId,
            SessionOperationalRouteCommand routeCommand,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            PipelineId = Normalize(pipelineId);
            RouteCommand = routeCommand;
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string PipelineId { get; }
        public SessionOperationalRouteCommand RouteCommand { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            RouteCommand.IsValid &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalPlayerParticipationStage
    {
        private readonly Func<IRoutePlayerParticipationEndpoint> _routePlayerParticipationEndpointResolver;
        private readonly Func<IPlayerParticipationRuntime> _playerParticipationRuntimeResolver;

        public OperationalPlayerParticipationStage(
            Func<IRoutePlayerParticipationEndpoint> routePlayerParticipationEndpointResolver,
            Func<IPlayerParticipationRuntime> playerParticipationRuntimeResolver)
        {
            _routePlayerParticipationEndpointResolver = routePlayerParticipationEndpointResolver ?? throw new ArgumentNullException(nameof(routePlayerParticipationEndpointResolver));
            _playerParticipationRuntimeResolver = playerParticipationRuntimeResolver ?? throw new ArgumentNullException(nameof(playerParticipationRuntimeResolver));
        }

        public OperationalPlayerParticipationResult Execute(OperationalPlayerParticipationCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalPlayerParticipationCommand is invalid.");
            }

            if (command.RouteCommand.CompletionHandoff != SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
            {
                return new OperationalPlayerParticipationResult(
                    OperationalPlayerParticipationResultKind.SkippedNoHandoff,
                    default,
                    SessionParticipationContext.Empty(command.RouteIdentity, command.RouteOperationId, command.Source, command.Reason));
            }

            if (string.IsNullOrWhiteSpace(command.RouteCommand.HandoffSessionStateId))
            {
                throw new InvalidOperationException("handoffSessionStateId is required when completionHandoff=SessionActivityEntry.");
            }

            PlayerParticipationSeedIdentity playerParticipationIdentity = new(
                command.PipelineId,
                command.RouteCommand.HandoffSessionStateId,
                command.RouteIdentity,
                command.RouteOperationId,
                command.RouteSequence,
                command.TransitionId);

            PlayerParticipationPlan playerParticipationPlan = new(
                playerParticipationIdentity,
                true,
                new PlayerSet(ResolvePlayerSetFromPlan(command.RouteCommand.Plan)),
                command.Source,
                command.Reason);

            LogPlayerParticipationStarted(command, playerParticipationIdentity);

            var routePlayerParticipationEndpoint = ResolveRoutePlayerParticipationEndpointOrFail(command);
            var playerParticipationResult = routePlayerParticipationEndpoint.Execute(playerParticipationPlan);
            if (!playerParticipationResult.IsValid)
            {
                throw new InvalidOperationException("PlayerParticipationStage returned an invalid result.");
            }

            var candidateSessionParticipationContext = BuildSessionParticipationContext(command, playerParticipationResult);
            if (candidateSessionParticipationContext == null || !candidateSessionParticipationContext.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][SessionOperationalPipeline][PlayerParticipation] SessionParticipationContext candidate invalid routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}'.");
            }

            var playerParticipationRuntime = ResolvePlayerParticipationRuntimeOrFail(command);
            var runtimeContextResult = playerParticipationRuntime.ResolveOrStoreSessionContext(
                command.RouteCommand.HandoffSessionStateId,
                candidateSessionParticipationContext,
                command.RouteIdentity,
                command.RouteOperationId,
                command.Source,
                command.Reason);
            if (!runtimeContextResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][SessionOperationalPipeline][PlayerParticipation] PlayerParticipationRuntime returned invalid context routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}'.");
            }

            var sessionParticipationContext = runtimeContextResult.Context;

            LogPlayerParticipationSeedResolved(command, playerParticipationIdentity, playerParticipationResult, sessionParticipationContext);
            LogPlayerParticipationRuntimeContextResolved(command, runtimeContextResult);
            LogSessionParticipationContextPrepared(command, sessionParticipationContext);
            LogPlayerParticipationCompleted(command, playerParticipationIdentity, playerParticipationResult);

            return new OperationalPlayerParticipationResult(
                OperationalPlayerParticipationResultKind.Completed,
                playerParticipationResult,
                sessionParticipationContext);
        }

        private IRoutePlayerParticipationEndpoint ResolveRoutePlayerParticipationEndpointOrFail(OperationalPlayerParticipationCommand command)
        {
            var routePlayerParticipationEndpoint = _routePlayerParticipationEndpointResolver();
            if (routePlayerParticipationEndpoint == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] IRoutePlayerParticipationEndpoint obrigatorio ausente routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}'.");
            }

            return routePlayerParticipationEndpoint;
        }

        private IPlayerParticipationRuntime ResolvePlayerParticipationRuntimeOrFail(OperationalPlayerParticipationCommand command)
        {
            var playerParticipationRuntime = _playerParticipationRuntimeResolver();
            if (playerParticipationRuntime == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] IPlayerParticipationRuntime obrigatorio ausente routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}'.");
            }

            return playerParticipationRuntime;
        }

        private static IReadOnlyList<PlayerSetEntry> ResolvePlayerSetFromPlan(SessionOperationalRoutePlan plan)
        {
            if (plan.RouteParticipantSetDefinition == null)
            {
                return Array.Empty<PlayerSetEntry>();
            }

            return plan.RouteParticipantSetDefinition.ResolveEntriesOrFail(nameof(OperationalPlayerParticipationStage));
        }

        private static SessionParticipationContext BuildSessionParticipationContext(
            OperationalPlayerParticipationCommand command,
            PlayerParticipationResult result)
        {
            IReadOnlyList<PlayerParticipationSeedEntry> seedEntries = result.Snapshot.SeedEntries ?? Array.Empty<PlayerParticipationSeedEntry>();
            List<PlayerSlotReservation> slotReservations = new(seedEntries.Count);
            List<PlayerSelection> selections = new(seedEntries.Count);
            List<SessionParticipantBinding> participants = new(seedEntries.Count);
            HashSet<PlayerSlotId> observedSlotIds = new();
            HashSet<SessionParticipantId> observedParticipantIds = new();

            for (int i = 0; i < seedEntries.Count; i++)
            {
                var entry = seedEntries[i];
                if (!entry.IsValid)
                {
                    continue;
                }

                var slotId = entry.PlayerSlotId;
                var selectionId = entry.PlayerSelectionId;
                var actorDefinitionId = entry.ActorDefinitionId;
                var actorId = entry.ActorId;
                var participantId = ResolveParticipantId(slotId);
                var role = ResolveParticipantRole(entry, i);

                if (!observedSlotIds.Add(slotId))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] Duplicate PlayerSlotId while building SessionParticipationContext playerSlotId='{slotId}' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}'.");
                }

                if (!observedParticipantIds.Add(participantId))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] Duplicate SessionParticipantId while building SessionParticipationContext participantId='{participantId}' playerSlotId='{slotId}' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}'.");
                }

                slotReservations.Add(new PlayerSlotReservation(
                    slotId,
                    PlayerSlotKind.Local,
                    PlayerSlotReservationSourceKind.RouteParticipantSetDefinition,
                    entry.Required,
                    false,
                    command.Source,
                    command.Reason));

                selections.Add(new PlayerSelection(
                    slotId,
                    selectionId,
                    actorDefinitionId,
                    PlayerSelectionSourceKind.RouteParticipantSetDefinition,
                    command.Source,
                    command.Reason));

                participants.Add(new SessionParticipantBinding(
                    participantId,
                    role,
                    slotId,
                    selectionId,
                    actorDefinitionId,
                    actorId,
                    ActorScope.SessionScoped,
                    ActorMaterializationPolicyKind.ReuseExistingIfAvailable,
                    entry.Required,
                    true,
                    true,
                    command.Source,
                    command.Reason));
            }

            return new SessionParticipationContext(
                command.RouteIdentity,
                command.RouteOperationId,
                RouteParticipationRequirementKind.RequiredDefaultable,
                slotReservations,
                selections,
                participants,
                RuntimePlayerJoinPolicyKind.Unsupported,
                command.Source,
                command.Reason);
        }

        private static SessionParticipantId ResolveParticipantId(PlayerSlotId slotId)
        {
            if (!slotId.IsValid)
            {
                return default;
            }

            return new SessionParticipantId($"participant.{slotId}");
        }

        private static SessionParticipantRole ResolveParticipantRole(PlayerParticipationSeedEntry entry, int index)
        {
            if (index == 0 && entry.Required)
            {
                return SessionParticipantRole.PrimaryPlayer;
            }

            return SessionParticipantRole.SupportingPlayer;
        }

        private static string ResolveRouteParticipantSetDefinitionLabel(SessionOperationalRoutePlan plan)
        {
            return plan.RouteParticipantSetDefinition != null
                ? plan.RouteParticipantSetDefinition.name
                : "<none>";
        }


        private static string FormatSeedActorScopes(IReadOnlyList<PlayerParticipationSeedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            List<string> values = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].ActorScope != ActorScope.Unknown)
                {
                    values.Add(entries[i].ActorScope.ToString());
                }
            }

            return values.Count == 0 ? "<none>" : string.Join(", ", values);
        }

        private static string FormatSeedSlotIds(IReadOnlyList<PlayerParticipationSeedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            List<string> slotIds = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].PlayerSlotId.IsValid)
                {
                    slotIds.Add(entries[i].PlayerSlotId.ToString());
                }
            }

            return slotIds.Count == 0 ? "<none>" : string.Join(", ", slotIds);
        }


        private static string FormatSeedActorDefinitionIds(IReadOnlyList<PlayerParticipationSeedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            List<string> ids = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].ActorDefinitionId.IsValid)
                {
                    ids.Add(entries[i].ActorDefinitionId.ToString());
                }
            }

            return ids.Count == 0 ? "<none>" : string.Join(", ", ids);
        }

        private static string FormatSeedActorIds(IReadOnlyList<PlayerParticipationSeedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            List<string> ids = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].ActorId.IsValid)
                {
                    ids.Add(entries[i].ActorId.ToString());
                }
            }

            return ids.Count == 0 ? "<none>" : string.Join(", ", ids);
        }

        private static string FormatSessionParticipantIds(IReadOnlyList<SessionParticipantBinding> participants)
        {
            if (participants == null || participants.Count == 0)
            {
                return "<none>";
            }

            List<string> participantIds = new(participants.Count);
            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i].IsValid)
                {
                    participantIds.Add(participants[i].ParticipantId.ToString());
                }
            }

            return participantIds.Count == 0 ? "<none>" : string.Join(", ", participantIds);
        }

        private static string FormatPlayerParticipationOutcome(PlayerParticipationResult result)
        {
            if (result.IsObservedNoOp)
            {
                return "observed_noop";
            }

            return result.IsSeedResolved ? "seed_resolved" : "materialized";
        }

        private static string FormatSlotReservations(IReadOnlyList<PlayerSlotReservation> slotReservations)
        {
            if (slotReservations == null || slotReservations.Count == 0)
            {
                return "<none>";
            }

            List<string> values = new(slotReservations.Count);
            for (int i = 0; i < slotReservations.Count; i++)
            {
                var reservation = slotReservations[i];
                values.Add($"slot='{reservation.SlotId}' required='{reservation.Required}' sourceKind='{reservation.SourceKind}'");
            }

            return string.Join(" | ", values);
        }

        private static string FormatSelections(IReadOnlyList<PlayerSelection> selections)
        {
            if (selections == null || selections.Count == 0)
            {
                return "<none>";
            }

            List<string> values = new(selections.Count);
            for (int i = 0; i < selections.Count; i++)
            {
                var selection = selections[i];
                values.Add($"slot='{selection.SlotId}' selection='{selection.SelectionId}' actorDefinitionId='{selection.ActorDefinitionId}' sourceKind='{selection.SourceKind}'");
            }

            return string.Join(" | ", values);
        }

        private static string FormatParticipants(IReadOnlyList<SessionParticipantBinding> participants)
        {
            if (participants == null || participants.Count == 0)
            {
                return "<none>";
            }

            List<string> values = new(participants.Count);
            for (int i = 0; i < participants.Count; i++)
            {
                var participant = participants[i];
                values.Add(
                    $"participantId='{participant.ParticipantId}' role='{participant.Role}' playerSlotId='{participant.PlayerSlotId}' actorDefinitionId='{participant.ActorDefinitionId}' actorId='{participant.ActorId}' scope='{participant.ActorScope}' materializationPolicy='{participant.MaterializationPolicy}' requiresPlayerInput='{participant.RequiresPlayerInput}'");
            }

            return string.Join(" | ", values);
        }

        private static void LogPlayerParticipationStarted(
            OperationalPlayerParticipationCommand command,
            PlayerParticipationSeedIdentity identity)
        {
            DebugUtility.LogVerbose(typeof(OperationalPlayerParticipationStage),
                $"event='PlayerParticipationStarted' pipelineId='{identity.PipelineId}' sessionId='{identity.SessionId}' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' routeSessionParticipation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(command.RouteCommand.Plan)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogPlayerParticipationSeedResolved(
            OperationalPlayerParticipationCommand command,
            PlayerParticipationSeedIdentity identity,
            PlayerParticipationResult result,
            SessionParticipationContext context)
        {
            DebugUtility.LogVerbose(typeof(OperationalPlayerParticipationStage),
                $"event='PlayerParticipationSeedResolved' pipelineId='{identity.PipelineId}' sessionId='{identity.SessionId}' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' routeSessionParticipation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(command.RouteCommand.Plan)}' source='{command.Source}' reason='{command.Reason}' seedOutcome='{FormatPlayerParticipationOutcome(result)}' seedSlotIds='{FormatSeedSlotIds(result.Snapshot.SeedEntries)}' seedActorDefinitionIds='{FormatSeedActorDefinitionIds(result.Snapshot.SeedEntries)}' seedActorIds='{FormatSeedActorIds(result.Snapshot.SeedEntries)}' actorIdSource='PlayerSetDefinitionEntry' derivedActorScopes='{FormatSeedActorScopes(result.Snapshot.SeedEntries)}' sessionParticipantIds='{FormatSessionParticipantIds(context.Participants)}' participantIdPolicy='PlayerSlotIdDerived'.",
                DebugUtility.Colors.Info);
        }


        private static void LogPlayerParticipationRuntimeContextResolved(
            OperationalPlayerParticipationCommand command,
            PlayerParticipationRuntimeContextResult result)
        {
            DebugUtility.LogVerbose(typeof(OperationalPlayerParticipationStage),
                $"event='PlayerParticipationRuntimeContextResolved' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' sessionId='{result.SessionId}' revision='{result.Revision}' resolutionKind='{result.Kind}' sessionParticipationContext='present' sessionSlotReservations='{result.Context.SlotReservationCount}' sessionSelections='{result.Context.SelectionCount}' sessionParticipants='{result.Context.ParticipantCount}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogSessionParticipationContextPrepared(
            OperationalPlayerParticipationCommand command,
            SessionParticipationContext context)
        {
            DebugUtility.Log(typeof(OperationalPlayerParticipationStage),
                $"event='SessionParticipationContextPrepared' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' requirementKind='{context.RequirementKind}' runtimeJoinPolicy='{context.RuntimeJoinPolicy}' slotReservationCount='{context.SlotReservationCount}' selectionCount='{context.SelectionCount}' participantCount='{context.ParticipantCount}' materializationOwner='ActivityEntryPipeline' actorIdSource='PlayerSetDefinitionEntry' inputInstanceRequiredBeforeHandoff='false' source='{command.Source}' reason='{command.Reason}' slots=\"{FormatSlotReservations(context.SlotReservations)}\" selections=\"{FormatSelections(context.Selections)}\" participants=\"{FormatParticipants(context.Participants)}\" participantIdPolicy='PlayerSlotIdDerived'.",
                DebugUtility.Colors.Info);
        }

        private static void LogPlayerParticipationCompleted(
            OperationalPlayerParticipationCommand command,
            PlayerParticipationSeedIdentity identity,
            PlayerParticipationResult result)
        {
            DebugUtility.Log(typeof(OperationalPlayerParticipationStage),
                $"event='PlayerParticipationCompleted' pipelineId='{identity.PipelineId}' sessionId='{identity.SessionId}' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' routeSessionParticipation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(command.RouteCommand.Plan)}' source='{command.Source}' reason='{command.Reason}' outcome='{FormatPlayerParticipationOutcome(result)}'.",
                DebugUtility.Colors.Info);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
