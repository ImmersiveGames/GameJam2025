using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalPreviousRouteExitResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalPreviousRouteExitCommand
    {
        public OperationalPreviousRouteExitCommand(
            SessionOperationalRouteCommand routeCommand,
            string previousRouteIdentity,
            string previousActivityIdentity,
            RouteActivitySavePlan routeActivitySavePlan,
            string source,
            string reason,
            OperationalHandoffExitStage handoffExitStage,
            OperationalHandoffExitCommand handoffExitCommand,
            OperationalConsumerPresentationReleaseStage consumerPresentationReleaseStage,
            OperationalConsumerPresentationReleaseCommand consumerPresentationReleaseCommand,
            OperationalRouteActivitySaveSaveOnExitStage routeActivitySaveSaveOnExitStage,
            OperationalRouteActivitySaveSaveOnExitCommand routeActivitySaveSaveOnExitCommand)
        {
            RouteCommand = routeCommand;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            PreviousActivityIdentity = Normalize(previousActivityIdentity);
            RouteActivitySavePlan = routeActivitySavePlan;
            Source = Normalize(source);
            Reason = Normalize(reason);
            HandoffExitStage = handoffExitStage;
            HandoffExitCommand = handoffExitCommand;
            ConsumerPresentationReleaseStage = consumerPresentationReleaseStage;
            ConsumerPresentationReleaseCommand = consumerPresentationReleaseCommand;
            RouteActivitySaveSaveOnExitStage = routeActivitySaveSaveOnExitStage;
            RouteActivitySaveSaveOnExitCommand = routeActivitySaveSaveOnExitCommand;
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string PreviousRouteIdentity { get; }
        public string PreviousActivityIdentity { get; }
        public RouteActivitySavePlan RouteActivitySavePlan { get; }
        public string Source { get; }
        public string Reason { get; }
        public OperationalHandoffExitStage HandoffExitStage { get; }
        public OperationalHandoffExitCommand HandoffExitCommand { get; }
        public OperationalConsumerPresentationReleaseStage ConsumerPresentationReleaseStage { get; }
        public OperationalConsumerPresentationReleaseCommand ConsumerPresentationReleaseCommand { get; }
        public OperationalRouteActivitySaveSaveOnExitStage RouteActivitySaveSaveOnExitStage { get; }
        public OperationalRouteActivitySaveSaveOnExitCommand RouteActivitySaveSaveOnExitCommand { get; }

        public bool IsValid => RouteCommand.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalPreviousRouteExitResult
    {
        public OperationalPreviousRouteExitResult(
            OperationalPreviousRouteExitResultKind kind,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string reason,
            string detail)
        {
            Kind = kind;
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalPreviousRouteExitResultKind Kind { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalPreviousRouteExitResultKind.Completed;
        public bool IsFailed => Kind == OperationalPreviousRouteExitResultKind.Failed;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalPreviousRouteExitStage
    {
        public async Task<OperationalPreviousRouteExitResult> ExecuteAsync(OperationalPreviousRouteExitCommand command)
        {
            if (!command.IsValid)
            {
                return Failed(command, "invalid_command", "OperationalPreviousRouteExitCommand invalido.");
            }

            if (command.HandoffExitStage == null || !command.HandoffExitCommand.IsValid)
            {
                return Failed(command, "missing_handoff_exit_step", "Handoff-exit stage obrigatorio ausente/invalido.");
            }

            if (command.ConsumerPresentationReleaseStage == null || !command.ConsumerPresentationReleaseCommand.IsValid)
            {
                return Failed(command, "missing_consumer_presentation_release_step", "Consumer presentation release stage obrigatorio ausente/invalido.");
            }

            if (command.RouteActivitySaveSaveOnExitStage == null || !command.RouteActivitySaveSaveOnExitCommand.IsValid)
            {
                return Failed(command, "missing_route_activity_save_step", "RouteActivitySave save-on-exit stage obrigatorio ausente/invalido.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            DebugUtility.Log(typeof(OperationalPreviousRouteExitStage),
                $"[OBS][SessionOperationalPipeline][PreviousRouteExit] OperationalPreviousRouteExitStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            OperationalHandoffExitResult handoffExitResult = await command.HandoffExitStage.ExecuteAsync(command.HandoffExitCommand);
            if (!handoffExitResult.IsAccepted)
            {
                return Failed(command, handoffExitResult.Reason, handoffExitResult.Detail);
            }

            OperationalConsumerPresentationReleaseResult consumerPresentationReleaseResult =
                command.ConsumerPresentationReleaseStage.Execute(command.ConsumerPresentationReleaseCommand);
            if (!consumerPresentationReleaseResult.IsAccepted)
            {
                return Failed(command, consumerPresentationReleaseResult.Reason, consumerPresentationReleaseResult.Detail);
            }

            OperationalRouteActivitySaveSaveOnExitResult saveOnExitResult =
                command.RouteActivitySaveSaveOnExitStage.Execute(command.RouteActivitySaveSaveOnExitCommand);
            if (!saveOnExitResult.IsCompleted)
            {
                return Failed(command, saveOnExitResult.Reason, saveOnExitResult.Detail);
            }

            DebugUtility.Log(typeof(OperationalPreviousRouteExitStage),
                $"[OBS][SessionOperationalPipeline][PreviousRouteExit] OperationalPreviousRouteExitCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' previousActivityIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new OperationalPreviousRouteExitResult(
                OperationalPreviousRouteExitResultKind.Completed,
                routeCommand.RouteIdentity,
                routeCommand.RouteOperationId,
                routeCommand.TransitionId,
                routeCommand.RouteSequence,
                "completed",
                "previous_route_exit_completed");
        }

        private static OperationalPreviousRouteExitResult Failed(
            OperationalPreviousRouteExitCommand command,
            string reason,
            string detail)
        {
            return new OperationalPreviousRouteExitResult(
                OperationalPreviousRouteExitResultKind.Failed,
                command.RouteCommand.RouteIdentity,
                command.RouteCommand.RouteOperationId,
                command.RouteCommand.TransitionId,
                command.RouteCommand.RouteSequence,
                reason,
                detail);
        }
    }
}
