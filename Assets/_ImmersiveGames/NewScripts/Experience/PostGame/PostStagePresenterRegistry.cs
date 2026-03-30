using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.PostGame
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostStagePresenterRegistry : IPostStagePresenterRegistry
    {
        private readonly object _sync = new();
        private readonly IPostStageControlService _controlService;
        private readonly IPostStagePresenterScopeResolver _scopeResolver;
        private IPostStagePresenter _currentPresenter;
        private string _currentSessionSignature = string.Empty;

        public PostStagePresenterRegistry(
            IPostStageControlService controlService,
            IPostStagePresenterScopeResolver scopeResolver)
        {
            _controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
            _scopeResolver = scopeResolver ?? throw new ArgumentNullException(nameof(scopeResolver));
        }

        public bool TryGetCurrentPresenter(out IPostStagePresenter presenter)
        {
            lock (_sync)
            {
                if (IsCurrentPresenterQueryableLocked())
                {
                    presenter = _currentPresenter;
                    return true;
                }

                presenter = null;
                return false;
            }
        }

        public bool TryEnsureCurrentPresenter(PostStageContext context, string source, out IPostStagePresenter presenter)
        {
            presenter = null;

            if (!_scopeResolver.TryResolvePresenters(context, out IReadOnlyList<IPostStagePresenter> presenters) || presenters.Count == 0)
            {
                return false;
            }

            if (presenters.Count > 1)
            {
                HardFailFastH1.Trigger(typeof(PostStagePresenterRegistry),
                    $"[FATAL][H1][PostGame] Multiple IPostStagePresenter components found. source='{source}' signature='{Normalize(context.Signature)}' scene='{Normalize(context.SceneName)}' presenters='{DescribePresenters(presenters)}'.");
                return false;
            }

            IPostStagePresenter candidate = presenters[0];
            if (candidate == null)
            {
                return false;
            }

            candidate.BindToSession(context, _controlService);
            if (!string.Equals(candidate.PresenterSignature, Normalize(context.Signature), StringComparison.Ordinal))
            {
                HardFailFastH1.Trigger(typeof(PostStagePresenterRegistry),
                    $"[FATAL][H1][PostGame] IPostStagePresenter bound with mismatched signature. source='{source}' expected='{Normalize(context.Signature)}' actual='{Normalize(candidate.PresenterSignature)}' scene='{Normalize(context.SceneName)}' presenter='{candidate.GetType().FullName}'.");
                return false;
            }

            if (!candidate.IsReady)
            {
                HardFailFastH1.Trigger(typeof(PostStagePresenterRegistry),
                    $"[FATAL][H1][PostGame] IPostStagePresenter bound but not ready. source='{source}' signature='{Normalize(context.Signature)}' scene='{Normalize(context.SceneName)}' presenter='{candidate.GetType().FullName}'.");
                return false;
            }

            lock (_sync)
            {
                _currentPresenter = candidate;
                _currentSessionSignature = Normalize(context.Signature);
                presenter = candidate;
            }

            DebugUtility.Log<PostStagePresenterRegistry>(
                $"[OBS][PostGame] PostStagePresenterAdopted source='{source}' signature='{Normalize(context.Signature)}' scene='{Normalize(context.SceneName)}' presenter='{DescribePresenter(candidate)}'.",
                DebugUtility.Colors.Info);

            return true;
        }

        public void Register(IPostStagePresenter presenter, string sessionSignature)
        {
            if (presenter == null)
            {
                return;
            }

            lock (_sync)
            {
                if (_currentPresenter != null && !ReferenceEquals(_currentPresenter, presenter))
                {
                    HardFailFastH1.Trigger(typeof(PostStagePresenterRegistry),
                        $"[FATAL][H1][PostGame] More than one IPostStagePresenter attempted to register. current='{_currentPresenter.GetType().FullName}' incoming='{presenter.GetType().FullName}' currentSignature='{_currentSessionSignature}' incomingSignature='{Normalize(sessionSignature)}'.");
                    return;
                }

                _currentPresenter = presenter;
                _currentSessionSignature = Normalize(sessionSignature);
            }
        }

        public void Unregister(IPostStagePresenter presenter)
        {
            if (presenter == null)
            {
                return;
            }

            lock (_sync)
            {
                if (ReferenceEquals(_currentPresenter, presenter))
                {
                    _currentPresenter = null;
                    _currentSessionSignature = string.Empty;
                }
            }
        }

        private bool IsCurrentPresenterQueryableLocked()
        {
            return _currentPresenter != null &&
                   _currentPresenter.IsReady &&
                   !string.IsNullOrWhiteSpace(_currentSessionSignature) &&
                   string.Equals(_currentPresenter.PresenterSignature, _currentSessionSignature, StringComparison.Ordinal);
        }

        private static string DescribePresenters(IReadOnlyList<IPostStagePresenter> presenters)
        {
            var types = new List<string>(presenters.Count);
            for (int i = 0; i < presenters.Count; i++)
            {
                if (presenters[i] != null)
                {
                    types.Add(presenters[i].GetType().FullName);
                }
            }

            return string.Join(",", types);
        }

        private static string DescribePresenter(IPostStagePresenter presenter)
        {
            if (presenter is UnityEngine.MonoBehaviour monoBehaviour)
            {
                return $"{monoBehaviour.GetType().Name}('{monoBehaviour.name}')";
            }

            return presenter.GetType().FullName ?? presenter.GetType().Name;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
