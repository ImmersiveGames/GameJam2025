namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public interface IPhaseCatalogNavigationService
    {
        IPhaseDefinitionCatalog Catalog { get; }
        PhaseCatalogTraversalMode TraversalMode { get; }
        PhaseDefinitionAsset CurrentCommitted { get; }
        PhaseDefinitionAsset PendingTarget { get; }
        bool Looping { get; }
        int LoopCount { get; }

        PhaseCatalogNavigationPlan ResolveNext(string reason = null);
        PhaseCatalogNavigationPlan AdvancePhase(string reason = null);
        PhaseCatalogNavigationPlan ResolvePrevious(string reason = null);
        PhaseCatalogNavigationPlan ResolveSpecificPhase(string phaseId, string reason = null);
        PhaseCatalogNavigationPlan RestartCatalog(string reason = null);
        void Commit(PhaseCatalogNavigationPlan navigationPlan);
    }

    public readonly struct PhaseCatalogNavigationPlan
    {
        public PhaseCatalogNavigationPlan(
            PhaseNavigationRequest request,
            PhaseNavigationOutcome outcome,
            PhaseDefinitionAsset currentCommitted,
            PhaseDefinitionAsset targetPhaseRef,
            PhaseCatalogTraversalMode traversalMode,
            bool wasWrapped,
            string catalogName)
        {
            Request = request;
            Outcome = outcome;
            CurrentCommitted = currentCommitted;
            TargetPhaseRef = targetPhaseRef;
            TraversalMode = traversalMode;
            WasWrapped = wasWrapped;
            CatalogName = string.IsNullOrWhiteSpace(catalogName) ? string.Empty : catalogName.Trim();
        }

        public PhaseNavigationRequest Request { get; }
        public PhaseNavigationOutcome Outcome { get; }
        public PhaseDefinitionAsset CurrentCommitted { get; }
        public PhaseDefinitionAsset TargetPhaseRef { get; }
        public PhaseCatalogTraversalMode TraversalMode { get; }
        public bool WasWrapped { get; }
        public string CatalogName { get; }

        public PhaseNavigationRequestKind RequestKind => Request.Kind;
        public PhaseNavigationDirection Direction => Request.Direction;
        public string Reason => Request.Reason;
        public bool IsChanged => Outcome == PhaseNavigationOutcome.Changed;
        public bool HasTarget => TargetPhaseRef != null;

        public bool IsValid =>
            CurrentCommitted != null &&
            CurrentCommitted.PhaseId.IsValid &&
            (Outcome != PhaseNavigationOutcome.Changed || HasTarget);

        public static PhaseCatalogNavigationPlan CreateChanged(
            PhaseNavigationRequest request,
            PhaseDefinitionAsset currentCommitted,
            PhaseDefinitionAsset targetPhaseRef,
            PhaseCatalogTraversalMode traversalMode,
            bool wasWrapped,
            string catalogName)
        {
            if (targetPhaseRef == null || !targetPhaseRef.PhaseId.IsValid)
            {
                throw new System.InvalidOperationException("[FATAL][Config][PhaseDefinition] Navigation plan requires a valid target phase.");
            }

            return new PhaseCatalogNavigationPlan(
                request,
                PhaseNavigationOutcome.Changed,
                currentCommitted,
                targetPhaseRef,
                traversalMode,
                wasWrapped,
                catalogName);
        }

        public static PhaseCatalogNavigationPlan CreateBlocked(
            PhaseNavigationRequest request,
            PhaseNavigationOutcome outcome,
            PhaseDefinitionAsset currentCommitted,
            PhaseCatalogTraversalMode traversalMode,
            string catalogName)
        {
            if (outcome == PhaseNavigationOutcome.Changed)
            {
                throw new System.InvalidOperationException("[FATAL][Config][PhaseDefinition] Blocked navigation plan cannot use Changed outcome.");
            }

            return new PhaseCatalogNavigationPlan(
                request,
                outcome,
                currentCommitted,
                null,
                traversalMode,
                false,
                catalogName);
        }
    }
}
