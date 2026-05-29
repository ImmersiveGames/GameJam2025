using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalConsumerPresentationReleaseResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct OperationalConsumerPresentationReleaseCommand
    {
        public OperationalConsumerPresentationReleaseCommand(
            IOperationalRouteConsumerPresentationPort presentationPort,
            SessionOperationalRouteCommand routeCommand,
            string activeSceneName,
            string previousRouteIdentity,
            string previousActivityIdentity,
            string source,
            string reason)
        {
            PresentationPort = presentationPort;
            RouteCommand = routeCommand;
            ActiveSceneName = Normalize(activeSceneName);
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            PreviousActivityIdentity = Normalize(previousActivityIdentity);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public IOperationalRouteConsumerPresentationPort PresentationPort { get; }
        public SessionOperationalRouteCommand RouteCommand { get; }
        public string ActiveSceneName { get; }
        public string PreviousRouteIdentity { get; }
        public string PreviousActivityIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            PresentationPort != null &&
            RouteCommand.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalConsumerPresentationReleaseResult
    {
        public OperationalConsumerPresentationReleaseResult(
            OperationalConsumerPresentationReleaseResultKind kind,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string previousRouteIdentity,
            string consumerIdentity,
            string reason,
            string detail)
        {
            Kind = kind;
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            ConsumerIdentity = Normalize(consumerIdentity);
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalConsumerPresentationReleaseResultKind Kind { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string PreviousRouteIdentity { get; }
        public string ConsumerIdentity { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalConsumerPresentationReleaseResultKind.Completed;
        public bool IsSkipped => Kind == OperationalConsumerPresentationReleaseResultKind.Skipped;
        public bool IsFailed => Kind == OperationalConsumerPresentationReleaseResultKind.Failed;
        public bool IsAccepted => IsCompleted || IsSkipped;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalConsumerPresentationReleaseStage
    {
        public OperationalConsumerPresentationReleaseResult Execute(OperationalConsumerPresentationReleaseCommand command)
        {
            if (!command.IsValid)
            {
                return Failed(command, "invalid_command", "OperationalConsumerPresentationReleaseCommand invalido.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            DebugUtility.Log(typeof(OperationalConsumerPresentationReleaseStage),
                $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationReleaseStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' consumerIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            OperationalRouteConsumerPresentationRequest request = new(
                routeCommand.RouteIdentity,
                routeCommand.RouteOperationId,
                routeCommand.TransitionId,
                routeCommand.RouteSequence,
                command.PreviousRouteIdentity,
                command.PreviousActivityIdentity,
                routeCommand.CompletionHandoff,
                command.ActiveSceneName,
                routeCommand.ActivityPresentationProfile,
                command.Source,
                command.Reason);

            if (!command.PresentationPort.TryReleasePrevious(
                    request,
                    out OperationalRouteConsumerPresentationResult result,
                    out string releaseReason))
            {
                string failureReason = string.IsNullOrWhiteSpace(releaseReason)
                    ? result.Reason
                    : releaseReason;

                DebugUtility.LogError(typeof(OperationalConsumerPresentationReleaseStage),
                    $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationReleaseFailed routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' consumerIdentity='{command.PreviousActivityIdentity}' reason='{failureReason}' source='{command.Source}' reasonDetail='{command.Reason}'.");

                return Failed(command, failureReason, "consumer_presentation_release_try_release_previous_failed");
            }

            if (result.IsFailed || result.IsRejected)
            {
                string failureReason = string.IsNullOrWhiteSpace(result.Reason)
                    ? "consumer_presentation_release_failed"
                    : result.Reason;

                DebugUtility.LogError(typeof(OperationalConsumerPresentationReleaseStage),
                    $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationReleaseFailed routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' consumerIdentity='{command.PreviousActivityIdentity}' reason='{failureReason}' detail='{result.Detail}' source='{command.Source}' reasonDetail='{command.Reason}'.");

                return Failed(command, failureReason, result.Detail);
            }

            if (result.IsSkipped)
            {
                DebugUtility.Log(typeof(OperationalConsumerPresentationReleaseStage),
                    $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationReleaseSkipped routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' consumerIdentity='{command.PreviousActivityIdentity}' skipReason='{result.Reason}' detail='{result.Detail}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

                return new OperationalConsumerPresentationReleaseResult(
                    OperationalConsumerPresentationReleaseResultKind.Skipped,
                    routeCommand.RouteIdentity,
                    routeCommand.RouteOperationId,
                    routeCommand.TransitionId,
                    routeCommand.RouteSequence,
                    command.PreviousRouteIdentity,
                    command.PreviousActivityIdentity,
                    result.Reason,
                    result.Detail);
            }

            DebugUtility.Log(typeof(OperationalConsumerPresentationReleaseStage),
                $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationReleaseCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' consumerIdentity='{command.PreviousActivityIdentity}' resultReason='{result.Reason}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new OperationalConsumerPresentationReleaseResult(
                OperationalConsumerPresentationReleaseResultKind.Completed,
                routeCommand.RouteIdentity,
                routeCommand.RouteOperationId,
                routeCommand.TransitionId,
                routeCommand.RouteSequence,
                command.PreviousRouteIdentity,
                command.PreviousActivityIdentity,
                result.Reason,
                result.Detail);
        }

        private static OperationalConsumerPresentationReleaseResult Failed(
            OperationalConsumerPresentationReleaseCommand command,
            string reason,
            string detail)
        {
            SessionOperationalRouteCommand routeCommand = command.RouteCommand;
            return new OperationalConsumerPresentationReleaseResult(
                OperationalConsumerPresentationReleaseResultKind.Failed,
                routeCommand.RouteIdentity,
                routeCommand.RouteOperationId,
                routeCommand.TransitionId,
                routeCommand.RouteSequence,
                command.PreviousRouteIdentity,
                command.PreviousActivityIdentity,
                string.IsNullOrWhiteSpace(reason) ? "consumer_presentation_release_failed" : reason,
                string.IsNullOrWhiteSpace(detail) ? "consumer_presentation_release_failed" : detail);
        }
    }
}
