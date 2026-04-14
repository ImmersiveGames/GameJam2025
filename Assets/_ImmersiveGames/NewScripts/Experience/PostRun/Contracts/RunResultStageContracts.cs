using _ImmersiveGames.NewScripts.Core.Events;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Contracts
{
    public enum RunResult
    {
        Unknown = 0,
        Victory = 1,
        Defeat = 2,
        Exit = 3,
    }

    public enum RunResultStageCompletionKind
    {
        Unknown = 0,
        Continue = 1,
    }

    public readonly struct RunResultStage
    {
        public RunResultStage(RunContinuationContext continuationContext)
        {
            ContinuationContext = continuationContext;
        }

        public RunContinuationContext ContinuationContext { get; }
        public RunEndIntent Intent => ContinuationContext.Intent;
        public RunResult Result => ContinuationContext.Result;
        public string Signature => Intent.Signature;
        public string SceneName => Intent.SceneName;
        public string Profile => Intent.Profile;
        public int Frame => Intent.Frame;
        public string Reason => Intent.Reason;
        public bool IsGameplayScene => Intent.IsGameplayScene;
        public bool HasRunResultStage => ContinuationContext.HasRunResultStage;
    }

    public readonly struct RunResultStageCompletion
    {
        public RunResultStageCompletion(RunResultStageCompletionKind kind, string reason)
        {
            Kind = kind;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public RunResultStageCompletionKind Kind { get; }
        public string Reason { get; }
        public bool WasContinued => Kind == RunResultStageCompletionKind.Continue;
    }

    /// <summary>
    /// Handoff estreito da saida local da phase para a continuidade macro.
    /// </summary>
    public readonly struct RunResultStageToRunDecisionHandoff
    {
        public RunResultStageToRunDecisionHandoff(RunResultStage stage, RunResultStageCompletion completion, string source)
        {
            Stage = stage;
            Completion = completion;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public RunResultStage Stage { get; }
        public RunResultStageCompletion Completion { get; }
        public string Source { get; }
        public RunContinuationContext ContinuationContext => Stage.ContinuationContext;
        public bool IsValid =>
            Stage.ContinuationContext.IsValid &&
            Completion.Kind != RunResultStageCompletionKind.Unknown &&
            !string.IsNullOrWhiteSpace(Source);
    }

    public interface IRunResultStageControl
    {
        bool IsActive { get; }
        bool HasCompleted { get; }
        RunResultStage CurrentStage { get; }
        bool TryComplete(string reason = null);
    }

    public readonly struct RunResultStageEnteredEvent : IEvent
    {
        public RunResultStageEnteredEvent(RunResultStage stage)
        {
            Stage = stage;
        }

        public RunResultStage Stage { get; }
    }

    public readonly struct RunResultStageCompletedEvent : IEvent
    {
        public RunResultStageCompletedEvent(RunResultStage stage, RunResultStageCompletion completion)
        {
            Stage = stage;
            Completion = completion;
        }

        public RunResultStage Stage { get; }
        public RunResultStageCompletion Completion { get; }
    }
}
