using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.Orchestration.Navigation.Runtime
{
    public enum GameplayRunResetTargetPolicy
    {
        Unknown = 0,
        FirstCatalogPhase = 1,
        CurrentCatalogPhase = 2,
    }

    public readonly struct GameplayRunResetRequest
    {
        public GameplayRunResetRequest(RunContinuationSelection selection)
        {
            Selection = selection;
            Kind = selection.SelectedContinuation;
            TargetPolicy = Kind switch
            {
                RunContinuationKind.ResetRun => GameplayRunResetTargetPolicy.FirstCatalogPhase,
                RunContinuationKind.Retry => GameplayRunResetTargetPolicy.CurrentCatalogPhase,
                _ => GameplayRunResetTargetPolicy.Unknown,
            };
        }

        public RunContinuationSelection Selection { get; }
        public RunContinuationKind Kind { get; }
        public string Reason => Selection.Reason;
        public GameplayRunResetTargetPolicy TargetPolicy { get; }
        public bool IsValid =>
            Selection.IsValid &&
            (Kind == RunContinuationKind.ResetRun || Kind == RunContinuationKind.Retry) &&
            TargetPolicy != GameplayRunResetTargetPolicy.Unknown;
    }
}
