using _ImmersiveGames.NewScripts.CameraPresentation.Authoring;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteConsumerPresentationResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3,
        RejectedForeignOrStale = 4,
    }

    public readonly struct OperationalRouteConsumerPresentationRequest
    {
        public OperationalRouteConsumerPresentationRequest(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string previousRouteIdentity,
            string consumerIdentity,
            SessionOperationalRouteCompletionHandoffKind completionHandoff,
            string activeSceneName,
            ActivityPresentationProfileAsset consumerPresentationProfile,
            string source,
            string reason)
        {
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            ConsumerIdentity = Normalize(consumerIdentity);
            CompletionHandoff = completionHandoff;
            ActiveSceneName = Normalize(activeSceneName);
            ConsumerPresentationProfile = consumerPresentationProfile;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string PreviousRouteIdentity { get; }
        public string ConsumerIdentity { get; }
        public SessionOperationalRouteCompletionHandoffKind CompletionHandoff { get; }
        public string ActiveSceneName { get; }
        public ActivityPresentationProfileAsset ConsumerPresentationProfile { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
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

    public readonly struct OperationalRouteConsumerPresentationResult
    {
        public OperationalRouteConsumerPresentationResult(
            OperationalRouteConsumerPresentationResultKind kind,
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

        public OperationalRouteConsumerPresentationResultKind Kind { get; }
        public string ConsumerIdentity { get; }
        public string RouteOperationId { get; }
        public string Reason { get; }
        public string Detail { get; }

        public bool IsCompleted => Kind == OperationalRouteConsumerPresentationResultKind.Completed;
        public bool IsSkipped => Kind == OperationalRouteConsumerPresentationResultKind.Skipped;
        public bool IsFailed => Kind == OperationalRouteConsumerPresentationResultKind.Failed;
        public bool IsRejected => Kind == OperationalRouteConsumerPresentationResultKind.RejectedForeignOrStale;

        public static OperationalRouteConsumerPresentationResult Completed(
            string consumerIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            return new OperationalRouteConsumerPresentationResult(
                OperationalRouteConsumerPresentationResultKind.Completed,
                consumerIdentity,
                routeOperationId,
                reason,
                detail);
        }

        public static OperationalRouteConsumerPresentationResult Skipped(
            string consumerIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            return new OperationalRouteConsumerPresentationResult(
                OperationalRouteConsumerPresentationResultKind.Skipped,
                consumerIdentity,
                routeOperationId,
                reason,
                detail);
        }

        public static OperationalRouteConsumerPresentationResult Failed(
            string consumerIdentity,
            string routeOperationId,
            string reason,
            string detail)
        {
            return new OperationalRouteConsumerPresentationResult(
                OperationalRouteConsumerPresentationResultKind.Failed,
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

    public interface IOperationalRouteConsumerPresentationPort
    {
        bool TryReleasePrevious(
            OperationalRouteConsumerPresentationRequest request,
            out OperationalRouteConsumerPresentationResult result,
            out string reason);

        bool TryPrepare(
            OperationalRouteConsumerPresentationRequest request,
            out OperationalRouteConsumerPresentationResult result,
            out string reason);
    }
}
