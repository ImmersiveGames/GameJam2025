using _ImmersiveGames.NewScripts.Core.Events;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Contracts
{
    public readonly struct RunDecision
    {
        public RunDecision(RunEndIntent intent, RunResult result)
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

    public enum RunDecisionCompletionKind
    {
        Unknown = 0,
        Restart = 1,
        Menu = 2,
        Macro = 3,
    }

    public readonly struct RunDecisionCompletion
    {
        public RunDecisionCompletion(RunDecisionCompletionKind kind, string reason, string nextState)
        {
            Kind = kind;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            NextState = string.IsNullOrWhiteSpace(nextState) ? string.Empty : nextState.Trim();
        }

        public RunDecisionCompletionKind Kind { get; }
        public string Reason { get; }
        public string NextState { get; }
    }

    public readonly struct RunDecisionEnteredEvent : IEvent
    {
        public RunDecisionEnteredEvent(RunDecision decision)
        {
            Decision = decision;
        }

        public RunDecision Decision { get; }
    }

    public readonly struct RunDecisionCompletedEvent : IEvent
    {
        public RunDecisionCompletedEvent(RunDecision decision, RunDecisionCompletion completion)
        {
            Decision = decision;
            Completion = completion;
        }

        public RunDecision Decision { get; }
        public RunDecisionCompletion Completion { get; }
    }
}
