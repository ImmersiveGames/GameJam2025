using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Events;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Contracts
{
    public enum RunContinuationKind
    {
        Unknown = 0,
        AdvancePhase = 1,
        RestartCurrentPhase = 2,
        ExitToMenu = 3,
        TerminateRun = 4,
        ResetRun = 5,
        Retry = 6,
    }

    public readonly struct RunContinuationTerminalFact
    {
        public RunContinuationTerminalFact(RunEndIntent intent, RunResult result, bool hasRunResultStage)
        {
            Intent = intent;
            Result = result;
            HasRunResultStage = hasRunResultStage;
        }

        public RunEndIntent Intent { get; }
        public RunResult Result { get; }
        public bool HasRunResultStage { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Intent.Signature) &&
            !string.IsNullOrWhiteSpace(Intent.SceneName) &&
            Result != RunResult.Unknown;
    }

    public readonly struct RunContinuationSelection
    {
        public RunContinuationSelection(
            RunContinuationContext continuationContext,
            RunContinuationKind selectedContinuation,
            RunDecisionCompletion completion)
        {
            ContinuationContext = continuationContext;
            SelectedContinuation = selectedContinuation;
            Completion = completion;
        }

        public RunContinuationContext ContinuationContext { get; }
        public RunContinuationKind SelectedContinuation { get; }
        public RunDecisionCompletion Completion { get; }

        public string Reason => Completion.Reason;
        public string NextState => Completion.NextState;
        public bool IsConfirmed => true;
        public bool IsValid =>
            ContinuationContext.IsValid &&
            SelectedContinuation != RunContinuationKind.Unknown &&
            ContinuationContext.HasContinuation(SelectedContinuation);
    }

    public readonly struct RunContinuationContext
    {
        public RunContinuationContext(
            RunEndIntent intent,
            RunResult result,
            IReadOnlyList<RunContinuationKind> allowedContinuations,
            bool requiresPlayerDecision,
            bool hasRunResultStage)
        {
            Intent = intent;
            Result = result;
            AllowedContinuations = allowedContinuations ?? Array.Empty<RunContinuationKind>();
            RequiresPlayerDecision = requiresPlayerDecision;
            HasRunResultStage = hasRunResultStage;
        }

        public RunEndIntent Intent { get; }
        public RunResult Result { get; }
        public IReadOnlyList<RunContinuationKind> AllowedContinuations { get; }
        public bool RequiresPlayerDecision { get; }
        public bool HasRunResultStage { get; }

        public string Signature => Intent.Signature;
        public string SceneName => Intent.SceneName;
        public string Profile => Intent.Profile;
        public int Frame => Intent.Frame;
        public string Reason => Intent.Reason;
        public bool IsGameplayScene => Intent.IsGameplayScene;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Intent.Signature) &&
            !string.IsNullOrWhiteSpace(Intent.SceneName) &&
            Result != RunResult.Unknown &&
            AllowedContinuations != null &&
            AllowedContinuations.Count > 0;
        public bool HasAllowedContinuations => AllowedContinuations != null && AllowedContinuations.Count > 0;
        public bool HasContinuation(RunContinuationKind kind) => AllowedContinuations.Contains(kind);
    }

    public sealed class RunContinuationSelectionResolvedEvent : IEvent
    {
        public RunContinuationSelectionResolvedEvent(RunContinuationSelection selection)
        {
            Selection = selection;
        }

        public RunContinuationSelection Selection { get; }
    }
}
