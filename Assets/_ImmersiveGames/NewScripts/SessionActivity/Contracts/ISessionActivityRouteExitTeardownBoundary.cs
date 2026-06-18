using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum SessionActivityRouteExitTeardownKind
    {
        Unknown = 0,
        NotRequired = 1,
        Started = 2,
        InProgress = 3,
        Completed = 4,
        Failed = 5
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
            SessionStateId = sessionStateId.TrimToEmpty();
            Stage = stage;
            ActivityId = activityId.TrimToEmpty();
            HasPendingHandoff = hasPendingHandoff;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
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
    }


    public enum SessionActivitySessionResetKind
    {
        Unknown = 0,
        NotRequired = 1,
        Completed = 2,
        Failed = 3
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
            SessionStateId = sessionStateId.TrimToEmpty();
            Stage = stage;
            ActivityId = activityId.TrimToEmpty();
            SessionActorCountBefore = sessionActorCountBefore;
            SessionActorCountAfter = sessionActorCountAfter;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
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
    }


    public enum SessionActivityRouteExitTeardownPreflightKind
    {
        Unknown = 0,
        Accepted = 1,
        NotRequired = 2,
        RejectedByPolicy = 3,
        Failed = 4
    }

    public readonly struct SessionActivityRouteExitTeardownPreflightResult
    {
        public SessionActivityRouteExitTeardownPreflightResult(
            SessionActivityRouteExitTeardownPreflightKind kind,
            string sessionStateId,
            SessionActivityStage stage,
            SessionActivityRailKind railKind,
            string activityId,
            bool hasPendingOperation,
            string reason,
            string detail)
        {
            Kind = kind;
            SessionStateId = sessionStateId.TrimToEmpty();
            Stage = stage;
            RailKind = railKind;
            ActivityId = activityId.TrimToEmpty();
            HasPendingOperation = hasPendingOperation;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public SessionActivityRouteExitTeardownPreflightKind Kind { get; }
        public string SessionStateId { get; }
        public SessionActivityStage Stage { get; }
        public SessionActivityRailKind RailKind { get; }
        public string ActivityId { get; }
        public bool HasPendingOperation { get; }
        public string Reason { get; }
        public string Detail { get; }

        public bool IsValid =>
            Kind != SessionActivityRouteExitTeardownPreflightKind.Unknown &&
            !string.IsNullOrWhiteSpace(Reason);

        public bool IsAccepted =>
            Kind == SessionActivityRouteExitTeardownPreflightKind.Accepted ||
            Kind == SessionActivityRouteExitTeardownPreflightKind.NotRequired;

        public bool IsRejected => Kind == SessionActivityRouteExitTeardownPreflightKind.RejectedByPolicy;
        public bool IsFailed => Kind == SessionActivityRouteExitTeardownPreflightKind.Failed;

        public override string ToString()
        {
            return $"kind='{Kind}', sessionStateId='{SessionStateId}', stage='{Stage}', railKind='{RailKind}', activityId='{ActivityId}', hasPendingOperation='{HasPendingOperation}', reason='{Reason}', detail='{Detail}'";
        }
    }

    public interface ISessionActivityRouteExitTeardownBoundary
    {
        SessionActivityRouteExitTeardownPreflightResult EvaluateRouteExitTeardownPreflight(
            string sessionStateId,
            string source,
            string reason);

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
