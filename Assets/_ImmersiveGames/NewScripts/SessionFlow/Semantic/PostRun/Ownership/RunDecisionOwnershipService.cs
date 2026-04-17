using System;
using ImmersiveGames.GameJam2025.Core.Events;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Experience.PostRun.Contracts;
using ImmersiveGames.GameJam2025.Experience.PostRun.Presentation;
//using NewRunDecisionCompletedEvent = ImmersiveGames.GameJam2025.Experience.PostRun.Contracts.RunDecisionCompletedEvent;
//using NewRunDecisionEnteredEvent = ImmersiveGames.GameJam2025.Experience.PostRun.Contracts.RunDecisionEnteredEvent;

namespace ImmersiveGames.GameJam2025.Experience.PostRun.Ownership
{
    public interface IRunDecisionOwnershipService
    {
        bool IsActive { get; }
        bool HasCompleted { get; }
        RunDecision CurrentDecision { get; }
        void EnterRunDecision(RunResultStageToRunDecisionHandoff handoff);
        void ExitRunDecision(RunDecisionCompletion completion, RunContinuationKind selectedContinuation);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunDecisionOwnershipService : IRunDecisionOwnershipService
    {
        private readonly IRunDecisionStagePresenterHost _presenterHost;
        private IRunDecisionStagePresenter _currentPresenter;

        public RunDecisionOwnershipService(IRunDecisionStagePresenterHost presenterHost)
        {
            _presenterHost = presenterHost ?? throw new ArgumentNullException(nameof(presenterHost));
        }

        public bool IsActive { get; private set; }
        public bool HasCompleted { get; private set; }
        public RunDecision CurrentDecision { get; private set; }

        public void EnterRunDecision(RunResultStageToRunDecisionHandoff handoff)
        {
            if (!handoff.IsValid)
            {
                HardFailFastH1.Trigger(typeof(RunDecisionOwnershipService),
                    "[FATAL][H1][RunDecision] Handoff de RunResultStage invalido recebido pelo owner canonico.");
            }

            EnterRunDecisionInternal(handoff);
        }

        private void EnterRunDecisionInternal(RunResultStageToRunDecisionHandoff handoff)
        {
            if (IsActive)
            {
                return;
            }

            RunContinuationContext continuationContext = handoff.ContinuationContext;
            if (!continuationContext.IsValid)
            {
                HardFailFastH1.Trigger(typeof(RunDecisionOwnershipService),
                    "[FATAL][H1][RunDecision] RunContinuationContext invalido recebido pelo owner canonico.");
            }

            var decision = new RunDecision(continuationContext);
            CurrentDecision = decision;

            AttachPresenterOrFail(decision);

            if (_currentPresenter == null)
            {
                CurrentDecision = default;
                IsActive = false;
                HasCompleted = false;
                return;
            }

            IsActive = true;
            HasCompleted = false;

            DebugUtility.Log<RunDecisionOwnershipService>(
                $"[OBS][GameplaySessionFlow][RunDecision] RunDecisionEntered signature='{decision.Signature}' scene='{decision.SceneName}' frame={decision.Frame} result='{decision.Result}' reason='{decision.Reason}' source='{Normalize(handoff.Source)}' exitDisposition='{handoff.ExitDisposition}'.",
                DebugUtility.Colors.Info);

            EventBus<RunDecisionEnteredEvent>.Raise(new RunDecisionEnteredEvent(decision));
        }

        public void ExitRunDecision(RunDecisionCompletion completion, RunContinuationKind selectedContinuation)
        {
            if (!IsActive)
            {
                return;
            }

            if (CurrentDecision.ContinuationContext.IsValid &&
                !CurrentDecision.ContinuationContext.HasContinuation(selectedContinuation))
            {
                HardFailFastH1.Trigger(typeof(RunDecisionOwnershipService),
                    $"[FATAL][H1][RunDecision] Continuation selecionada invalida. selected='{selectedContinuation}' signature='{CurrentDecision.Signature}'.");
            }

            IsActive = false;
            HasCompleted = true;

            DebugUtility.Log<RunDecisionOwnershipService>(
                $"[OBS][GameplaySessionFlow][RunDecision] RunDecisionCompleted signature='{CurrentDecision.Signature}' scene='{CurrentDecision.SceneName}' frame='{CurrentDecision.Frame}' result='{CurrentDecision.Result}' reason='{completion.Reason}' handoff='{completion.NextState}' kind='{completion.Kind}' selectedContinuation='{selectedContinuation}'.",
                DebugUtility.Colors.Info);

            EventBus<RunDecisionCompletedEvent>.Raise(new RunDecisionCompletedEvent(CurrentDecision, completion));

            var selection = new RunContinuationSelection(
                CurrentDecision.ContinuationContext,
                selectedContinuation,
                completion);

            EventBus<RunContinuationSelectionResolvedEvent>.Raise(new RunContinuationSelectionResolvedEvent(selection));

            if (_currentPresenter != null)
            {
                // O presenter macro vive em UIGlobalScene e deve permanecer adotado pelo host
                // entre ciclos. Aqui soltamos apenas o bind da decisão corrente.
                _currentPresenter.DetachFromRunDecision(completion.Reason);
                _currentPresenter = null;
            }
        }

        private void AttachPresenterOrFail(RunDecision decision)
        {
            if (!_presenterHost.TryGetCurrentPresenter(out var presenter) || presenter == null)
            {
                HardFailFastH1.Trigger(typeof(RunDecisionOwnershipService),
                    $"[FATAL][H1][RunDecision] Presenter obrigatorio ausente. signature='{Normalize(decision.Signature)}' scene='{Normalize(decision.SceneName)}'.");
                return;
            }

            _currentPresenter = presenter;
            presenter.BindToRunDecision(decision);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

