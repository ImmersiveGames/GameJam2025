using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Host.PostRun.Presentation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Ownership
{
    public interface IRunResultStageOwnershipService
    {
        bool IsActive { get; }
        bool HasCompleted { get; }
        Contracts.RunResultStage CurrentStage { get; }
        void EnterRunResultStage(RunContinuationContext continuationContext);
        void CompleteRunResultStage(RunResultStageCompletion completion);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunResultStageOwnershipService : IRunResultStageOwnershipService, IRunResultStageControl
    {
        private readonly IRunDecisionOwnershipService _runDecisionOwnershipService;
        private readonly IRunResultStagePresenterHost _presenterHost;
        private IRunResultStagePresenter _currentPresenter;

        public RunResultStageOwnershipService(
            IRunDecisionOwnershipService runDecisionOwnershipService,
            IRunResultStagePresenterHost presenterHost)
        {
            _runDecisionOwnershipService = runDecisionOwnershipService ?? throw new ArgumentNullException(nameof(runDecisionOwnershipService));
            _presenterHost = presenterHost ?? throw new ArgumentNullException(nameof(presenterHost));
        }

        public bool IsActive { get; private set; }
        public bool HasCompleted { get; private set; }
        public Contracts.RunResultStage CurrentStage { get; private set; }

        public void EnterRunResultStage(RunContinuationContext continuationContext)
        {
            if (IsActive)
            {
                return;
            }

            if (!continuationContext.IsValid)
            {
                HardFailFastH1.Trigger(typeof(RunResultStageOwnershipService),
                    "[FATAL][H1][RunResultStage] RunContinuationContext invalido recebido pelo stage owner.");
            }

            CurrentStage = new Contracts.RunResultStage(continuationContext);

            if (!_presenterHost.TryEnsureCurrentPresenter(CurrentStage, this, nameof(RunResultStageOwnershipService), out IRunResultStagePresenter presenter) ||
                presenter == null)
            {
                var handoff = new RunResultStageToRunDecisionHandoff(
                    continuationContext,
                    new RunResultStageCompletion(RunResultStageCompletionKind.Continue, "no_content"),
                    RunLocalExitDisposition.SkippedLocalExitNoContent,
                    nameof(RunResultStageOwnershipService),
                    CurrentStage);

                DebugUtility.Log<RunResultStageOwnershipService>(
                    $"[OBS][GameplaySessionFlow][RunResultStage] RunResultStageSkipped reason='no_content' disposition='skipped_local_exit_no_content' signature='{Normalize(CurrentStage.Signature)}' scene='{Normalize(CurrentStage.SceneName)}' result='{CurrentStage.Result}' reasonText='{Normalize(CurrentStage.Reason)}'.",
                    DebugUtility.Colors.Info);

                CurrentStage = default;
                IsActive = false;
                HasCompleted = true;
                DebugUtility.Log<RunResultStageOwnershipService>(
                    $"[OBS][GameplaySessionFlow][RunResultStage] RunResultStageToRunDecisionHandoffIssued disposition='skipped_local_exit_no_content' signature='{Normalize(handoff.ContinuationContext.Signature)}' scene='{Normalize(handoff.ContinuationContext.SceneName)}' frame={handoff.ContinuationContext.Frame} result='{handoff.ContinuationContext.Result}' source='{handoff.Source}'.",
                    DebugUtility.Colors.Info);

                _runDecisionOwnershipService.EnterRunDecision(handoff);
                return;
            }

            _currentPresenter = presenter;
            IsActive = true;
            HasCompleted = false;

            DebugUtility.Log<RunResultStageOwnershipService>(
                $"[OBS][GameplaySessionFlow][RunResultStage] RunResultStageEntered signature='{CurrentStage.Signature}' scene='{CurrentStage.SceneName}' frame={CurrentStage.Frame} result='{CurrentStage.Result}' reason='{CurrentStage.Reason}'.",
                DebugUtility.Colors.Info);

            EventBus<RunResultStageEnteredEvent>.Raise(new RunResultStageEnteredEvent(CurrentStage));
        }

        public bool TryComplete(string reason = null)
        {
            return TryFinish(new RunResultStageCompletion(RunResultStageCompletionKind.Continue, reason), "direct-complete");
        }

        public void CompleteRunResultStage(RunResultStageCompletion completion)
        {
            TryFinish(completion, "legacy-completion");
        }

        private bool TryFinish(RunResultStageCompletion completion, string source)
        {
            if (!IsActive || HasCompleted)
            {
                return false;
            }

            IsActive = false;
            HasCompleted = true;

            if (_currentPresenter != null)
            {
                _presenterHost.TryDetachCurrentPresenter(completion.Reason);
                _currentPresenter = null;
            }

            DebugUtility.Log<RunResultStageOwnershipService>(
                $"[OBS][GameplaySessionFlow][RunResultStage] RunResultStageCompleted signature='{CurrentStage.Signature}' scene='{CurrentStage.SceneName}' frame='{CurrentStage.Frame}' result='{CurrentStage.Result}' reason='{completion.Reason}' kind='{completion.Kind}' source='{source}'.",
                DebugUtility.Colors.Info);

            EventBus<RunResultStageCompletedEvent>.Raise(new RunResultStageCompletedEvent(CurrentStage, completion));

            var handoff = new RunResultStageToRunDecisionHandoff(
                CurrentStage.ContinuationContext,
                completion,
                RunLocalExitDisposition.MaterializedLocalExit,
                source,
                CurrentStage);

            DebugUtility.Log<RunResultStageOwnershipService>(
                $"[OBS][GameplaySessionFlow][RunResultStage] RunResultStageToRunDecisionHandoffIssued disposition='materialized_local_exit' signature='{CurrentStage.Signature}' scene='{CurrentStage.SceneName}' frame='{CurrentStage.Frame}' result='{CurrentStage.Result}' reason='{completion.Reason}' kind='{completion.Kind}' source='{handoff.Source}'.",
                DebugUtility.Colors.Info);

            _runDecisionOwnershipService.EnterRunDecision(handoff);
            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

