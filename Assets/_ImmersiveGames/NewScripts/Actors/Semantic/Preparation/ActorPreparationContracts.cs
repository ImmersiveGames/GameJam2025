namespace _ImmersiveGames.NewScripts.Actors.Semantic.Preparation
{
    public enum ActorPreparationOutcome
    {
        Unknown = 0,
        ObservedNoOp = 1,
    }

    public readonly struct ActorPreparationIdentity
    {
        public ActorPreparationIdentity(
            string pipelineId,
            string sessionId,
            string routeIdentity,
            int routeSequence,
            string transitionId)
        {
            PipelineId = Normalize(pipelineId);
            SessionId = Normalize(sessionId);
            RouteIdentity = Normalize(routeIdentity);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            TransitionId = Normalize(transitionId);
        }

        public string PipelineId { get; }
        public string SessionId { get; }
        public string RouteIdentity { get; }
        public int RouteSequence { get; }
        public string TransitionId { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionId) &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(TransitionId);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActorPreparationPlan
    {
        public ActorPreparationPlan(
            ActorPreparationIdentity identity,
            string source,
            string reason)
        {
            Identity = identity;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActorPreparationIdentity Identity { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActorPreparationSnapshot
    {
        public ActorPreparationSnapshot(
            ActorPreparationIdentity identity,
            ActorPreparationOutcome outcome,
            int plannedActorsCount,
            string message)
        {
            Identity = identity;
            Outcome = outcome;
            PlannedActorsCount = plannedActorsCount < 0 ? 0 : plannedActorsCount;
            Message = Normalize(message);
        }

        public ActorPreparationIdentity Identity { get; }
        public ActorPreparationOutcome Outcome { get; }
        public int PlannedActorsCount { get; }
        public string Message { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Outcome != ActorPreparationOutcome.Unknown &&
            !string.IsNullOrWhiteSpace(Message);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActorPreparationResult
    {
        public ActorPreparationResult(ActorPreparationPlan plan, ActorPreparationSnapshot snapshot)
        {
            Plan = plan;
            Snapshot = snapshot;
        }

        public ActorPreparationPlan Plan { get; }
        public ActorPreparationSnapshot Snapshot { get; }
        public bool IsObservedNoOp => Snapshot.Outcome == ActorPreparationOutcome.ObservedNoOp;

        public bool IsValid =>
            Plan.IsValid &&
            Snapshot.IsValid;
    }
}
