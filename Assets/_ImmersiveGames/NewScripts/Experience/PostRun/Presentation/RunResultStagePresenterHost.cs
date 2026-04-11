using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Presentation
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunResultStagePresenterHost : IRunResultStagePresenterHost
    {
        private readonly object _sync = new();
        private IRunResultStagePresenter _registeredPresenter;
        private string _registeredPresenterSource = string.Empty;
        private string _attachedStageSignature = string.Empty;
        private bool _isAttached;

        public bool HasPresenter
        {
            get
            {
                lock (_sync)
                {
                    return _registeredPresenter != null;
                }
            }
        }

        public bool TryGetCurrentPresenter(out IRunResultStagePresenter presenter)
        {
            lock (_sync)
            {
                if (_registeredPresenter == null)
                {
                    presenter = null;
                    return false;
                }

                presenter = _registeredPresenter;
                return true;
            }
        }

        public bool TryAdoptPresenter(IRunResultStagePresenter presenter, string source)
        {
            if (presenter == null)
            {
                return false;
            }

            lock (_sync)
            {
                if (_registeredPresenter != null && !ReferenceEquals(_registeredPresenter, presenter))
                {
                    HardFailFastH1.Trigger(typeof(RunResultStagePresenterHost),
                        $"[FATAL][H1][RunResultStage] Duplicate presenter registration detected. source='{Normalize(source)}' current='{DescribePresenter(_registeredPresenter)}' incoming='{DescribePresenter(presenter)}'.");
                    return false;
                }

                _registeredPresenter = presenter;
                _registeredPresenterSource = Normalize(source);
            }

            DebugUtility.Log<RunResultStagePresenterHost>(
                $"[OBS][RunResultStage] RunResultStagePresenterRegistered source='{Normalize(source)}' presenter='{DescribePresenter(presenter)}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool TryEnsureCurrentPresenter(RunResultStage stage, IRunResultStageControl control, string source, out IRunResultStagePresenter presenter)
        {
            presenter = null;

            if (!stage.IsGameplayScene)
            {
                return false;
            }

            if (control == null)
            {
                HardFailFastH1.Trigger(typeof(RunResultStagePresenterHost),
                    $"[FATAL][H1][RunResultStage] IRunResultStageControl obrigatorio ausente ao adotar presenter. source='{Normalize(source)}' signature='{Normalize(stage.Signature)}' scene='{Normalize(stage.SceneName)}'.");
                return false;
            }

            lock (_sync)
            {
                if (_registeredPresenter == null)
                {
                    DebugUtility.Log<RunResultStagePresenterHost>(
                        $"[OBS][RunResultStage] RunResultStageSkipped reason='no_content' source='{Normalize(source)}' signature='{Normalize(stage.Signature)}' scene='{Normalize(stage.SceneName)}' detail='presenter_not_registered'.",
                        DebugUtility.Colors.Info);
                    return false;
                }

                if (_isAttached && string.Equals(_attachedStageSignature, Normalize(stage.Signature), StringComparison.Ordinal))
                {
                    presenter = _registeredPresenter;
                    return true;
                }

                if (_isAttached)
                {
                    DetachCurrentPresenterLocked("stage_changed");
                }

                _registeredPresenter.AttachToRunResultStage(stage, control);

                if (!_registeredPresenter.IsReady ||
                    !string.Equals(Normalize(_registeredPresenter.PresenterSignature), Normalize(stage.Signature), StringComparison.Ordinal))
                {
                    HardFailFastH1.Trigger(typeof(RunResultStagePresenterHost),
                        $"[FATAL][H1][RunResultStage] RunResultStage presenter bound but not queryable. source='{Normalize(source)}' signature='{Normalize(stage.Signature)}' scene='{Normalize(stage.SceneName)}' presenter='{DescribePresenter(_registeredPresenter)}' presenterSignature='{Normalize(_registeredPresenter.PresenterSignature)}'.");
                    return false;
                }

                _attachedStageSignature = Normalize(stage.Signature);
                _isAttached = true;
                presenter = _registeredPresenter;
            }

            DebugUtility.Log<RunResultStagePresenterHost>(
                $"[OBS][RunResultStage] RunResultStagePresenterAdopted source='{Normalize(source)}' signature='{Normalize(stage.Signature)}' scene='{Normalize(stage.SceneName)}' presenter='{DescribePresenter(presenter)}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool TryDetachCurrentPresenter(string reason)
        {
            lock (_sync)
            {
                if (_registeredPresenter == null || !_isAttached)
                {
                    return false;
                }

                DetachCurrentPresenterLocked(reason);
            }

            DebugUtility.Log<RunResultStagePresenterHost>(
                $"[OBS][RunResultStage] RunResultStagePresenterDetached reason='{Normalize(reason)}' presenter='{DescribePresenter(_registeredPresenter)}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        public bool TryUnregisterPresenter(IRunResultStagePresenter presenter, string reason)
        {
            if (presenter == null)
            {
                return false;
            }

            lock (_sync)
            {
                if (_registeredPresenter == null)
                {
                    return false;
                }

                if (!ReferenceEquals(_registeredPresenter, presenter))
                {
                    HardFailFastH1.Trigger(typeof(RunResultStagePresenterHost),
                        $"[FATAL][H1][RunResultStage] Presenter unregister mismatch. reason='{Normalize(reason)}' current='{DescribePresenter(_registeredPresenter)}' incoming='{DescribePresenter(presenter)}'.");
                    return false;
                }

                DetachCurrentPresenterLocked(reason);
                _registeredPresenter = null;
                _registeredPresenterSource = string.Empty;
            }

            DebugUtility.Log<RunResultStagePresenterHost>(
                $"[OBS][RunResultStage] RunResultStagePresenterUnregistered reason='{Normalize(reason)}' presenter='{DescribePresenter(presenter)}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        private void DetachCurrentPresenterLocked(string reason)
        {
            if (_registeredPresenter == null)
            {
                return;
            }

            try
            {
                _registeredPresenter.DetachFromRunResultStage(reason);
            }
            catch (Exception ex)
            {
                HardFailFastH1.Trigger(typeof(RunResultStagePresenterHost),
                    $"[FATAL][H1][RunResultStage] Presenter detach failed. reason='{Normalize(reason)}' presenter='{_registeredPresenter.GetType().FullName}' ex='{ex.GetType().Name}: {ex.Message}'.");
            }

            _isAttached = false;
            _attachedStageSignature = string.Empty;
        }

        private static string DescribePresenter(IRunResultStagePresenter presenter)
        {
            if (presenter == null)
            {
                return "<null>";
            }

            return presenter.GetType().Name;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
