#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime
{
    public sealed class LevelIntroStagePresenterHost : ILevelIntroStagePresenterRegistry, IDisposable
    {
        private readonly object _sync = new();
        private readonly ILevelIntroStagePresenterScopeResolver _presenterScopeResolver;

        private GameObject? _currentPresenterInstance;
        private ILevelIntroStagePresenter? _currentPresenter;
        private bool _currentPresenterOwnedByHost;
        private string _currentSessionSignature = string.Empty;

        public LevelIntroStagePresenterHost()
            : this(ResolvePresenterScopeResolverOrFail())
        {
        }

        public LevelIntroStagePresenterHost(ILevelIntroStagePresenterScopeResolver presenterScopeResolver)
        {
            _presenterScopeResolver = presenterScopeResolver ?? throw new ArgumentNullException(nameof(presenterScopeResolver));
            DebugUtility.LogVerbose<LevelIntroStagePresenterHost>(
                "[OBS][LevelFlow] LevelIntroStagePresenterHost registrado (EnterStage presenter lifecycle bridge).",
                DebugUtility.Colors.Info);
        }

        public bool TryGetCurrentPresenter(out ILevelIntroStagePresenter presenter)
        {
            lock (_sync)
            {
                if (_currentPresenter != null &&
                    _currentPresenter.IsReady &&
                    string.Equals(_currentPresenter.PresenterSignature, _currentSessionSignature, StringComparison.Ordinal))
                {
                    presenter = _currentPresenter;
                    return true;
                }

                presenter = null!;
                return false;
            }
        }

        public void Register(ILevelIntroStagePresenter presenter, string sessionSignature)
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

        public bool TryEnsureCurrentPresenter(LevelIntroStageSession session, string source, out ILevelIntroStagePresenter presenter)
        {
            presenter = null!;

            if (!session.IsValid)
            {
                return false;
            }

            lock (_sync)
            {
                if (!session.HasIntroStage)
                {
                    DestroyCurrentPresenterLocked();
                    return false;
                }

                if (IsCurrentPresenterQueryable(session.LevelSignature))
                {
                    presenter = _currentPresenter!;
                    return true;
                }

                DestroyCurrentPresenterLocked();
                CreatePresenterInstanceLocked(session, source);

                if (!IsCurrentPresenterQueryable(session.LevelSignature))
                {
                    HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                        $"[FATAL][H1][LevelFlow] Intro presenter contract not queryable after adoption/bind. source='{source}' levelRef='{session.LevelRef.name}' signature='{session.LevelSignature}' registered='{(_currentPresenter != null).ToString().ToLowerInvariant()}' ready='{(_currentPresenter != null && _currentPresenter.IsReady).ToString().ToLowerInvariant()}' presenterSignature='{Normalize(_currentPresenter?.PresenterSignature ?? string.Empty)}' currentSignature='{_currentSessionSignature}'.");
                    return false;
                }

                presenter = _currentPresenter!;
                return true;
            }
        }

        public void Unregister(ILevelIntroStagePresenter presenter)
        {
            if (presenter == null)
            {
                return;
            }

            lock (_sync)
            {
                if (ReferenceEquals(_currentPresenter, presenter))
                {
                    ClearCurrentPresenterLocked(destroyOwnedInstance: _currentPresenterOwnedByHost);
                }
            }
        }

        public void Dispose()
        {
            DestroyCurrentPresenter();
        }

        private bool IsCurrentPresenterQueryable(string sessionSignature)
        {
            return _currentPresenterInstance != null &&
                   _currentPresenter != null &&
                   _currentPresenter.IsReady &&
                   string.Equals(_currentPresenter.PresenterSignature, sessionSignature, StringComparison.Ordinal) &&
                   string.Equals(_currentSessionSignature, sessionSignature, StringComparison.Ordinal);
        }

        private void CreatePresenterInstanceLocked(LevelIntroStageSession session, string source)
        {
            if (_presenterScopeResolver.TryResolvePresenters(session, out IReadOnlyList<ILevelIntroStagePresenter> scopedPresenters))
            {
                if (scopedPresenters.Count > 1)
                {
                    HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                        $"[FATAL][H1][LevelFlow] Multiple ILevelIntroStagePresenter components found in level content. source='{source}' levelRef='{session.LevelRef.name}' signature='{session.LevelSignature}' contentId='{session.LocalContentId}' presenters='{DescribePresenters(scopedPresenters)}'.");
                    return;
                }

                if (scopedPresenters.Count == 1 && scopedPresenters[0] != null)
                {
                    ILevelIntroStagePresenter existingPresenter = scopedPresenters[0];
                    existingPresenter.BindToSession(session.LevelSignature);
                    AdoptPresenterLocked(existingPresenter, session, source, ownedByHost: false);

                    if (!IsCurrentPresenterQueryable(session.LevelSignature))
                    {
                        HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                            $"[FATAL][H1][LevelFlow] Intro presenter from level content bound but not queryable. source='{source}' levelRef='{session.LevelRef.name}' signature='{session.LevelSignature}'.");
                        return;
                    }

                    return;
                }
            }

            if (session.PresenterPrefab == null)
            {
                ClearCurrentPresenterLocked(destroyOwnedInstance: false);
                return;
            }

            GameObject? prefab = session.PresenterPrefab;
            if (prefab == null)
            {
                ClearCurrentPresenterLocked(destroyOwnedInstance: false);
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.MoveGameObjectToScene(instance, activeScene);

            if (!TryFindPresentersOnObject(instance, out List<ILevelIntroStagePresenter> presenters))
            {
                UnityEngine.Object.Destroy(instance);
                ClearCurrentPresenterLocked(destroyOwnedInstance: false);
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][LevelFlow] Intro presenter prefab does not expose ILevelIntroStagePresenter. source='{source}' levelRef='{session.LevelRef.name}' signature='{session.LevelSignature}' prefab='{prefab.name}'.");
                return;
            }

            if (presenters.Count > 1)
            {
                UnityEngine.Object.Destroy(instance);
                ClearCurrentPresenterLocked(destroyOwnedInstance: false);
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][LevelFlow] Intro presenter prefab exposes multiple ILevelIntroStagePresenter components. source='{source}' levelRef='{session.LevelRef.name}' signature='{session.LevelSignature}' prefab='{prefab.name}' presenters='{DescribePresenters(presenters)}'.");
                return;
            }

            ILevelIntroStagePresenter presenter = presenters[0];
            if (presenter is not MonoBehaviour presenterComponent)
            {
                UnityEngine.Object.Destroy(instance);
                ClearCurrentPresenterLocked(destroyOwnedInstance: false);
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][LevelFlow] Intro presenter prefab does not derive from MonoBehaviour. source='{source}' levelRef='{session.LevelRef.name}' signature='{session.LevelSignature}' prefab='{prefab.name}' presenterType='{presenter.GetType().FullName}'.");
                return;
            }

            presenter.BindToSession(session.LevelSignature);
            AdoptPresenterLocked(presenter, session, source, ownedByHost: true, presenterComponent.gameObject);

            if (!IsCurrentPresenterQueryable(session.LevelSignature))
            {
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][LevelFlow] Intro presenter prefab bound but not queryable. source='{source}' levelRef='{session.LevelRef.name}' signature='{session.LevelSignature}' prefab='{prefab.name}'.");
                return;
            }

            DebugUtility.Log<LevelIntroStagePresenterHost>(
                $"[OBS][LevelFlow] EnterStagePresenterSpawned source='{source}' levelRef='{session.LevelRef.name}' contentId='{session.LocalContentId}' signature='{session.LevelSignature}' presenter='{prefab.name}'.",
                DebugUtility.Colors.Info);
        }

        private void DestroyCurrentPresenter()
        {
            lock (_sync)
            {
                DestroyCurrentPresenterLocked();
            }
        }

        private void DestroyCurrentPresenterLocked()
        {
            ClearCurrentPresenterLocked(destroyOwnedInstance: true);
        }

        private void ClearCurrentPresenterLocked(bool destroyOwnedInstance)
        {
            if (destroyOwnedInstance && _currentPresenterInstance != null && _currentPresenterOwnedByHost)
            {
                UnityEngine.Object.Destroy(_currentPresenterInstance);
            }

            _currentPresenterInstance = null;
            _currentPresenter = null;
            _currentPresenterOwnedByHost = false;
            _currentSessionSignature = string.Empty;
        }

        private static bool TryFindPresentersOnObject(GameObject root, out List<ILevelIntroStagePresenter> presenters)
        {
            presenters = new List<ILevelIntroStagePresenter>();

            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is ILevelIntroStagePresenter candidate)
                {
                    presenters.Add(candidate);
                }
            }

            return presenters.Count > 0;
        }

        private void AdoptPresenterLocked(ILevelIntroStagePresenter presenter, LevelIntroStageSession session, string source, bool ownedByHost, GameObject? presenterInstanceOverride = null)
        {
            if (presenter is not MonoBehaviour presenterComponent)
            {
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][LevelFlow] Intro presenter does not derive from MonoBehaviour. source='{source}' levelRef='{session.LevelRef.name}' signature='{session.LevelSignature}' presenterType='{presenter.GetType().FullName}'.");
                return;
            }

            MonoBehaviour presenterBehaviour = presenterComponent;
            _currentPresenterInstance = presenterInstanceOverride ?? presenterComponent.gameObject;
            _currentPresenterOwnedByHost = ownedByHost;
            _currentSessionSignature = session.LevelSignature;
            _currentPresenter = presenter;

            DebugUtility.Log<LevelIntroStagePresenterHost>(
                $"[OBS][LevelFlow] IntroPresenterAdopted source='{source}' levelRef='{session.LevelRef.name}' contentId='{session.LocalContentId}' signature='{session.LevelSignature}' presenter='{presenterBehaviour.name}'.",
                DebugUtility.Colors.Info);
        }

        private static string DescribePresenters(IEnumerable<ILevelIntroStagePresenter> presenters)
            => string.Join(", ", presenters.Select(DescribePresenter));

        private static string DescribePresenter(ILevelIntroStagePresenter presenter)
        {
            if (presenter is MonoBehaviour monoBehaviour)
            {
                return $"{monoBehaviour.GetType().Name}('{monoBehaviour.name}')";
            }

            return presenter.GetType().FullName ?? presenter.GetType().Name;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static ILevelIntroStagePresenterScopeResolver ResolvePresenterScopeResolverOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelIntroStagePresenterScopeResolver>(out var resolver) && resolver != null)
            {
                return resolver;
            }

            throw new InvalidOperationException("[FATAL][Config][LevelFlow] ILevelIntroStagePresenterScopeResolver obrigatorio ausente.");
        }
    }
}
