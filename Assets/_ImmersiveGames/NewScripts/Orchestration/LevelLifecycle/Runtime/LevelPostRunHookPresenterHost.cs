#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Handoff;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public sealed class LevelPostRunHookPresenterHost : ILevelPostRunHookPresenterRegistry
    {
        private readonly object _sync = new();
        private readonly ILevelPostRunHookPresenterScopeResolver _presenterScopeResolver;
        private ILevelPostRunHookPresenter? _currentPresenter;
        private string _currentSessionSignature = string.Empty;

        public LevelPostRunHookPresenterHost()
            : this(ResolvePresenterScopeResolverOrFail())
        {
        }

        public LevelPostRunHookPresenterHost(ILevelPostRunHookPresenterScopeResolver presenterScopeResolver)
        {
            _presenterScopeResolver = presenterScopeResolver ?? throw new ArgumentNullException(nameof(presenterScopeResolver));
            DebugUtility.LogVerbose<LevelPostRunHookPresenterHost>(
                "[OBS][LevelFlow] LevelPostRunHookPresenterHost registrado (post-run level presenter lifecycle bridge).",
                DebugUtility.Colors.Info);
        }

        public bool TryGetCurrentPresenter(out ILevelPostRunHookPresenter presenter)
        {
            lock (_sync)
            {
                if (IsCurrentPresenterQueryableLocked())
                {
                    presenter = _currentPresenter!;
                    return true;
                }

                presenter = null!;
                return false;
            }
        }

        public void Register(ILevelPostRunHookPresenter presenter, string sessionSignature)
        {
            if (presenter == null)
            {
                return;
            }

            lock (_sync)
            {
                _currentSessionSignature = Normalize(sessionSignature);
                _currentPresenter = presenter;
            }
        }

        public bool TryEnsureCurrentPresenter(LevelPostRunHookContext context, string source, out ILevelPostRunHookPresenter presenter)
        {
            presenter = null!;

            if (context.LevelRef == null)
            {
                return false;
            }

            lock (_sync)
            {
                if (IsCurrentPresenterQueryable(context.LevelSignature))
                {
                    presenter = _currentPresenter!;
                    return true;
                }

                ClearCurrentPresenterLocked();

                if (!_presenterScopeResolver.TryResolvePresenters(context, out IReadOnlyList<ILevelPostRunHookPresenter> scopedPresenters) ||
                    scopedPresenters.Count == 0)
                {
                    return false;
                }

                if (scopedPresenters.Count > 1)
                {
                    HardFailFastH1.Trigger(typeof(LevelPostRunHookPresenterHost),
                        $"[FATAL][H1][LevelFlow] Multiple ILevelPostRunHookPresenter components found in level content. source='{source}' levelRef='{context.LevelRef.name}' signature='{Normalize(context.LevelSignature)}' presenters='{DescribePresenters(scopedPresenters)}'.");
                    return false;
                }

                ILevelPostRunHookPresenter candidate = scopedPresenters[0];
                if (candidate == null)
                {
                    return false;
                }

                candidate.BindToSession(context);
                if (!candidate.IsReady)
                {
                    HardFailFastH1.Trigger(typeof(LevelPostRunHookPresenterHost),
                        $"[FATAL][H1][LevelFlow] LevelPostRunHookPresenter bound but not ready. source='{source}' levelRef='{context.LevelRef.name}' signature='{Normalize(context.LevelSignature)}' presenter='{candidate.GetType().FullName}'.");
                    return false;
                }

                _currentPresenter = candidate;
                _currentSessionSignature = Normalize(context.LevelSignature);
                presenter = candidate;

                DebugUtility.Log<LevelPostRunHookPresenterHost>(
                    $"[OBS][LevelFlow] PostRunLevelPresenterAdopted source='{source}' levelRef='{context.LevelRef.name}' signature='{Normalize(context.LevelSignature)}' presenter='{DescribePresenter(candidate)}'.",
                    DebugUtility.Colors.Info);

                return true;
            }
        }

        public void Unregister(ILevelPostRunHookPresenter presenter)
        {
            if (presenter == null)
            {
                return;
            }

            lock (_sync)
            {
                if (ReferenceEquals(_currentPresenter, presenter))
                {
                    ClearCurrentPresenterLocked();
                }
            }
        }

        private bool IsCurrentPresenterQueryable(string sessionSignature)
        {
            return _currentPresenter != null &&
                   _currentPresenter.IsReady &&
                   string.Equals(_currentPresenter.PresenterSignature, sessionSignature, StringComparison.Ordinal) &&
                   string.Equals(_currentSessionSignature, sessionSignature, StringComparison.Ordinal);
        }

        private bool IsCurrentPresenterQueryableLocked()
        {
            return IsCurrentPresenterQueryable(_currentSessionSignature);
        }

        private void ClearCurrentPresenterLocked()
        {
            _currentPresenter = null;
            _currentSessionSignature = string.Empty;
        }

        private static string DescribePresenters(IEnumerable<ILevelPostRunHookPresenter> presenters)
            => string.Join(", ", presenters.Select(DescribePresenter));

        private static string DescribePresenter(ILevelPostRunHookPresenter presenter)
        {
            if (presenter is MonoBehaviour monoBehaviour)
            {
                return $"{monoBehaviour.GetType().Name}('{monoBehaviour.name}')";
            }

            return presenter.GetType().FullName ?? presenter.GetType().Name;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static ILevelPostRunHookPresenterScopeResolver ResolvePresenterScopeResolverOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelPostRunHookPresenterScopeResolver>(out var resolver) && resolver != null)
            {
                return resolver;
            }

            throw new InvalidOperationException("[FATAL][Config][LevelFlow] ILevelPostRunHookPresenterScopeResolver obrigatorio ausente.");
        }
    }
}
