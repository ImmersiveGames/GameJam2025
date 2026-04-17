using ImmersiveGames.GameJam2025.Experience.PostRun.Contracts;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime;

namespace ImmersiveGames.GameJam2025.Orchestration.Navigation.Runtime
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

