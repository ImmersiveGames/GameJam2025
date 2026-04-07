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
        public RunResultStage(RunEndIntent intent, RunResult result)
        {
            Intent = intent;
            Result = result;
        }

        public RunEndIntent Intent { get; }
        public RunResult Result { get; }
        public string Signature => Intent.Signature;
        public string SceneName => Intent.SceneName;
        public string Profile => Intent.Profile;
        public int Frame => Intent.Frame;
        public string Reason => Intent.Reason;
        public bool IsGameplayScene => Intent.IsGameplayScene;
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
