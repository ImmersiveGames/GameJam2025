using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation
{
    public enum PhaseNavigationOutcome
    {
        Changed = 0,
        BlockedAtFirst = 1,
        BlockedAtLast = 2,
        RejectedNotReady = 3,
        InvalidCurrentPhase = 4,
        InvalidCatalog = 5,
        SpecificPhaseIdInvalid = 6,
        SpecificPhaseMissing = 7,
        TargetAlreadyCurrent = 8
    }

    public enum PhaseNavigationRequestKind
    {
        Next = 0,
        Previous = 1,
        Specific = 2,
        RestartCatalog = 3,
        FirstPhase = 4
    }

    public readonly struct PhaseNavigationRequest
    {
        public PhaseNavigationRequest(PhaseNavigationRequestKind kind, PhaseNavigationDirection direction, string reason, string targetPhaseId = null)
        {
            Kind = kind;
            Direction = direction;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            TargetPhaseId = string.IsNullOrWhiteSpace(targetPhaseId) ? string.Empty : targetPhaseId.Trim();
        }

        public PhaseNavigationRequestKind Kind { get; }
        public PhaseNavigationDirection Direction { get; }
        public string Reason { get; }
        public string TargetPhaseId { get; }
        public bool HasTargetPhaseId => !string.IsNullOrWhiteSpace(TargetPhaseId);

        public static PhaseNavigationRequest Next(string reason = null)
            => new(PhaseNavigationRequestKind.Next, PhaseNavigationDirection.Next, reason);

        public static PhaseNavigationRequest Previous(string reason = null)
            => new(PhaseNavigationRequestKind.Previous, PhaseNavigationDirection.Previous, reason);

        public static PhaseNavigationRequest Specific(string phaseId, string reason = null)
            => new(PhaseNavigationRequestKind.Specific, PhaseNavigationDirection.Specific, reason, phaseId);

        public static PhaseNavigationRequest RestartCatalog(string phaseId, string reason = null)
            => new(PhaseNavigationRequestKind.RestartCatalog, PhaseNavigationDirection.Specific, reason, phaseId);

        public static PhaseNavigationRequest FirstPhase(string phaseId, string reason = null)
            => new(PhaseNavigationRequestKind.FirstPhase, PhaseNavigationDirection.Specific, reason, phaseId);
    }

    public readonly struct PhaseNavigationResult
    {
        public PhaseNavigationResult(
            PhaseNavigationRequest request,
            PhaseNavigationOutcome outcome,
            PhaseDefinitionAsset currentPhaseRef,
            string catalogName,
            PhaseCatalogTraversalMode traversalMode,
            bool wasWrapped,
            PhaseNavigationSelectionContext selectionContext)
        {
            Request = request;
            Outcome = outcome;
            CurrentPhaseRef = currentPhaseRef;
            CatalogName = string.IsNullOrWhiteSpace(catalogName) ? string.Empty : catalogName.Trim();
            TraversalMode = traversalMode;
            WasWrapped = wasWrapped;
            SelectionContext = selectionContext;
        }

        public PhaseNavigationRequest Request { get; }
        public PhaseNavigationOutcome Outcome { get; }
        public PhaseDefinitionAsset CurrentPhaseRef { get; }
        public string CatalogName { get; }
        public PhaseCatalogTraversalMode TraversalMode { get; }
        public bool WasWrapped { get; }
        public PhaseNavigationSelectionContext SelectionContext { get; }

        public PhaseNavigationDirection Direction => Request.Direction;
        public string Reason => Request.Reason;
        public PhaseDefinitionAsset TargetPhaseRef => SelectionContext.TargetPhaseRef;
        public string FromPhaseId => CurrentPhaseRef != null && CurrentPhaseRef.PhaseId.IsValid ? CurrentPhaseRef.PhaseId.Value : string.Empty;
        public string ToPhaseId => TargetPhaseRef != null && TargetPhaseRef.PhaseId.IsValid ? TargetPhaseRef.PhaseId.Value : string.Empty;
        public bool HasSelectionContext => Outcome == PhaseNavigationOutcome.Changed && SelectionContext.IsValid;
        public bool IsBlockedAtBoundary =>
            Outcome == PhaseNavigationOutcome.BlockedAtFirst ||
            Outcome == PhaseNavigationOutcome.BlockedAtLast;
    }

    public enum PhaseNavigationDirection
    {
        Next = 0,
        Previous = 1,
        Specific = 2
    }

    public readonly struct PhaseNavigationSelectionContext
    {
        public PhaseNavigationSelectionContext(
            GameplayStartSnapshot currentSnapshot,
            PhaseDefinitionAsset targetPhaseRef,
            PhaseDefinitionSelectedEvent phaseSelectedEvent,
            string reason,
            string targetSceneName,
            PhaseNavigationDirection direction,
            bool forceFullReload)
        {
            CurrentSnapshot = currentSnapshot;
            TargetPhaseRef = targetPhaseRef;
            PhaseSelectedEvent = phaseSelectedEvent;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            TargetSceneName = string.IsNullOrWhiteSpace(targetSceneName) ? string.Empty : targetSceneName.Trim();
            Direction = direction;
            ForceFullReload = forceFullReload;
        }

        public GameplayStartSnapshot CurrentSnapshot { get; }
        public PhaseDefinitionAsset CurrentPhaseRef => CurrentSnapshot.PhaseDefinitionRef;
        public PhaseDefinitionAsset TargetPhaseRef { get; }
        public PhaseDefinitionSelectedEvent PhaseSelectedEvent { get; }
        public string Reason { get; }
        public string TargetSceneName { get; }
        public PhaseNavigationDirection Direction { get; }
        public bool ForceFullReload { get; }

        public int SelectionVersion => PhaseSelectedEvent.SelectionVersion;
        public string TargetIntroContentId => TargetPhaseRef != null ? PhaseDefinitionId.BuildCanonicalIntroContentId(TargetPhaseRef.PhaseId) : string.Empty;
        public bool IsValid =>
            CurrentSnapshot.IsValid &&
            TargetPhaseRef != null &&
            PhaseSelectedEvent.IsValid &&
            !string.IsNullOrWhiteSpace(TargetSceneName);
    }
}
