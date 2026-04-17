using System;
using ImmersiveGames.GameJam2025.Core.Events;
using ImmersiveGames.GameJam2025.Core.Logging;

namespace ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime
{
    public sealed class PhaseCatalogRuntimeStateService : IPhaseCatalogRuntimeStateService
    {
        private readonly object _sync = new();
        private PhaseDefinitionAsset _currentCommitted;
        private PhaseDefinitionAsset _pendingTarget;

        public PhaseCatalogRuntimeStateService(IPhaseDefinitionCatalog catalog)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            TraversalMode = Catalog.TraversalMode;
            Looping = TraversalMode == PhaseCatalogTraversalMode.Looping;
            _currentCommitted = Catalog.ResolveInitialOrFail();
            _pendingTarget = null;
            LoopCount = 0;

            if (_currentCommitted == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] Catalog runtime state could not resolve the initial committed phase.");
            }

            DebugUtility.LogVerbose(typeof(PhaseCatalogRuntimeStateService),
                $"[OBS][PhaseFlow][State] RuntimeStateInitialized currentCommitted='{DescribePhase(_currentCommitted)}' pendingTarget='<none>' looping='{Looping}' traversalMode='{TraversalMode}' loopCount='{LoopCount}'.",
                DebugUtility.Colors.Info);
        }

        public IPhaseDefinitionCatalog Catalog { get; }
        public int LoopCount { get; private set; }
        public bool Looping { get; }
        public PhaseCatalogTraversalMode TraversalMode { get; }

        public PhaseDefinitionAsset CurrentCommitted
        {
            get
            {
                lock (_sync)
                {
                    return _currentCommitted;
                }
            }
        }

        public PhaseDefinitionAsset PendingTarget
        {
            get
            {
                lock (_sync)
                {
                    return _pendingTarget;
                }
            }
        }

        public void SetPendingTarget(PhaseDefinitionAsset targetPhaseRef, string reason = null)
        {
            ValidateTargetOrFail(targetPhaseRef, nameof(SetPendingTarget), reason);
            PhaseDefinitionAsset previousPendingTarget;

            lock (_sync)
            {
                previousPendingTarget = _pendingTarget;
                _pendingTarget = targetPhaseRef;
            }

            DebugUtility.LogVerbose(typeof(PhaseCatalogRuntimeStateService),
                $"[OBS][PhaseFlow][State] PendingTargetUpdated pendingTarget='{DescribePhase(targetPhaseRef)}' reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Info);

            if (!HasSamePhase(previousPendingTarget, targetPhaseRef))
            {
                EventBus<PhaseCatalogPendingTargetChangedEvent>.Raise(new PhaseCatalogPendingTargetChangedEvent(
                    DescribePhase(previousPendingTarget),
                    DescribePhase(targetPhaseRef),
                    NormalizeReason(reason),
                    false));
            }
        }

        public void CommitCurrentTarget(PhaseDefinitionAsset targetPhaseRef, string reason = null)
        {
            ValidateTargetOrFail(targetPhaseRef, nameof(CommitCurrentTarget), reason);
            PhaseDefinitionAsset previousCommittedTarget;
            bool committedChanged;

            lock (_sync)
            {
                previousCommittedTarget = _currentCommitted;
                if (_pendingTarget != null && !ReferenceEquals(_pendingTarget, targetPhaseRef))
                {
                    FailFastConfig($"CommitCurrentTarget mismatch: pendingTarget='{DescribePhase(_pendingTarget)}' target='{DescribePhase(targetPhaseRef)}' reason='{NormalizeReason(reason)}'.");
                }

                _currentCommitted = targetPhaseRef;
                committedChanged = !HasSamePhase(previousCommittedTarget, targetPhaseRef);
            }

            DebugUtility.LogVerbose(typeof(PhaseCatalogRuntimeStateService),
                $"[OBS][PhaseFlow][State] CurrentCommittedUpdated currentCommitted='{DescribePhase(targetPhaseRef)}' pendingTarget='{DescribePhase(_pendingTarget)}' pendingTargetRetainedUntilHandoff='true' loopCount='{LoopCount}' reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Info);

            if (committedChanged)
            {
                EventBus<PhaseCatalogCurrentCommittedChangedEvent>.Raise(new PhaseCatalogCurrentCommittedChangedEvent(
                    DescribePhase(previousCommittedTarget),
                    DescribePhase(targetPhaseRef),
                    NormalizeReason(reason)));
            }
        }

        public void RegisterTraversalWrap(PhaseNavigationDirection direction, string reason = null)
        {
            string normalizedReason = NormalizeReason(reason);
            string directionLabel = DescribeDirection(direction);
            int previousLoopCount;
            int nextLoopCount;

            lock (_sync)
            {
                if (direction == PhaseNavigationDirection.Next)
                {
                    previousLoopCount = LoopCount;
                    LoopCount++;
                    nextLoopCount = LoopCount;
                    DebugUtility.LogVerbose(typeof(PhaseCatalogRuntimeStateService),
                        $"[OBS][PhaseFlow][State] LoopCountUpdated direction='{directionLabel}' delta='+1' loopCount='{LoopCount}' reason='{normalizedReason}' wrapKind='forward_catalog_wrap'.",
                        DebugUtility.Colors.Info);
                    EventBus<PhaseCatalogLoopCountChangedEvent>.Raise(new PhaseCatalogLoopCountChangedEvent(
                        previousLoopCount,
                        nextLoopCount,
                        direction,
                        normalizedReason,
                        true));
                    return;
                }

                if (direction == PhaseNavigationDirection.Previous)
                {
                    DebugUtility.LogVerbose(typeof(PhaseCatalogRuntimeStateService),
                        $"[OBS][PhaseFlow][State] LoopCountUnchanged direction='{directionLabel}' delta='0' loopCount='{LoopCount}' reason='{normalizedReason}' wrapKind='retrograde_catalog_wrap' backwardWrapCounted='false'.",
                        DebugUtility.Colors.Info);
                    return;
                }
            }

            FailFastConfig($"RegisterTraversalWrap received unsupported direction='{directionLabel}' reason='{normalizedReason}'.");
        }

        public void ClearPendingTarget(string reason = null)
        {
            PhaseDefinitionAsset previousPendingTarget;

            lock (_sync)
            {
                previousPendingTarget = _pendingTarget;
                if (_pendingTarget == null)
                {
                    return;
                }

                DebugUtility.LogVerbose(typeof(PhaseCatalogRuntimeStateService),
                    $"[OBS][PhaseFlow][State] PendingTargetCleared pendingTarget='{DescribePhase(_pendingTarget)}' reason='{NormalizeReason(reason)}' clearedAfterHandoff='true'.",
                    DebugUtility.Colors.Info);

                _pendingTarget = null;
            }

            EventBus<PhaseCatalogPendingTargetChangedEvent>.Raise(new PhaseCatalogPendingTargetChangedEvent(
                DescribePhase(previousPendingTarget),
                string.Empty,
                NormalizeReason(reason),
                true));
        }

        private static void ValidateTargetOrFail(PhaseDefinitionAsset targetPhaseRef, string operation, string reason)
        {
            if (targetPhaseRef == null || !targetPhaseRef.PhaseId.IsValid)
            {
                FailFastConfig($"{operation} received invalid target phase. reason='{NormalizeReason(reason)}'.");
            }
        }

        private static string DescribePhase(PhaseDefinitionAsset phaseDefinition)
        {
            return phaseDefinition != null && phaseDefinition.PhaseId.IsValid
                ? phaseDefinition.PhaseId.Value
                : "<none>";
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim();
        }

        private static string DescribeDirection(PhaseNavigationDirection direction)
        {
            return direction == PhaseNavigationDirection.Previous
                ? "Previous"
                : direction == PhaseNavigationDirection.Next
                    ? "Next"
                    : direction.ToString();
        }

        private static bool HasSamePhase(PhaseDefinitionAsset left, PhaseDefinitionAsset right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || !left.PhaseId.IsValid || !right.PhaseId.IsValid)
            {
                return false;
            }

            return string.Equals(left.PhaseId.Value, right.PhaseId.Value, StringComparison.OrdinalIgnoreCase);
        }

        private static void FailFastConfig(string detail)
        {
            string message = $"[FATAL][Config][PhaseDefinition] {detail}";
            DebugUtility.LogError(typeof(PhaseCatalogRuntimeStateService), message);
            throw new InvalidOperationException(message);
        }
    }

    public readonly struct PhaseCatalogPendingTargetChangedEvent : IEvent
    {
        public PhaseCatalogPendingTargetChangedEvent(
            string previousPendingTargetId,
            string pendingTargetId,
            string reason,
            bool isCleared)
        {
            PreviousPendingTargetId = string.IsNullOrWhiteSpace(previousPendingTargetId) ? string.Empty : previousPendingTargetId.Trim();
            PendingTargetId = string.IsNullOrWhiteSpace(pendingTargetId) ? string.Empty : pendingTargetId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            IsCleared = isCleared;
        }

        public string PreviousPendingTargetId { get; }
        public string PendingTargetId { get; }
        public string Reason { get; }
        public bool IsCleared { get; }
    }

    public readonly struct PhaseCatalogCurrentCommittedChangedEvent : IEvent
    {
        public PhaseCatalogCurrentCommittedChangedEvent(string previousCommittedId, string currentCommittedId, string reason)
        {
            PreviousCommittedId = string.IsNullOrWhiteSpace(previousCommittedId) ? string.Empty : previousCommittedId.Trim();
            CurrentCommittedId = string.IsNullOrWhiteSpace(currentCommittedId) ? string.Empty : currentCommittedId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public string PreviousCommittedId { get; }
        public string CurrentCommittedId { get; }
        public string Reason { get; }
    }

    public readonly struct PhaseCatalogLoopCountChangedEvent : IEvent
    {
        public PhaseCatalogLoopCountChangedEvent(
            int previousLoopCount,
            int loopCount,
            PhaseNavigationDirection direction,
            string reason,
            bool wasWrapped)
        {
            PreviousLoopCount = previousLoopCount;
            LoopCount = loopCount;
            Direction = direction;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            WasWrapped = wasWrapped;
        }

        public int PreviousLoopCount { get; }
        public int LoopCount { get; }
        public PhaseNavigationDirection Direction { get; }
        public string Reason { get; }
        public bool WasWrapped { get; }
    }

}

