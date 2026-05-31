using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalConsumerEntryAndReadinessResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct OperationalConsumerEntryAndReadinessResult
    {
        public OperationalConsumerEntryAndReadinessResult(
            OperationalConsumerEntryAndReadinessResultKind kind,
            string consumerIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            Kind = kind;
            ConsumerIdentity = Normalize(consumerIdentity);
            RouteOperationId = Normalize(routeOperationId);
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalConsumerEntryAndReadinessResultKind Kind { get; }
        public string ConsumerIdentity { get; }
        public string RouteOperationId { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalConsumerEntryAndReadinessResultKind.Completed;
        public bool IsSkipped => Kind == OperationalConsumerEntryAndReadinessResultKind.Skipped;
        public bool IsFailed => Kind == OperationalConsumerEntryAndReadinessResultKind.Failed;
        public bool IsAccepted => IsCompleted || IsSkipped;

        public static OperationalConsumerEntryAndReadinessResult Completed(
            string consumerIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            return new OperationalConsumerEntryAndReadinessResult(
                OperationalConsumerEntryAndReadinessResultKind.Completed,
                consumerIdentity,
                routeOperationId,
                reason,
                detail);
        }

        public static OperationalConsumerEntryAndReadinessResult Skipped(
            string consumerIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            return new OperationalConsumerEntryAndReadinessResult(
                OperationalConsumerEntryAndReadinessResultKind.Skipped,
                consumerIdentity,
                routeOperationId,
                reason,
                detail);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalConsumerEntryAndReadinessCommand
    {
        public OperationalConsumerEntryAndReadinessCommand(
            SessionOperationalRouteCommand routeCommand,
            SessionOperationalLoadingCommand loadingCommand,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            LoadingCommand = loadingCommand;
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public SessionOperationalLoadingCommand LoadingCommand { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            RouteCommand.IsValid &&
            LoadingCommand.IsValid &&
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

    public sealed class OperationalConsumerEntryAndReadinessStage
    {
        private readonly Func<IOperationalRouteConsumerEntryPort> _entryPortResolver;
        private readonly Func<IOperationalRouteConsumerReadinessPort> _readinessPortResolver;

        public OperationalConsumerEntryAndReadinessStage(
            Func<IOperationalRouteConsumerEntryPort> entryPortResolver,
            Func<IOperationalRouteConsumerReadinessPort> readinessPortResolver)
        {
            _entryPortResolver = entryPortResolver ?? throw new ArgumentNullException(nameof(entryPortResolver));
            _readinessPortResolver = readinessPortResolver ?? throw new ArgumentNullException(nameof(readinessPortResolver));
        }

        public async Task<OperationalConsumerEntryAndReadinessResult> ExecuteAsync(
            OperationalConsumerEntryAndReadinessCommand command,
            OperationalPlayerParticipationResult playerParticipationStageResult)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalConsumerEntryAndReadinessCommand is invalid.");
            }

            if (command.RouteCommand.CompletionHandoff != SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
            {
                return OperationalConsumerEntryAndReadinessResult.Skipped(
                    string.Empty,
                    command.RouteOperationId,
                    "no_consumer_entry_handoff",
                    "completion_handoff_not_session_activity_entry");
            }

            if (string.IsNullOrWhiteSpace(command.RouteCommand.HandoffSessionStateId))
            {
                throw new InvalidOperationException("handoffSessionStateId is required when completionHandoff=SessionActivityEntry.");
            }

            if (!playerParticipationStageResult.IsCompleted)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] Missing valid OperationalPlayerParticipationResult for operational consumer entry routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}'.");
            }

            PlayerParticipationResult playerParticipationResult = playerParticipationStageResult.PlayerParticipationResult;
            SessionParticipationContext sessionParticipationContext = playerParticipationStageResult.SessionParticipationContext;
            if (sessionParticipationContext == null || !sessionParticipationContext.IsValid)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][PlayerParticipation] Missing valid SessionParticipationContext for operational consumer entry routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}'.");
            }

            LogEntryStarted(command, playerParticipationResult, sessionParticipationContext);

            OperationalRouteConsumerEntryRequest consumerEntryRequest = new(
                command.RouteCommand.HandoffSessionStateId,
                sessionParticipationContext,
                ResolvePlayerTechnicalEntriesFromPlan(command.RouteCommand.Plan),
                command.RouteCommand.UsesTransition,
                command.RouteCommand.TransitionProfile,
                command.LoadingCommand.LoadingMode == SessionOperationalRouteLoadingMode.Profile,
                command.LoadingCommand.LoadingProfile,
                command.Source,
                command.Reason);

            IOperationalRouteConsumerEntryPort entryPort = ResolveEntryPortOrFail(command);
            OperationalRouteConsumerEntryResult consumerEntryResult = await entryPort
                .RequestEntryAsync(consumerEntryRequest, CancellationToken.None);
            if (!consumerEntryResult.IsValid || !consumerEntryResult.IsCompleted)
            {
                throw new InvalidOperationException($"Operational route consumer entry failed/rejected. kind='{consumerEntryResult.Kind}' reason='{consumerEntryResult.Reason}' detail='{consumerEntryResult.Detail}'.");
            }

            DebugUtility.Log(typeof(OperationalConsumerEntryAndReadinessStage),
                $"[OBS][SessionOperationalPipeline][Route] handoff='OperationalRouteConsumerEntryCompleted' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' source='{command.Source}' reason='{command.Reason}' resultKind='{consumerEntryResult.Kind}' resultReason='{consumerEntryResult.Reason}'.",
                DebugUtility.Colors.Success);

            await AwaitReadinessOrFailAsync(command, playerParticipationResult.Snapshot.Identity.RouteOperationId);

            return OperationalConsumerEntryAndReadinessResult.Completed(
                command.RouteCommand.HandoffSessionStateId,
                command.RouteOperationId,
                "completed",
                "consumer_entry_and_readiness_completed");
        }

        private async Task AwaitReadinessOrFailAsync(
            OperationalConsumerEntryAndReadinessCommand command,
            string expectedRouteOperationId)
        {
            string normalizedConsumerIdentity = Normalize(command.RouteCommand.HandoffSessionStateId);
            string normalizedExpectedRouteOperationId = Normalize(expectedRouteOperationId);
            if (string.IsNullOrWhiteSpace(normalizedConsumerIdentity))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] consumerIdentity ausente para readiness visual routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}'.");
            }

            if (string.IsNullOrWhiteSpace(normalizedExpectedRouteOperationId))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] expectedRouteOperationId ausente para readiness visual routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}'.");
            }

            OperationalRouteConsumerReadinessRequest request = new(
                normalizedConsumerIdentity,
                normalizedExpectedRouteOperationId,
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.Source,
                command.Reason);

            DebugUtility.Log(typeof(OperationalConsumerEntryAndReadinessStage),
                $"[OBS][SessionOperationalPipeline][ConsumerReadiness] OperationalRouteConsumerReadinessAwaitStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{normalizedConsumerIdentity}' expectedRouteOperationId='{normalizedExpectedRouteOperationId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            IOperationalRouteConsumerReadinessPort readinessPort = ResolveReadinessPortOrFail(command);
            OperationalRouteConsumerReadinessResult readinessResult = await readinessPort.AwaitReadinessAsync(
                request,
                CancellationToken.None);

            if (!readinessResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] readiness_result_invalid routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' readinessResult='{readinessResult}'.");
            }

            if (readinessResult.IsReady)
            {
                DebugUtility.Log(typeof(OperationalConsumerEntryAndReadinessStage),
                    $"[OBS][SessionOperationalPipeline][ConsumerReadiness] OperationalRouteConsumerReadinessCompleted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{normalizedConsumerIdentity}' expectedRouteOperationId='{normalizedExpectedRouteOperationId}' readinessResult='{readinessResult}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            if (readinessResult.IsNotRequired)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] readiness_not_required_invalid_for_handoff routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{normalizedConsumerIdentity}' expectedRouteOperationId='{normalizedExpectedRouteOperationId}' readinessResult='{readinessResult}'.");
            }

            if (readinessResult.IsRejectedForeignOrStale || readinessResult.IsFailed)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] readiness_rejected_or_failed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{normalizedConsumerIdentity}' expectedRouteOperationId='{normalizedExpectedRouteOperationId}' readinessResult='{readinessResult}'.");
            }

            throw new InvalidOperationException(
                $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] readiness_unhandled_kind routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{normalizedConsumerIdentity}' expectedRouteOperationId='{normalizedExpectedRouteOperationId}' readinessResult='{readinessResult}'.");
        }

        private IOperationalRouteConsumerEntryPort ResolveEntryPortOrFail(OperationalConsumerEntryAndReadinessCommand command)
        {
            IOperationalRouteConsumerEntryPort entryPort = _entryPortResolver();
            if (entryPort == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][ConsumerEntry] IOperationalRouteConsumerEntryPort obrigatorio ausente para o trilho operacional routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{Normalize(command.RouteCommand.HandoffSessionStateId)}'.");
            }

            return entryPort;
        }

        private IOperationalRouteConsumerReadinessPort ResolveReadinessPortOrFail(OperationalConsumerEntryAndReadinessCommand command)
        {
            IOperationalRouteConsumerReadinessPort readinessPort = _readinessPortResolver();
            if (readinessPort == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] IOperationalRouteConsumerReadinessPort obrigatorio ausente para readiness visual do route consumer routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{Normalize(command.RouteCommand.HandoffSessionStateId)}'.");
            }

            return readinessPort;
        }

        private static void LogEntryStarted(
            OperationalConsumerEntryAndReadinessCommand command,
            PlayerParticipationResult playerParticipationResult,
            SessionParticipationContext sessionParticipationContext)
        {
            DebugUtility.Log(typeof(OperationalConsumerEntryAndReadinessStage),
                $"[OBS][SessionOperationalPipeline][Route] handoff='OperationalRouteConsumerEntryStarted' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' source='{command.Source}' reason='{command.Reason}' pendingHandoff='SessionActivityEntry' routeSessionParticipation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(command.RouteCommand.Plan)}' sessionParticipationContext='present' sessionParticipationRevision='{sessionParticipationContext.Revision}' sessionSlotReservations='{sessionParticipationContext.SlotReservationCount}' sessionSelections='{sessionParticipationContext.SelectionCount}' sessionParticipants='{sessionParticipationContext.ParticipantCount}' playerParticipationSeedOutcome='{FormatPlayerParticipationSeedOutcome(playerParticipationResult.Snapshot.Outcome)}'.",
                DebugUtility.Colors.Info);
        }

        private static string FormatPlayerParticipationSeedOutcome(PlayerParticipationOutcome outcome)
        {
            return outcome switch
            {
                PlayerParticipationOutcome.ObservedNoOp => "ObservedNoOp",
                PlayerParticipationOutcome.SeedResolved => "SeedResolved",
                PlayerParticipationOutcome.Materialized => "Materialized",
                _ => "Unknown",
            };
        }

        private static IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> ResolvePlayerTechnicalEntriesFromPlan(SessionOperationalRoutePlan plan)
        {
            if (plan.RouteParticipantSetDefinition == null)
            {
                return Array.Empty<PlayerSetDefinitionAsset.PlayerActorResolvedEntry>();
            }

            IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> entries =
                plan.RouteParticipantSetDefinition.ResolvePlayerActorEntriesOrFail(nameof(OperationalConsumerEntryAndReadinessStage));
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<PlayerSetDefinitionAsset.PlayerActorResolvedEntry>();
            }

            return entries;
        }

        private static string ResolveRouteParticipantSetDefinitionLabel(SessionOperationalRoutePlan plan)
        {
            if (plan.RouteParticipantSetDefinition == null)
            {
                return "<none>";
            }

            return string.IsNullOrWhiteSpace(plan.RouteParticipantSetDefinition.name)
                ? "<unnamed>"
                : plan.RouteParticipantSetDefinition.name.Trim();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
