using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity
{
    public readonly struct GameplayRunResetRequest
    {
        public GameplayRunResetRequest(
            RunContinuationSelection selection,
            PhaseDefinitionAsset targetPhaseRef,
            string reason)
        {
            Selection = selection;
            TargetPhaseRef = targetPhaseRef;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public RunContinuationSelection Selection { get; }
        public RunContinuationKind Kind => Selection.SelectedContinuation;
        public PhaseDefinitionAsset TargetPhaseRef { get; }
        public string Reason { get; }
        public bool IsValid =>
            Selection.IsValid &&
            (Kind == RunContinuationKind.ResetRun || Kind == RunContinuationKind.Retry) &&
            TargetPhaseRef != null &&
            TargetPhaseRef.PhaseId.IsValid &&
            !string.IsNullOrWhiteSpace(Reason);
    }
}

