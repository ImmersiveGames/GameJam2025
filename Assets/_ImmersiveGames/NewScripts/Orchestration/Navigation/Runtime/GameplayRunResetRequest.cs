using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.Navigation.Runtime
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
