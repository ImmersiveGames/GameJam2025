using System;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
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
                $"[OBS][PhaseDefinition][CatalogState] RuntimeStateInitialized currentCommitted='{DescribePhase(_currentCommitted)}' pendingTarget='<none>' looping='{Looping}' traversalMode='{TraversalMode}' loopCount='{LoopCount}'.",
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

            lock (_sync)
            {
                _pendingTarget = targetPhaseRef;
            }

            DebugUtility.LogVerbose(typeof(PhaseCatalogRuntimeStateService),
                $"[OBS][PhaseDefinition][CatalogState] PendingTargetUpdated pendingTarget='{DescribePhase(targetPhaseRef)}' reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Info);
        }

        public void CommitCurrentTarget(PhaseDefinitionAsset targetPhaseRef, string reason = null)
        {
            ValidateTargetOrFail(targetPhaseRef, nameof(CommitCurrentTarget), reason);

            lock (_sync)
            {
                if (_pendingTarget != null && !ReferenceEquals(_pendingTarget, targetPhaseRef))
                {
                    FailFastConfig($"CommitCurrentTarget mismatch: pendingTarget='{DescribePhase(_pendingTarget)}' target='{DescribePhase(targetPhaseRef)}' reason='{NormalizeReason(reason)}'.");
                }

                _currentCommitted = targetPhaseRef;
            }

            DebugUtility.LogVerbose(typeof(PhaseCatalogRuntimeStateService),
                $"[OBS][PhaseDefinition][CatalogState] CurrentCommittedUpdated currentCommitted='{DescribePhase(targetPhaseRef)}' pendingTarget='{DescribePhase(_pendingTarget)}' pendingTargetRetainedUntilHandoff='true' reason='{NormalizeReason(reason)}'.",
                DebugUtility.Colors.Info);
        }

        public void ClearPendingTarget(string reason = null)
        {
            lock (_sync)
            {
                if (_pendingTarget == null)
                {
                    return;
                }

                DebugUtility.LogVerbose(typeof(PhaseCatalogRuntimeStateService),
                    $"[OBS][PhaseDefinition][CatalogState] PendingTargetCleared pendingTarget='{DescribePhase(_pendingTarget)}' reason='{NormalizeReason(reason)}' clearedAfterHandoff='true'.",
                    DebugUtility.Colors.Info);

                _pendingTarget = null;
            }
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

        private static void FailFastConfig(string detail)
        {
            string message = $"[FATAL][Config][PhaseDefinition] {detail}";
            DebugUtility.LogError(typeof(PhaseCatalogRuntimeStateService), message);
            throw new InvalidOperationException(message);
        }
    }
}
