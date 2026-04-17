using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Ownership
{
    public interface IRunContinuationOwnershipService
    {
        bool HasCurrentContext { get; }
        bool HasLastContext { get; }
        RunContinuationContext CurrentContext { get; }
        RunContinuationContext LastContext { get; }
        void AcceptTerminalFact(RunContinuationTerminalFact terminalFact);
        void ClearCurrentContext(string reason = null);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunContinuationOwnershipService : IRunContinuationOwnershipService
    {
        private static readonly IReadOnlyList<RunContinuationKind> DefaultAllowedContinuations =
            new[]
            {
                RunContinuationKind.AdvancePhase,
                RunContinuationKind.RestartCurrentPhase,
                RunContinuationKind.ResetRun,
                RunContinuationKind.Retry,
                RunContinuationKind.ExitToMenu,
                RunContinuationKind.TerminateRun,
            };

        public bool HasCurrentContext { get; private set; }
        public bool HasLastContext { get; private set; }
        public RunContinuationContext CurrentContext { get; private set; }
        public RunContinuationContext LastContext { get; private set; }

        public void AcceptTerminalFact(RunContinuationTerminalFact terminalFact)
        {
            if (!terminalFact.IsValid)
            {
                HardFailFastH1.Trigger(typeof(RunContinuationOwnershipService),
                    "[FATAL][H1][GameplaySessionFlow][RunContinuation] Terminal fact invalido recebido pelo owner canonico.");
            }

            if (HasCurrentContext)
            {
                if (string.Equals(CurrentContext.Signature, terminalFact.Intent.Signature, StringComparison.Ordinal))
                {
                    return;
                }

                HardFailFastH1.Trigger(typeof(RunContinuationOwnershipService),
                    $"[FATAL][H1][GameplaySessionFlow][RunContinuation] Terminal fact inesperado para assinatura diferente. current='{CurrentContext.Signature}' incoming='{terminalFact.Intent.Signature}'.");
            }

            LastContext = CurrentContext;
            HasLastContext = LastContext.IsValid;

            CurrentContext = BuildContext(terminalFact);
            HasCurrentContext = true;

            DebugUtility.Log<RunContinuationOwnershipService>(
                $"[OBS][GameplaySessionFlow][RunContinuation] RunContinuationContextMaterialized owner='RunContinuationOwnershipService' signature='{CurrentContext.Signature}' scene='{CurrentContext.SceneName}' frame={CurrentContext.Frame} result='{CurrentContext.Result}' allowedContinuations='[{string.Join(",", CurrentContext.AllowedContinuations)}]' requiresDecision='{CurrentContext.RequiresPlayerDecision}' hasRunResultStage='{CurrentContext.HasRunResultStage}'.",
                DebugUtility.Colors.Info);

            EventBus<RunContinuationContextMaterializedEvent>.Raise(new RunContinuationContextMaterializedEvent(CurrentContext));
        }

        public void ClearCurrentContext(string reason = null)
        {
            if (!HasCurrentContext)
            {
                return;
            }

            DebugUtility.Log<RunContinuationOwnershipService>(
                $"[OBS][GameplaySessionFlow][RunContinuation] RunContinuationContextCleared owner='RunContinuationOwnershipService' reason='{Normalize(reason)}' signature='{CurrentContext.Signature}'.",
                DebugUtility.Colors.Info);

            CurrentContext = default;
            HasCurrentContext = false;
        }

        private static RunContinuationContext BuildContext(RunContinuationTerminalFact terminalFact)
        {
            bool requiresPlayerDecision = true;

            return new RunContinuationContext(
                terminalFact.Intent,
                terminalFact.Result,
                DefaultAllowedContinuations,
                requiresPlayerDecision,
                terminalFact.HasRunResultStage);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class RunContinuationContextMaterializedEvent : IEvent
    {
        public RunContinuationContextMaterializedEvent(RunContinuationContext context)
        {
            Context = context;
        }

        public RunContinuationContext Context { get; }
    }
}
