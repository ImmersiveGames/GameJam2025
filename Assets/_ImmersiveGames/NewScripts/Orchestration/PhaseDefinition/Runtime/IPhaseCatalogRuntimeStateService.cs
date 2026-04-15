namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public interface IPhaseCatalogRuntimeStateService
    {
        IPhaseDefinitionCatalog Catalog { get; }
        PhaseDefinitionAsset CurrentCommitted { get; }
        PhaseDefinitionAsset PendingTarget { get; }
        int LoopCount { get; }
        bool Looping { get; }
        PhaseCatalogTraversalMode TraversalMode { get; }

        void SetPendingTarget(PhaseDefinitionAsset targetPhaseRef, string reason = null);
        void CommitCurrentTarget(PhaseDefinitionAsset targetPhaseRef, string reason = null);
        void ClearPendingTarget(string reason = null);
    }
}
