using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;
using _ImmersiveGames.NewScripts.Experience.PostRun.Presentation;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Ownership
{
    public interface IRunResultStageOwnershipService
    {
        bool IsActive { get; }
        bool HasCompleted { get; }
        RunResultStage CurrentStage { get; }
        void EnterRunResultStage(RunResultStage stage);
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
        public RunResultStage CurrentStage { get; private set; }

        public void EnterRunResultStage(RunResultStage stage)
        {
            if (IsActive)
            {
                return;
            }

            CurrentStage = stage;

            if (!_presenterHost.TryEnsureCurrentPresenter(stage, this, nameof(RunResultStageOwnershipService), out IRunResultStagePresenter presenter) ||
                presenter == null)
            {
                HardFailFastH1.Trigger(typeof(RunResultStageOwnershipService),
                    $"[FATAL][H1][RunResultStage] Presenter obrigatorio ausente. signature='{Normalize(stage.Signature)}' scene='{Normalize(stage.SceneName)}'.");
                CurrentStage = default;
                IsActive = false;
                HasCompleted = false;
                return;
            }

            _currentPresenter = presenter;
            IsActive = true;
            HasCompleted = false;

            DebugUtility.Log<RunResultStageOwnershipService>(
                $"[OBS][GameplaySessionFlow][RunResultStage] RunResultStageEntered signature='{stage.Signature}' scene='{stage.SceneName}' frame={stage.Frame} result='{stage.Result}' reason='{stage.Reason}'.",
                DebugUtility.Colors.Info);

            EventBus<RunResultStageEnteredEvent>.Raise(new RunResultStageEnteredEvent(stage));
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

            _runDecisionOwnershipService.EnterRunDecision(new RunDecision(CurrentStage.Intent, CurrentStage.Result));
            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
