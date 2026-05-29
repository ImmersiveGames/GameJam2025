using _ImmersiveGames.NewScripts.CameraPresentation.Authoring;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public enum SessionOperationalRouteCameraPrepareOutcomeKind
    {
        Prepared = 0,
        Skipped = 1,
        Failed = 2,
    }

    public readonly struct SessionOperationalRouteCameraPrepareCommand
    {
        public SessionOperationalRouteCameraPrepareCommand(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string surfaceKind,
            SessionOperationalRouteCompletionHandoffKind completionHandoff,
            string activeSceneName,
            SurfacePresentationProfileAsset surfacePresentationProfile,
            ActivityPresentationProfileAsset activityPresentationProfile,
            string source,
            string reason)
        {
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            SurfaceKind = Normalize(surfaceKind);
            CompletionHandoff = completionHandoff;
            ActiveSceneName = Normalize(activeSceneName);
            SurfacePresentationProfile = surfacePresentationProfile;
            ActivityPresentationProfile = activityPresentationProfile;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string SurfaceKind { get; }
        public SessionOperationalRouteCompletionHandoffKind CompletionHandoff { get; }
        public string ActiveSceneName { get; }
        public SurfacePresentationProfileAsset SurfacePresentationProfile { get; }
        public ActivityPresentationProfileAsset ActivityPresentationProfile { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(SurfaceKind) &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionOperationalRouteCameraPrepareResult
    {
        public SessionOperationalRouteCameraPrepareResult(
            SessionOperationalRouteCameraPrepareOutcomeKind outcomeKind,
            RouteCameraReadyFact readyFact,
            RouteCameraFailureFact failureFact,
            string reason,
            string skipReason)
        {
            OutcomeKind = outcomeKind;
            ReadyFact = readyFact;
            FailureFact = failureFact;
            Reason = Normalize(reason);
            SkipReason = Normalize(skipReason);
        }

        public SessionOperationalRouteCameraPrepareOutcomeKind OutcomeKind { get; }
        public RouteCameraReadyFact ReadyFact { get; }
        public RouteCameraFailureFact FailureFact { get; }
        public string Reason { get; }
        public string SkipReason { get; }

        public bool IsPrepared => OutcomeKind == SessionOperationalRouteCameraPrepareOutcomeKind.Prepared && ReadyFact != null;
        public bool IsSkipped => OutcomeKind == SessionOperationalRouteCameraPrepareOutcomeKind.Skipped;
        public bool IsFailed => OutcomeKind == SessionOperationalRouteCameraPrepareOutcomeKind.Failed;

        public static SessionOperationalRouteCameraPrepareResult Prepared(RouteCameraReadyFact readyFact, string reason)
        {
            return new SessionOperationalRouteCameraPrepareResult(
                SessionOperationalRouteCameraPrepareOutcomeKind.Prepared,
                readyFact,
                null,
                reason,
                string.Empty);
        }

        public static SessionOperationalRouteCameraPrepareResult Skipped(string skipReason)
        {
            return new SessionOperationalRouteCameraPrepareResult(
                SessionOperationalRouteCameraPrepareOutcomeKind.Skipped,
                null,
                null,
                skipReason,
                skipReason);
        }

        public static SessionOperationalRouteCameraPrepareResult Failed(RouteCameraFailureFact failureFact, string reason)
        {
            return new SessionOperationalRouteCameraPrepareResult(
                SessionOperationalRouteCameraPrepareOutcomeKind.Failed,
                null,
                failureFact,
                reason,
                string.Empty);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum SessionOperationalRouteCameraReleaseOutcomeKind
    {
        Released = 0,
        Skipped = 1,
        Failed = 2,
    }

    public readonly struct SessionOperationalRouteCameraReleaseCommand
    {
        public SessionOperationalRouteCameraReleaseCommand(
            string currentRouteIdentity,
            string previousRouteIdentity,
            string source,
            string reason)
        {
            CurrentRouteIdentity = Normalize(currentRouteIdentity);
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string CurrentRouteIdentity { get; }
        public string PreviousRouteIdentity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionOperationalRouteCameraReleaseResult
    {
        public SessionOperationalRouteCameraReleaseResult(
            SessionOperationalRouteCameraReleaseOutcomeKind outcomeKind,
            RouteCameraReleasedFact releasedFact,
            RouteCameraReleaseFailureFact failureFact,
            string reason,
            string skipReason)
        {
            OutcomeKind = outcomeKind;
            ReleasedFact = releasedFact;
            FailureFact = failureFact;
            Reason = Normalize(reason);
            SkipReason = Normalize(skipReason);
        }

        public SessionOperationalRouteCameraReleaseOutcomeKind OutcomeKind { get; }
        public RouteCameraReleasedFact ReleasedFact { get; }
        public RouteCameraReleaseFailureFact FailureFact { get; }
        public string Reason { get; }
        public string SkipReason { get; }

        public bool IsReleased => OutcomeKind == SessionOperationalRouteCameraReleaseOutcomeKind.Released && ReleasedFact != null;
        public bool IsSkipped => OutcomeKind == SessionOperationalRouteCameraReleaseOutcomeKind.Skipped;
        public bool IsFailed => OutcomeKind == SessionOperationalRouteCameraReleaseOutcomeKind.Failed;

        public static SessionOperationalRouteCameraReleaseResult Released(RouteCameraReleasedFact releasedFact, string reason)
        {
            return new SessionOperationalRouteCameraReleaseResult(
                SessionOperationalRouteCameraReleaseOutcomeKind.Released,
                releasedFact,
                null,
                reason,
                string.Empty);
        }

        public static SessionOperationalRouteCameraReleaseResult Skipped(string skipReason)
        {
            return new SessionOperationalRouteCameraReleaseResult(
                SessionOperationalRouteCameraReleaseOutcomeKind.Skipped,
                null,
                null,
                skipReason,
                skipReason);
        }

        public static SessionOperationalRouteCameraReleaseResult Failed(RouteCameraReleaseFailureFact failureFact, string reason)
        {
            return new SessionOperationalRouteCameraReleaseResult(
                SessionOperationalRouteCameraReleaseOutcomeKind.Failed,
                null,
                failureFact,
                reason,
                string.Empty);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface ISessionOperationalRouteCameraAdapter
    {
        bool TryPrepareRouteCamera(
            SessionOperationalRouteCameraPrepareCommand command,
            out SessionOperationalRouteCameraPrepareResult result,
            out string reason);

        bool TryReleaseRouteCamera(
            SessionOperationalRouteCameraReleaseCommand command,
            out SessionOperationalRouteCameraReleaseResult result,
            out string reason);
    }
}
