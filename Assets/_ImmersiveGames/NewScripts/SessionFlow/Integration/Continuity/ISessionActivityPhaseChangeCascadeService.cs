using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity
{
    public interface ISessionActivityPhaseChangeCascadeService
    {
        Task<PhaseResetExecutionResult> RestartFromFirstPhaseAsync(string reason = null, CancellationToken ct = default);
        Task<PhaseResetExecutionResult> ResetCurrentPhaseAsync(string reason = null, CancellationToken ct = default);
        Task ExitToMenuAsync(string reason = null, CancellationToken ct = default);
        SessionActivityPhaseChangeCascadeResolution BeginPhaseChangeCascade(
            string operation,
            PhaseOrdinalNavigationKind navigationKind,
            PhaseNavigationDirection direction,
            PhaseDefinitionAsset targetPhaseRef,
            PhaseCatalogNavigationPlan navigationPlan,
            string reason = null,
            string source = null,
            SessionTransitionPlan plan = default,
            CancellationToken ct = default);
        void OpenPhaseChangeCascadeResultPresentation(
            SessionActivityPhaseChangeCascadeResolution resolution,
            PhaseCatalogNavigationPlan navigationPlan,
            SessionTransitionPlan plan,
            string source = null);
        void PublishPhaseChangePipelineHandoff(
            SessionActivityPhaseChangeCascadeResolution resolution,
            PhaseCatalogNavigationPlan navigationPlan,
            SessionTransitionPlan plan,
            string source = null,
            CancellationToken ct = default);
    }
}
