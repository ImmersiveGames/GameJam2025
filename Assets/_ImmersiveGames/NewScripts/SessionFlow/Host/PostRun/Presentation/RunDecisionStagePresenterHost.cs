using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.SessionFlow.Host.PostRun.Presentation
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunDecisionStagePresenterHost : IRunDecisionStagePresenterHost
    {
        private readonly object _sync = new();
        private IRunDecisionStagePresenter _currentPresenter;
        private string _currentPresenterSource = string.Empty;

        public bool HasPresenter
        {
            get
            {
                lock (_sync)
                {
                    return _currentPresenter != null;
                }
            }
        }

        public bool TryGetCurrentPresenter(out IRunDecisionStagePresenter presenter)
        {
            lock (_sync)
            {
                presenter = _currentPresenter;
                return presenter != null;
            }
        }

        public bool TryAdoptPresenter(IRunDecisionStagePresenter presenter, string source)
        {
            if (presenter == null)
            {
                return false;
            }

            lock (_sync)
            {
                if (_currentPresenter != null && !ReferenceEquals(_currentPresenter, presenter))
                {
                    HardFailFastH1.Trigger(typeof(RunDecisionStagePresenterHost),
                        $"[FATAL][H1][RunDecision] Duplicate presenter registration detected. source='{Normalize(source)}' current='{DescribePresenter(_currentPresenter)}' incoming='{DescribePresenter(presenter)}'.");
                    return false;
                }

                _currentPresenter = presenter;
                _currentPresenterSource = Normalize(source);
            }

            DebugUtility.Log<RunDecisionStagePresenterHost>(
                $"[OBS][RunDecision] RunDecisionPresenterRegistered source='{Normalize(source)}' presenter='{DescribePresenter(presenter)}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool TryReleasePresenter(IRunDecisionStagePresenter presenter, string reason)
        {
            if (presenter == null)
            {
                return false;
            }

            lock (_sync)
            {
                if (_currentPresenter == null)
                {
                    return false;
                }

                if (!ReferenceEquals(_currentPresenter, presenter))
                {
                    HardFailFastH1.Trigger(typeof(RunDecisionStagePresenterHost),
                        $"[FATAL][H1][RunDecision] Presenter release mismatch. reason='{Normalize(reason)}' current='{DescribePresenter(_currentPresenter)}' incoming='{DescribePresenter(presenter)}'.");
                    return false;
                }

                _currentPresenter = null;
                _currentPresenterSource = string.Empty;
            }

            DebugUtility.Log<RunDecisionStagePresenterHost>(
                $"[OBS][RunDecision] RunDecisionPresenterReleased reason='{Normalize(reason)}' presenter='{DescribePresenter(presenter)}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private static string DescribePresenter(IRunDecisionStagePresenter presenter)
        {
            return presenter == null ? "<null>" : "RunDecisionStagePresenter";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

