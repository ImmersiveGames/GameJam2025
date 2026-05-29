using _ImmersiveGames.NewScripts.CameraPresentation.Authoring;
using _ImmersiveGames.NewScripts.CameraPresentation.Models;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public enum SessionOperationalActivityCameraPrepareOutcomeKind
    {
        Prepared = 0,
        Skipped = 1,
        Failed = 2,
    }

    public readonly struct SessionOperationalActivityCameraPrepareCommand
    {
        public SessionOperationalActivityCameraPrepareCommand(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string activityIdentity,
            SessionOperationalRouteCompletionHandoffKind completionHandoff,
            string activeSceneName,
            ActivityPresentationProfileAsset activityPresentationProfile,
            string source,
            string reason)
        {
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            ActivityIdentity = Normalize(activityIdentity);
            CompletionHandoff = completionHandoff;
            ActiveSceneName = Normalize(activeSceneName);
            ActivityPresentationProfile = activityPresentationProfile;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string ActivityIdentity { get; }
        public SessionOperationalRouteCompletionHandoffKind CompletionHandoff { get; }
        public string ActiveSceneName { get; }
        public ActivityPresentationProfileAsset ActivityPresentationProfile { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(ActivityIdentity) &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionOperationalActivityCameraPrepareResult
    {
        public SessionOperationalActivityCameraPrepareResult(
            SessionOperationalActivityCameraPrepareOutcomeKind outcomeKind,
            ActivityCameraReadyFact readyFact,
            ActivityCameraFailureFact failureFact,
            string reason,
            string skipReason)
        {
            OutcomeKind = outcomeKind;
            ReadyFact = readyFact;
            FailureFact = failureFact;
            Reason = Normalize(reason);
            SkipReason = Normalize(skipReason);
        }

        public SessionOperationalActivityCameraPrepareOutcomeKind OutcomeKind { get; }
        public ActivityCameraReadyFact ReadyFact { get; }
        public ActivityCameraFailureFact FailureFact { get; }
        public string Reason { get; }
        public string SkipReason { get; }

        public bool IsPrepared => OutcomeKind == SessionOperationalActivityCameraPrepareOutcomeKind.Prepared && ReadyFact != null;
        public bool IsSkipped => OutcomeKind == SessionOperationalActivityCameraPrepareOutcomeKind.Skipped;
        public bool IsFailed => OutcomeKind == SessionOperationalActivityCameraPrepareOutcomeKind.Failed;

        public static SessionOperationalActivityCameraPrepareResult Prepared(
            ActivityCameraReadyFact readyFact,
            string reason)
        {
            return new SessionOperationalActivityCameraPrepareResult(
                SessionOperationalActivityCameraPrepareOutcomeKind.Prepared,
                readyFact,
                null,
                reason,
                string.Empty);
        }

        public static SessionOperationalActivityCameraPrepareResult Skipped(
            string skipReason)
        {
            return new SessionOperationalActivityCameraPrepareResult(
                SessionOperationalActivityCameraPrepareOutcomeKind.Skipped,
                null,
                null,
                skipReason,
                skipReason);
        }

        public static SessionOperationalActivityCameraPrepareResult Failed(
            ActivityCameraFailureFact failureFact,
            string reason)
        {
            return new SessionOperationalActivityCameraPrepareResult(
                SessionOperationalActivityCameraPrepareOutcomeKind.Failed,
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

    public enum SessionOperationalActivityCameraReleaseOutcomeKind
    {
        Released = 0,
        Skipped = 1,
        Failed = 2,
    }

    public readonly struct SessionOperationalActivityCameraReleaseCommand
    {
        public SessionOperationalActivityCameraReleaseCommand(
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

    public readonly struct SessionOperationalActivityCameraReleaseResult
    {
        public SessionOperationalActivityCameraReleaseResult(
            SessionOperationalActivityCameraReleaseOutcomeKind outcomeKind,
            ActivityCameraReleasedFact releasedFact,
            ActivityCameraReleaseFailureFact failureFact,
            string reason,
            string skipReason)
        {
            OutcomeKind = outcomeKind;
            ReleasedFact = releasedFact;
            FailureFact = failureFact;
            Reason = Normalize(reason);
            SkipReason = Normalize(skipReason);
        }

        public SessionOperationalActivityCameraReleaseOutcomeKind OutcomeKind { get; }
        public ActivityCameraReleasedFact ReleasedFact { get; }
        public ActivityCameraReleaseFailureFact FailureFact { get; }
        public string Reason { get; }
        public string SkipReason { get; }

        public bool IsReleased => OutcomeKind == SessionOperationalActivityCameraReleaseOutcomeKind.Released && ReleasedFact != null;
        public bool IsSkipped => OutcomeKind == SessionOperationalActivityCameraReleaseOutcomeKind.Skipped;
        public bool IsFailed => OutcomeKind == SessionOperationalActivityCameraReleaseOutcomeKind.Failed;

        public static SessionOperationalActivityCameraReleaseResult Released(
            ActivityCameraReleasedFact releasedFact,
            string reason)
        {
            return new SessionOperationalActivityCameraReleaseResult(
                SessionOperationalActivityCameraReleaseOutcomeKind.Released,
                releasedFact,
                null,
                reason,
                string.Empty);
        }

        public static SessionOperationalActivityCameraReleaseResult Skipped(
            string skipReason)
        {
            return new SessionOperationalActivityCameraReleaseResult(
                SessionOperationalActivityCameraReleaseOutcomeKind.Skipped,
                null,
                null,
                skipReason,
                skipReason);
        }

        public static SessionOperationalActivityCameraReleaseResult Failed(
            ActivityCameraReleaseFailureFact failureFact,
            string reason)
        {
            return new SessionOperationalActivityCameraReleaseResult(
                SessionOperationalActivityCameraReleaseOutcomeKind.Failed,
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

    public interface ISessionOperationalActivityCameraAdapter
    {
        bool TryPrepareActivityCamera(
            SessionOperationalActivityCameraPrepareCommand command,
            out SessionOperationalActivityCameraPrepareResult result,
            out string reason);

        bool TryReleaseActivityCamera(
            SessionOperationalActivityCameraReleaseCommand command,
            out SessionOperationalActivityCameraReleaseResult result,
            out string reason);
    }
}
