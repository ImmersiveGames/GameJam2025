using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalConsumerPresentationPreparationResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct OperationalConsumerPresentationPreparationResult
    {
        public OperationalConsumerPresentationPreparationResult(
            OperationalConsumerPresentationPreparationResultKind kind,
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

        public OperationalConsumerPresentationPreparationResultKind Kind { get; }
        public string ConsumerIdentity { get; }
        public string RouteOperationId { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalConsumerPresentationPreparationResultKind.Completed;
        public bool IsSkipped => Kind == OperationalConsumerPresentationPreparationResultKind.Skipped;
        public bool IsFailed => Kind == OperationalConsumerPresentationPreparationResultKind.Failed;
        public bool IsAccepted => IsCompleted || IsSkipped;

        public static OperationalConsumerPresentationPreparationResult Completed(
            string consumerIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            return new OperationalConsumerPresentationPreparationResult(
                OperationalConsumerPresentationPreparationResultKind.Completed,
                consumerIdentity,
                routeOperationId,
                reason,
                detail);
        }

        public static OperationalConsumerPresentationPreparationResult Skipped(
            string consumerIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            return new OperationalConsumerPresentationPreparationResult(
                OperationalConsumerPresentationPreparationResultKind.Skipped,
                consumerIdentity,
                routeOperationId,
                reason,
                detail);
        }

        public static OperationalConsumerPresentationPreparationResult Failed(
            string consumerIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            return new OperationalConsumerPresentationPreparationResult(
                OperationalConsumerPresentationPreparationResultKind.Failed,
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

    public readonly struct OperationalConsumerPresentationPreparationCommand
    {
        public OperationalConsumerPresentationPreparationCommand(
            SessionOperationalRouteCommand routeCommand,
            string activeSceneName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string consumerIdentity,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            ActiveSceneName = Normalize(activeSceneName);
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            ConsumerIdentity = Normalize(consumerIdentity);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string ActiveSceneName { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string ConsumerIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
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

    public sealed class OperationalConsumerPresentationPreparationStage
    {
        private readonly Func<IOperationalRouteConsumerPresentationPort> _presentationPortResolver;

        public OperationalConsumerPresentationPreparationStage(Func<IOperationalRouteConsumerPresentationPort> presentationPortResolver)
        {
            _presentationPortResolver = presentationPortResolver ?? throw new ArgumentNullException(nameof(presentationPortResolver));
        }

        public OperationalConsumerPresentationPreparationResult Execute(OperationalConsumerPresentationPreparationCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalConsumerPresentationPreparationCommand is invalid.");
            }

            DebugUtility.Log(typeof(OperationalConsumerPresentationPreparationStage),
                $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationPrepareStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{Normalize(command.ConsumerIdentity)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            OperationalRouteConsumerPresentationRequest request = new(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                string.Empty,
                command.ConsumerIdentity,
                command.RouteCommand.CompletionHandoff,
                command.ActiveSceneName,
                command.RouteCommand.ActivityPresentationProfile,
                command.Source,
                command.Reason);

            IOperationalRouteConsumerPresentationPort presentationPort = ResolvePresentationPortOrFail(command);
            if (!presentationPort.TryPrepare(request, out OperationalRouteConsumerPresentationResult result, out string prepareReason))
            {
                string failureReason = string.IsNullOrWhiteSpace(prepareReason)
                    ? result.Reason
                    : prepareReason;
                LogFailure(command, failureReason, string.Empty);
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][ConsumerPresentation] prepare_failed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{Normalize(command.ConsumerIdentity)}' reason='{failureReason}'.");
            }

            if (result.IsFailed || result.IsRejected)
            {
                string failureReason = string.IsNullOrWhiteSpace(result.Reason)
                    ? "consumer_presentation_prepare_failed"
                    : result.Reason;
                LogFailure(command, failureReason, result.Detail);
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][ConsumerPresentation] prepare_failed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{Normalize(command.ConsumerIdentity)}' reason='{failureReason}'.");
            }

            if (result.IsSkipped)
            {
                DebugUtility.Log(typeof(OperationalConsumerPresentationPreparationStage),
                    $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationPrepareSkipped routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{Normalize(command.ConsumerIdentity)}' skipReason='{result.Reason}' detail='{result.Detail}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return OperationalConsumerPresentationPreparationResult.Skipped(
                    command.ConsumerIdentity,
                    command.RouteOperationId,
                    result.Reason,
                    result.Detail);
            }

            DebugUtility.Log(typeof(OperationalConsumerPresentationPreparationStage),
                $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationPrepareCompleted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{Normalize(command.ConsumerIdentity)}' resultReason='{result.Reason}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return OperationalConsumerPresentationPreparationResult.Completed(
                command.ConsumerIdentity,
                command.RouteOperationId,
                result.Reason,
                result.Detail);
        }

        private IOperationalRouteConsumerPresentationPort ResolvePresentationPortOrFail(OperationalConsumerPresentationPreparationCommand command)
        {
            IOperationalRouteConsumerPresentationPort presentationPort = _presentationPortResolver();
            if (presentationPort == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][ConsumerPresentation] IOperationalRouteConsumerPresentationPort obrigatorio ausente para presentation do route consumer routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{Normalize(command.ConsumerIdentity)}'.");
            }

            return presentationPort;
        }

        private static void LogFailure(
            OperationalConsumerPresentationPreparationCommand command,
            string failureReason,
            string detail)
        {
            string detailSegment = string.IsNullOrWhiteSpace(detail)
                ? string.Empty
                : $" detail='{detail}'";
            DebugUtility.LogError(typeof(OperationalConsumerPresentationPreparationStage),
                $"[OBS][SessionOperationalPipeline][ConsumerPresentation] OperationalRouteConsumerPresentationPrepareFailed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' consumerIdentity='{Normalize(command.ConsumerIdentity)}' reason='{failureReason}'{detailSegment} source='{command.Source}' reasonDetail='{command.Reason}'.");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
