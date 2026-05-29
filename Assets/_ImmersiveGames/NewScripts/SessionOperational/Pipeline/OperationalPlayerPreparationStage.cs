using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalPlayerPreparationResultKind
    {
        Unknown = 0,
        Completed = 1,
        SkippedNoHandoff = 2,
        Failed = 3,
    }

    public readonly struct OperationalPlayerPreparationResult
    {
        public OperationalPlayerPreparationResult(
            OperationalPlayerPreparationResultKind kind,
            PlayerPreparationResult playerPreparationResult)
        {
            Kind = kind;
            PlayerPreparationResult = playerPreparationResult;
        }

        public OperationalPlayerPreparationResultKind Kind { get; }
        public PlayerPreparationResult PlayerPreparationResult { get; }
        public bool IsCompleted => Kind == OperationalPlayerPreparationResultKind.Completed && PlayerPreparationResult.IsValid;
        public bool IsSkipped => Kind == OperationalPlayerPreparationResultKind.SkippedNoHandoff;
        public bool IsAccepted => IsCompleted || IsSkipped;
    }

    public readonly struct OperationalPlayerPreparationCommand
    {
        public OperationalPlayerPreparationCommand(
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

    public sealed class OperationalPlayerPreparationStage
    {
        public OperationalPlayerPreparationResult Execute(OperationalPlayerPreparationCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalPlayerPreparationCommand is invalid.");
            }

            if (command.RouteCommand.CompletionHandoff != SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
            {
                return new OperationalPlayerPreparationResult(
                    OperationalPlayerPreparationResultKind.SkippedNoHandoff,
                    default);
            }

            if (string.IsNullOrWhiteSpace(command.RouteCommand.HandoffSessionStateId))
            {
                throw new InvalidOperationException("handoffSessionStateId is required when completionHandoff=SessionActivityEntry.");
            }

            PlayerPreparationIdentity playerPreparationIdentity = new(
                command.PipelineId,
                command.RouteCommand.HandoffSessionStateId,
                command.RouteIdentity,
                command.RouteOperationId,
                command.RouteSequence,
                command.TransitionId);

            PlayerPreparationPlan playerPreparationPlan = new(
                playerPreparationIdentity,
                true,
                new PlayerSet(ResolvePlayerSetFromPlan(command.RouteCommand.Plan)),
                command.Source,
                command.Reason);

            LogPlayerPreparationStarted(command, playerPreparationIdentity);

            PlayerPreparationResult playerPreparationResult = PlayerPreparationStage.Execute(playerPreparationPlan);
            if (!playerPreparationResult.IsValid)
            {
                throw new InvalidOperationException("PlayerPreparationStage returned an invalid result.");
            }

            LogPlayerPreparationIntentPrepared(command, playerPreparationIdentity, playerPreparationResult);
            LogPlayerPreparationCompleted(command, playerPreparationIdentity, playerPreparationResult);

            return new OperationalPlayerPreparationResult(
                OperationalPlayerPreparationResultKind.Completed,
                playerPreparationResult);
        }

        private static IReadOnlyList<PlayerSetEntry> ResolvePlayerSetFromPlan(SessionOperationalRoutePlan plan)
        {
            if (plan.RouteParticipantSetDefinition == null)
            {
                return Array.Empty<PlayerSetEntry>();
            }

            return plan.RouteParticipantSetDefinition.ResolveEntriesOrFail(nameof(OperationalPlayerPreparationStage));
        }

        private static string ResolveRouteParticipantSetDefinitionLabel(SessionOperationalRoutePlan plan)
        {
            return plan.RouteParticipantSetDefinition != null
                ? plan.RouteParticipantSetDefinition.name
                : "<none>";
        }

        private static string FormatPlayerIdsForHandoff(IReadOnlyList<PlayerPlannedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            List<string> playerIds = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(entries[i].PlayerId))
                {
                    playerIds.Add(entries[i].PlayerId);
                }
            }

            return playerIds.Count == 0 ? "<none>" : string.Join(", ", playerIds);
        }

        private static string FormatPlayerPreparationOutcome(PlayerPreparationResult result)
        {
            if (result.IsObservedNoOp)
            {
                return "observed_noop";
            }

            return result.IsPlannedOnly ? "planned_only" : "materialized";
        }

        private static void LogPlayerPreparationStarted(
            OperationalPlayerPreparationCommand command,
            PlayerPreparationIdentity identity)
        {
            DebugUtility.Log(typeof(OperationalPlayerPreparationStage),
                $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerPreparationStarted' pipelineId='{identity.PipelineId}' sessionId='{identity.SessionId}' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' routeSessionParticipantPreparation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(command.RouteCommand.Plan)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogPlayerPreparationIntentPrepared(
            OperationalPlayerPreparationCommand command,
            PlayerPreparationIdentity identity,
            PlayerPreparationResult result)
        {
            DebugUtility.Log(typeof(OperationalPlayerPreparationStage),
                $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerPreparationIntentPrepared' pipelineId='{identity.PipelineId}' sessionId='{identity.SessionId}' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' routeSessionParticipantPreparation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(command.RouteCommand.Plan)}' source='{command.Source}' reason='{command.Reason}' playerIds='{FormatPlayerIdsForHandoff(result.Snapshot.PlannedEntries)}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogPlayerPreparationCompleted(
            OperationalPlayerPreparationCommand command,
            PlayerPreparationIdentity identity,
            PlayerPreparationResult result)
        {
            DebugUtility.Log(typeof(OperationalPlayerPreparationStage),
                $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerPreparationCompleted' pipelineId='{identity.PipelineId}' sessionId='{identity.SessionId}' routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' routeSessionParticipantPreparation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(command.RouteCommand.Plan)}' source='{command.Source}' reason='{command.Reason}' outcome='{FormatPlayerPreparationOutcome(result)}'.",
                DebugUtility.Colors.Info);
        }
    }
}
