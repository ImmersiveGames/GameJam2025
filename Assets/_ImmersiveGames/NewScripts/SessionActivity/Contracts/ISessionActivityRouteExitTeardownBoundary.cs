using System.Threading;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum SessionActivityRouteExitTeardownKind
    {
        Unknown = 0,
        NotRequired = 1,
        Started = 2,
        InProgress = 3,
        Completed = 4,
        Failed = 5,
    }

    public readonly struct SessionActivityRouteExitTeardownResult
    {
        public SessionActivityRouteExitTeardownResult(
            SessionActivityRouteExitTeardownKind kind,
            string sessionStateId,
            SessionActivityStage stage,
            string activityId,
            bool hasPendingHandoff,
            string reason,
            string detail)
        {
            Kind = kind;
            SessionStateId = Normalize(sessionStateId);
            Stage = stage;
            ActivityId = Normalize(activityId);
            HasPendingHandoff = hasPendingHandoff;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public SessionActivityRouteExitTeardownKind Kind { get; }
        public string SessionStateId { get; }
        public SessionActivityStage Stage { get; }
        public string ActivityId { get; }
        public bool HasPendingHandoff { get; }
        public string Reason { get; }
        public string Detail { get; }

        public bool IsValid =>
            Kind != SessionActivityRouteExitTeardownKind.Unknown &&
            !string.IsNullOrWhiteSpace(Reason);

        public bool IsInProgress => Kind == SessionActivityRouteExitTeardownKind.InProgress || Kind == SessionActivityRouteExitTeardownKind.Started;
        public bool IsCompleted => Kind == SessionActivityRouteExitTeardownKind.Completed || Kind == SessionActivityRouteExitTeardownKind.NotRequired;
        public bool IsFailed => Kind == SessionActivityRouteExitTeardownKind.Failed;

        public override string ToString()
        {
            return $"kind='{Kind}', sessionStateId='{SessionStateId}', stage='{Stage}', activityId='{ActivityId}', hasPendingHandoff='{HasPendingHandoff}', reason='{Reason}', detail='{Detail}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }


    public enum SessionActivitySessionResetKind
    {
        Unknown = 0,
        NotRequired = 1,
        Completed = 2,
        Failed = 3,
    }

    public readonly struct SessionActivitySessionResetResult
    {
        public SessionActivitySessionResetResult(
            SessionActivitySessionResetKind kind,
            string sessionStateId,
            SessionActivityStage stage,
            string activityId,
            int sessionActorCountBefore,
            int sessionActorCountAfter,
            string reason,
            string detail)
        {
            Kind = kind;
            SessionStateId = Normalize(sessionStateId);
            Stage = stage;
            ActivityId = Normalize(activityId);
            SessionActorCountBefore = sessionActorCountBefore;
            SessionActorCountAfter = sessionActorCountAfter;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public SessionActivitySessionResetKind Kind { get; }
        public string SessionStateId { get; }
        public SessionActivityStage Stage { get; }
        public string ActivityId { get; }
        public int SessionActorCountBefore { get; }
        public int SessionActorCountAfter { get; }
        public string Reason { get; }
        public string Detail { get; }

        public bool IsValid =>
            Kind != SessionActivitySessionResetKind.Unknown &&
            !string.IsNullOrWhiteSpace(Reason);

        public bool IsCompleted => Kind == SessionActivitySessionResetKind.Completed || Kind == SessionActivitySessionResetKind.NotRequired;
        public bool IsFailed => Kind == SessionActivitySessionResetKind.Failed;

        public override string ToString()
        {
            return $"kind='{Kind}', sessionStateId='{SessionStateId}', stage='{Stage}', activityId='{ActivityId}', sessionActorCountBefore='{SessionActorCountBefore}', sessionActorCountAfter='{SessionActorCountAfter}', reason='{Reason}', detail='{Detail}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface ISessionActivityRouteExitTeardownBoundary
    {
        SessionActivityRailKind CurrentRailKind { get; }
        SessionActivityStage CurrentStage { get; }
        bool HasPendingOperation { get; }

        SessionActivityRouteExitTeardownResult RequestRouteExitTeardown(
            string sessionStateId,
            string source,
            string reason);

        Task<SessionActivityRouteExitTeardownResult> AwaitRouteExitTeardownAsync(
            string sessionStateId,
            string source,
            string reason,
            CancellationToken cancellationToken);

        SessionActivitySessionResetResult ResetSessionAfterRouteExit(
            string sessionStateId,
            string source,
            string reason);
    }
}
