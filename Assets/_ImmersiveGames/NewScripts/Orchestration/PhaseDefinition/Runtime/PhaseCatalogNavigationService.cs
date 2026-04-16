using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseCatalogNavigationService : IPhaseCatalogNavigationService
    {
        private readonly IPhaseDefinitionCatalog _catalog;
        private readonly IPhaseCatalogRuntimeStateService _runtimeStateService;

        public PhaseCatalogNavigationService(
            IPhaseDefinitionCatalog catalog,
            IPhaseCatalogRuntimeStateService runtimeStateService)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _runtimeStateService = runtimeStateService ?? throw new ArgumentNullException(nameof(runtimeStateService));

            if (!ReferenceEquals(_runtimeStateService.Catalog, _catalog))
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] Phase catalog navigation service requires a single canonical catalog/runtime state pair.");
            }
        }

        public IPhaseDefinitionCatalog Catalog => _catalog;
        public PhaseCatalogTraversalMode TraversalMode => _runtimeStateService.TraversalMode;
        public PhaseDefinitionAsset CurrentCommitted => _runtimeStateService.CurrentCommitted;
        public PhaseDefinitionAsset PendingTarget => _runtimeStateService.PendingTarget;
        public bool Looping => _runtimeStateService.Looping;
        public int LoopCount => _runtimeStateService.LoopCount;

        public PhaseCatalogNavigationPlan ResolveNext(string reason = null)
        {
            return ResolveDirectionalPlan(
                PhaseNavigationRequest.Next(reason),
                PhaseNavigationDirection.Next);
        }

        public PhaseCatalogNavigationPlan AdvancePhase(string reason = null)
        {
            return ResolveNext(reason);
        }

        public PhaseCatalogNavigationPlan ResolvePrevious(string reason = null)
        {
            return ResolveDirectionalPlan(
                PhaseNavigationRequest.Previous(reason),
                PhaseNavigationDirection.Previous);
        }

        public PhaseCatalogNavigationPlan ResolveSpecificPhase(string phaseId, string reason = null)
        {
            string normalizedReason = PhaseNextPhaseServiceSupport.NormalizeReason(reason);
            PhaseNavigationRequest request = PhaseNavigationRequest.Specific(phaseId, normalizedReason);
            PhaseDefinitionAsset currentCommitted = ResolveCurrentCommittedOrFail(normalizedReason);
            string catalogName = PhaseNextPhaseServiceSupport.DescribeCatalog(_catalog);

            if (string.IsNullOrWhiteSpace(phaseId))
            {
                HardFailFastH1.Trigger(typeof(PhaseCatalogNavigationService),
                    $"[FATAL][H1][PhaseDefinition] Specific phase resolution requires a valid phaseId. reason='{normalizedReason}'.");
            }

            if (!_catalog.TryGet(phaseId, out PhaseDefinitionAsset targetPhaseRef) || targetPhaseRef == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseCatalogNavigationService),
                    $"[FATAL][H1][PhaseDefinition] Specific phase '{PhaseDefinitionId.Normalize(phaseId)}' is not present in catalog '{catalogName}'. reason='{normalizedReason}'.");
            }

            if (HasSamePhase(currentCommitted, targetPhaseRef))
            {
                return PhaseCatalogNavigationPlan.CreateBlocked(
                    request,
                    PhaseNavigationOutcome.TargetAlreadyCurrent,
                    currentCommitted,
                    TraversalMode,
                    catalogName);
            }

            return PhaseCatalogNavigationPlan.CreateChanged(
                request,
                currentCommitted,
                targetPhaseRef,
                TraversalMode,
                wasWrapped: false,
                catalogName);
        }

        public PhaseCatalogNavigationPlan RestartCatalog(string reason = null)
        {
            string normalizedReason = PhaseNextPhaseServiceSupport.NormalizeReason(reason);
            PhaseDefinitionAsset currentCommitted = ResolveCurrentCommittedOrFail(normalizedReason);
            PhaseDefinitionAsset targetPhaseRef = _catalog.ResolveInitialOrFail();
            PhaseNavigationRequest request = PhaseNavigationRequest.RestartCatalog(targetPhaseRef.PhaseId.Value, normalizedReason);
            string catalogName = PhaseNextPhaseServiceSupport.DescribeCatalog(_catalog);

            return PhaseCatalogNavigationPlan.CreateChanged(
                request,
                currentCommitted,
                targetPhaseRef,
                TraversalMode,
                wasWrapped: false,
                catalogName);
        }

        public void Commit(PhaseCatalogNavigationPlan navigationPlan)
        {
            if (!navigationPlan.IsChanged)
            {
                HardFailFastH1.Trigger(typeof(PhaseCatalogNavigationService),
                    $"[FATAL][H1][PhaseDefinition] Commit requires a changed navigation plan. outcome='{navigationPlan.Outcome}' reason='{navigationPlan.Reason}'.");
            }

            if (navigationPlan.CurrentCommitted == null || !navigationPlan.CurrentCommitted.PhaseId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseCatalogNavigationService),
                    $"[FATAL][H1][PhaseDefinition] Commit requires a valid currentCommitted phase. reason='{navigationPlan.Reason}'.");
            }

            if (navigationPlan.TargetPhaseRef == null || !navigationPlan.TargetPhaseRef.PhaseId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseCatalogNavigationService),
                    $"[FATAL][H1][PhaseDefinition] Commit requires a valid target phase. reason='{navigationPlan.Reason}'.");
            }

            PhaseDefinitionAsset runtimeCurrentCommitted = CurrentCommitted;
            if (!HasSamePhase(runtimeCurrentCommitted, navigationPlan.CurrentCommitted))
            {
                HardFailFastH1.Trigger(typeof(PhaseCatalogNavigationService),
                    $"[FATAL][H1][PhaseDefinition] Commit rejected because the runtime committed phase changed while plan was being applied. planCurrent='{PhaseNextPhaseServiceSupport.DescribePhase(navigationPlan.CurrentCommitted)}' runtimeCurrent='{PhaseNextPhaseServiceSupport.DescribePhase(runtimeCurrentCommitted)}' target='{PhaseNextPhaseServiceSupport.DescribePhase(navigationPlan.TargetPhaseRef)}' reason='{navigationPlan.Reason}'.");
            }

            _runtimeStateService.SetPendingTarget(navigationPlan.TargetPhaseRef, navigationPlan.Reason);
            _runtimeStateService.CommitCurrentTarget(navigationPlan.TargetPhaseRef, navigationPlan.Reason);

            if (navigationPlan.WasWrapped)
            {
                _runtimeStateService.RegisterTraversalWrap(navigationPlan.Direction, navigationPlan.Reason);
            }
        }

        private PhaseCatalogNavigationPlan ResolveDirectionalPlan(PhaseNavigationRequest request, PhaseNavigationDirection expectedDirection)
        {
            string normalizedReason = PhaseNextPhaseServiceSupport.NormalizeReason(request.Reason);
            PhaseDefinitionAsset currentCommitted = ResolveCurrentCommittedOrFail(normalizedReason);
            string catalogName = PhaseNextPhaseServiceSupport.DescribeCatalog(_catalog);

            if (expectedDirection == PhaseNavigationDirection.Next)
            {
                if (_catalog.TryGetNext(currentCommitted.PhaseId.Value, out PhaseDefinitionAsset nextPhaseRef) && nextPhaseRef != null)
                {
                    return PhaseCatalogNavigationPlan.CreateChanged(
                        request,
                        currentCommitted,
                        nextPhaseRef,
                        TraversalMode,
                        wasWrapped: false,
                        catalogName);
                }

                if (!Looping)
                {
                    return PhaseCatalogNavigationPlan.CreateBlocked(
                        request,
                        PhaseNavigationOutcome.BlockedAtLast,
                        currentCommitted,
                        TraversalMode,
                        catalogName);
                }

                PhaseDefinitionAsset wrappedPhaseRef = _catalog.ResolveInitialOrFail();
                return PhaseCatalogNavigationPlan.CreateChanged(
                    request,
                    currentCommitted,
                    wrappedPhaseRef,
                    TraversalMode,
                    wasWrapped: true,
                    catalogName);
            }

            if (expectedDirection == PhaseNavigationDirection.Previous)
            {
                if (_catalog.TryGetPrevious(currentCommitted.PhaseId.Value, out PhaseDefinitionAsset previousPhaseRef) && previousPhaseRef != null)
                {
                    return PhaseCatalogNavigationPlan.CreateChanged(
                        request,
                        currentCommitted,
                        previousPhaseRef,
                        TraversalMode,
                        wasWrapped: false,
                        catalogName);
                }

                if (!Looping)
                {
                    return PhaseCatalogNavigationPlan.CreateBlocked(
                        request,
                        PhaseNavigationOutcome.BlockedAtFirst,
                        currentCommitted,
                        TraversalMode,
                        catalogName);
                }

                IReadOnlyList<string> phaseIds = _catalog.PhaseIds;
                if (phaseIds == null || phaseIds.Count == 0)
                {
                    HardFailFastH1.Trigger(typeof(PhaseCatalogNavigationService),
                        $"[FATAL][H1][PhaseDefinition] Catalog '{catalogName}' has no phaseIds for looping traversal.");
                }

                string lastPhaseId = phaseIds[phaseIds.Count - 1];
                PhaseDefinitionAsset wrappedPhaseRef = _catalog.ResolveSpecificPhaseOrFail(lastPhaseId);
                return PhaseCatalogNavigationPlan.CreateChanged(
                    request,
                    currentCommitted,
                    wrappedPhaseRef,
                    TraversalMode,
                    wasWrapped: true,
                    catalogName);
            }

            HardFailFastH1.Trigger(typeof(PhaseCatalogNavigationService),
                $"[FATAL][H1][PhaseDefinition] Unsupported traversal direction '{expectedDirection}' for directional resolution.");
            return default;
        }

        private PhaseDefinitionAsset ResolveCurrentCommittedOrFail(string reason)
        {
            PhaseDefinitionAsset currentCommitted = CurrentCommitted;
            if (currentCommitted == null || !currentCommitted.PhaseId.IsValid)
            {
                HardFailFastH1.Trigger(typeof(PhaseCatalogNavigationService),
                    $"[FATAL][H1][PhaseDefinition] Navigation requires a committed current phase. reason='{reason}'.");
            }

            if (!_catalog.TryGet(currentCommitted.PhaseId.Value, out _))
            {
                HardFailFastH1.Trigger(typeof(PhaseCatalogNavigationService),
                    $"[FATAL][H1][PhaseDefinition] Current committed phase is not present in the catalog. currentPhase='{PhaseNextPhaseServiceSupport.DescribePhase(currentCommitted)}' reason='{reason}'.");
            }

            return currentCommitted;
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
    }
}
