namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum SessionActivityPredefinedVisualReadinessKind
    {
        Unknown = 0,
        NotRequired = 1,
        Waiting = 2,
        Ready = 3,
        RejectedForeignOrStale = 4,
        Failed = 5,
    }

    public readonly struct SessionActivityPredefinedVisualReadinessResult
    {
        public SessionActivityPredefinedVisualReadinessResult(
            SessionActivityPredefinedVisualReadinessKind kind,
            string sessionStateId,
            string routeOperationId,
            string activityId,
            int entrySequence,
            SessionActivityStage stage,
            string reason,
            string detail)
        {
            Kind = kind;
            SessionStateId = Normalize(sessionStateId);
            RouteOperationId = Normalize(routeOperationId);
            ActivityId = Normalize(activityId);
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            Stage = stage;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public SessionActivityPredefinedVisualReadinessKind Kind { get; }
        public string SessionStateId { get; }
        public string RouteOperationId { get; }
        public string ActivityId { get; }
        public int EntrySequence { get; }
        public SessionActivityStage Stage { get; }
        public string Reason { get; }
        public string Detail { get; }

        public bool IsValid =>
            Kind != SessionActivityPredefinedVisualReadinessKind.Unknown &&
            !string.IsNullOrWhiteSpace(Reason);

        public bool IsReady => Kind == SessionActivityPredefinedVisualReadinessKind.Ready;
        public bool IsWaiting => Kind == SessionActivityPredefinedVisualReadinessKind.Waiting;
        public bool IsFailed => Kind == SessionActivityPredefinedVisualReadinessKind.Failed;
        public bool IsRejectedForeignOrStale => Kind == SessionActivityPredefinedVisualReadinessKind.RejectedForeignOrStale;
        public bool IsNotRequired => Kind == SessionActivityPredefinedVisualReadinessKind.NotRequired;

        public override string ToString()
        {
            return $"kind='{Kind}', sessionStateId='{SessionStateId}', routeOperationId='{RouteOperationId}', activityId='{ActivityId}', entrySequence='{EntrySequence}', stage='{Stage}', reason='{Reason}', detail='{Detail}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface ISessionActivityPredefinedVisualReadinessBoundary
    {
        SessionActivityPredefinedVisualReadinessResult ObservePredefinedVisualReadiness(
            string sessionStateId,
            string expectedRouteOperationId,
            string source,
            string reason);
    }
}
