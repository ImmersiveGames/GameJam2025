using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Core.Events;

namespace ImmersiveGames.GameJam2025.Experience.PostRun.Contracts
{
    public readonly struct RunDecision
    {
        public RunDecision(RunContinuationContext continuationContext)
        {
            ContinuationContext = continuationContext;
        }

        public RunContinuationContext ContinuationContext { get; }
        public RunEndIntent Intent => ContinuationContext.Intent;
        public RunResult Result => ContinuationContext.Result;
        public IReadOnlyList<RunContinuationKind> AllowedContinuations => ContinuationContext.AllowedContinuations;
        public bool RequiresPlayerDecision => ContinuationContext.RequiresPlayerDecision;
        public bool HasAllowedContinuations => ContinuationContext.HasAllowedContinuations;
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
        Menu = 1,
        Macro = 2,
    }

    public readonly struct RunRestart
    {
        public RunRestart(string reason, string source)
        {
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
        }

        public string Reason { get; }
        public string Source { get; }
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

