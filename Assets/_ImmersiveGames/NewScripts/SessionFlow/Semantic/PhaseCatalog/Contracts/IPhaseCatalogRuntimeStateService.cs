using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts
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
        void RegisterTraversalWrap(PhaseNavigationDirection direction, string reason = null);
        void ClearPendingTarget(string reason = null);
    }
}

