#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime
{
    public sealed class IntroStagePresenterHost : IIntroStagePresenterRegistry, IDisposable
    {
        private readonly object _sync = new();
        private readonly IIntroStagePresenterScopeResolver _presenterScopeResolver;
        private readonly EventBinding<IntroStageCompletedEvent> _introStageCompletedBinding;

        private GameObject? _currentPresenterInstance;
        private IIntroStagePresenter? _currentPresenter;
        private bool _currentPresenterOwnedByHost;
        private string _currentSessionSignature = string.Empty;
        private bool _disposed;

        public IntroStagePresenterHost()
            : this(ResolvePresenterScopeResolverOrFail())
        {
        }

        public IntroStagePresenterHost(IIntroStagePresenterScopeResolver presenterScopeResolver)
        {
            _presenterScopeResolver = presenterScopeResolver ?? throw new ArgumentNullException(nameof(presenterScopeResolver));
            _introStageCompletedBinding = new EventBinding<IntroStageCompletedEvent>(OnIntroStageCompleted);
            EventBus<IntroStageCompletedEvent>.Register(_introStageCompletedBinding);

            DebugUtility.LogVerbose<IntroStagePresenterHost>(
                "[OBS][IntroStage] IntroStagePresenterHost registrado (IntroStage presenter lifecycle bridge).",
                DebugUtility.Colors.Info);
        }

        public bool TryGetCurrentPresenter(out IIntroStagePresenter presenter)
        {
            lock (_sync)
            {
                if (IsCurrentPresenterQueryable(_currentSessionSignature))
                {
                    presenter = _currentPresenter!;
                    return true;
                }

                presenter = null!;
                return false;
            }
        }

        public bool TryEnsureCurrentPresenter(IntroStageSession session, string source, out IIntroStagePresenter presenter)
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
                    DestroyCurrentPresenterLocked("session_no_intro");
                    return false;
                }

                if (IsCurrentPresenterQueryable(session.SessionSignature))
                {
                    presenter = _currentPresenter!;
                    return true;
                }

                DestroyCurrentPresenterLocked("session_changed");
                if (TryResolveSceneLocalPresenterLocked(session, source, out presenter))
                {
                    return true;
                }

                if (session.IntroPresenterPrefab == null)
                {
                    LogNoContentSkip(session, source, "missing_presenter_prefab");
                    return false;
                }

                if (!TryCreatePresenterInstanceLocked(session, source, out presenter))
                {
                    return false;
                }

                if (!IsCurrentPresenterQueryable(session.SessionSignature))
                {
                    HardFailFastH1.Trigger(typeof(IntroStagePresenterHost),
                        $"[FATAL][H1][IntroStage] Intro presenter contract not queryable after adoption/bind. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.SessionSignature}' registered='{(_currentPresenter != null).ToString().ToLowerInvariant()}' queryable='{(_currentPresenter != null && _currentPresenter.CanServe(session.SessionSignature)).ToString().ToLowerInvariant()}' presenterSignature='{Normalize(_currentPresenter?.PresenterSignature ?? string.Empty)}' currentSignature='{_currentSessionSignature}'.");
                    return false;
                }

                presenter = _currentPresenter!;
                return true;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<IntroStageCompletedEvent>.Unregister(_introStageCompletedBinding);
            DestroyCurrentPresenter("dispose");
        }

        private bool IsCurrentPresenterQueryable(string sessionSignature)
        {
            return _currentPresenterInstance != null &&
                   _currentPresenter != null &&
                   _currentPresenter.IsPresentationAttached &&
                   _currentPresenter.CanServe(sessionSignature) &&
                   string.Equals(_currentPresenter.PresenterSignature, sessionSignature, StringComparison.Ordinal) &&
                   string.Equals(_currentSessionSignature, sessionSignature, StringComparison.Ordinal);
        }

        private bool TryResolveSceneLocalPresenterLocked(IntroStageSession session, string source, out IIntroStagePresenter presenter)
        {
            presenter = null!;

            if (_presenterScopeResolver.TryResolvePresenters(session, out IReadOnlyList<IIntroStagePresenter> scopedPresenters))
            {
                if (scopedPresenters.Count > 1)
                {
                    HardFailFastH1.Trigger(typeof(IntroStagePresenterHost),
                        $"[FATAL][H1][IntroStage] Multiple IIntroStagePresenter components found in local content. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.SessionSignature}' contentId='{session.LocalContentId}' presenters='{DescribePresenters(scopedPresenters)}'.");
                    return false;
                }

                if (scopedPresenters.Count == 1 && scopedPresenters[0] != null)
                {
                    IIntroStagePresenter existingPresenter = scopedPresenters[0];
                    AdoptPresenterLocked(existingPresenter, session, source, ownedByHost: false);

                    if (!IsCurrentPresenterQueryable(session.SessionSignature))
                    {
                        HardFailFastH1.Trigger(typeof(IntroStagePresenterHost),
                        $"[FATAL][H1][IntroStage] Intro presenter from local content bound but not queryable. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.SessionSignature}'.");
                        return false;
                    }

                    presenter = _currentPresenter!;
                    return true;
                }
            }

            return false;
        }

        private bool TryCreatePresenterInstanceLocked(IntroStageSession session, string source, out IIntroStagePresenter presenter)
        {
            presenter = null!;

            if (session.IntroPresenterPrefab == null)
            {
                LogNoContentSkip(session, source, "missing_presenter_prefab");
                ClearCurrentPresenterLocked(destroyOwnedInstance: false, reason: "missing_presenter_prefab");
                return false;
            }

            GameObject? prefab = session.IntroPresenterPrefab;
            if (prefab == null)
            {
                LogNoContentSkip(session, source, "missing_presenter_prefab");
                ClearCurrentPresenterLocked(destroyOwnedInstance: false, reason: "missing_presenter_prefab");
                return false;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.MoveGameObjectToScene(instance, activeScene);

            if (!TryFindPresentersOnObject(instance, out List<IIntroStagePresenter> presenters))
            {
                UnityEngine.Object.Destroy(instance);
                ClearCurrentPresenterLocked(destroyOwnedInstance: false, reason: "prefab_missing_presenter");
                LogNoContentSkip(session, source, "prefab_missing_presenter");
                return false;
            }

            if (presenters.Count > 1)
            {
                UnityEngine.Object.Destroy(instance);
                ClearCurrentPresenterLocked(destroyOwnedInstance: false, reason: "prefab_multiple_presenters");
                HardFailFastH1.Trigger(typeof(IntroStagePresenterHost),
                    $"[FATAL][H1][IntroStage] Intro presenter prefab exposes multiple IIntroStagePresenter components. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.SessionSignature}' presenters='{DescribePresenters(presenters)}'.");
                return false;
            }

            IIntroStagePresenter createdPresenter = presenters[0];
            if (createdPresenter is not MonoBehaviour presenterComponent)
            {
                UnityEngine.Object.Destroy(instance);
                ClearCurrentPresenterLocked(destroyOwnedInstance: false, reason: "prefab_non_monobehaviour");
                HardFailFastH1.Trigger(typeof(IntroStagePresenterHost),
                    $"[FATAL][H1][IntroStage] Intro presenter prefab does not derive from MonoBehaviour. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.SessionSignature}' presenterType='{createdPresenter.GetType().FullName}'.");
                return false;
            }

            AdoptPresenterLocked(createdPresenter, session, source, ownedByHost: true, presenterComponent.gameObject);

            if (!IsCurrentPresenterQueryable(session.SessionSignature))
            {
                HardFailFastH1.Trigger(typeof(IntroStagePresenterHost),
                    $"[FATAL][H1][IntroStage] Intro presenter prefab bound but not queryable. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.SessionSignature}' presenterType='{createdPresenter.GetType().Name}'.");
                return false;
            }

            DebugUtility.Log<IntroStagePresenterHost>(
                $"[OBS][IntroStage] IntroStagePresenterSpawned source='{source}' contentName='{DescribeSessionContentName(session)}' contentId='{session.LocalContentId}' signature='{session.SessionSignature}' presenterType='{createdPresenter.GetType().Name}' scope='scene_local'.",
                DebugUtility.Colors.Info);

            presenter = _currentPresenter!;
            return true;
        }

        private void DestroyCurrentPresenter(string reason)
        {
            lock (_sync)
            {
                DestroyCurrentPresenterLocked(reason);
            }
        }

        private void DestroyCurrentPresenterLocked(string reason)
        {
            ClearCurrentPresenterLocked(destroyOwnedInstance: true, reason: reason);
        }

        private void ClearCurrentPresenterLocked(bool destroyOwnedInstance, string reason)
        {
            DetachCurrentPresenterLocked(reason);

            if (destroyOwnedInstance && _currentPresenterInstance != null && _currentPresenterOwnedByHost)
            {
                UnityEngine.Object.Destroy(_currentPresenterInstance);
            }

            _currentPresenterInstance = null;
            _currentPresenter = null;
            _currentPresenterOwnedByHost = false;
            _currentSessionSignature = string.Empty;
        }

        private static void LogNoContentSkip(IntroStageSession session, string source, string reason)
        {
            DebugUtility.Log<IntroStagePresenterHost>(
                $"[OBS][IntroStage] IntroStageSkipped contentName='{DescribeSessionContentName(session)}' reason='no_content' source='{source}' detail='{reason}' signature='{session.SessionSignature}'.",
                DebugUtility.Colors.Info);
        }

        private static bool TryFindPresentersOnObject(GameObject root, out List<IIntroStagePresenter> presenters)
        {
            presenters = new List<IIntroStagePresenter>();

            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is IIntroStagePresenter candidate)
                {
                    presenters.Add(candidate);
                }
            }

            return presenters.Count > 0;
        }

        private void AdoptPresenterLocked(IIntroStagePresenter presenter, IntroStageSession session, string source, bool ownedByHost, GameObject? presenterInstanceOverride = null)
        {
            if (presenter is not MonoBehaviour presenterComponent)
            {
                HardFailFastH1.Trigger(typeof(IntroStagePresenterHost),
                    $"[FATAL][H1][IntroStage] Intro presenter does not derive from MonoBehaviour. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.SessionSignature}' presenterType='{presenter.GetType().FullName}'.");
                return;
            }

            IntroStagePresentationContract contract = BuildPresentationContract(session);
            presenter.AttachPresentation(contract);

            _currentPresenterInstance = presenterInstanceOverride ?? presenterComponent.gameObject;
            _currentPresenterOwnedByHost = ownedByHost;
            _currentSessionSignature = session.SessionSignature;
            _currentPresenter = presenter;

            DebugUtility.Log<IntroStagePresenterHost>(
                $"[OBS][IntroStage] IntroStagePresenterRegistered source='{source}' contentName='{DescribeSessionContentName(session)}' contentId='{session.LocalContentId}' signature='{session.SessionSignature}' presenterType='{presenter.GetType().Name}' scope='{(ownedByHost ? "host_owned" : "scene_local")}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<IntroStagePresenterHost>(
                $"[OBS][IntroStage] IntroStagePresenterAdopted source='{source}' contentName='{DescribeSessionContentName(session)}' contentId='{session.LocalContentId}' signature='{session.SessionSignature}' presenterType='{presenter.GetType().Name}' scope='{(ownedByHost ? "host_owned" : "scene_local")}'.",
                DebugUtility.Colors.Info);
        }

        private void OnIntroStageCompleted(IntroStageCompletedEvent evt)
        {
            if (!evt.Session.IsValid)
            {
                return;
            }

            lock (_sync)
            {
                if (!string.Equals(_currentSessionSignature, evt.Session.SessionSignature, StringComparison.Ordinal) ||
                    _currentPresenter == null)
                {
                    return;
                }

                string detachReason = Normalize(evt.Reason);
                string presenterType = _currentPresenter.GetType().Name;
                bool destroyOwnedInstance = _currentPresenterOwnedByHost;

                ClearCurrentPresenterLocked(destroyOwnedInstance, detachReason);

                DebugUtility.Log<IntroStagePresenterHost>(
                    $"[OBS][IntroStage] IntroStagePresenterDetached reason='{detachReason}' signature='{Normalize(evt.Session.SessionSignature)}' presenterType='{presenterType}'.",
                    DebugUtility.Colors.Info);
            }
        }

        private static IntroStagePresentationContract BuildPresentationContract(IntroStageSession session)
        {
            return new IntroStagePresentationContract(
                session.PhaseDefinitionRef,
                session.SessionSignature,
                session.SelectionVersion,
                session.LocalContentId,
                session.HasIntroStage,
                session.HasRunResultStage);
        }

        private void DetachCurrentPresenterLocked(string reason)
        {
            if (_currentPresenter == null)
            {
                return;
            }

            try
            {
                _currentPresenter.DetachPresentation(reason);
            }
            catch (Exception ex)
            {
                HardFailFastH1.Trigger(typeof(IntroStagePresenterHost),
                    $"[FATAL][H1][IntroStage] Presenter detach failed. reason='{reason}' presenter='{_currentPresenter.GetType().FullName}' ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static string DescribePresenters(IEnumerable<IIntroStagePresenter> presenters)
        {
            return string.Join(", ", presenters.Select(DescribePresenter));
        }

        private static string DescribePresenter(IIntroStagePresenter presenter)
        {
            if (presenter is MonoBehaviour monoBehaviour)
            {
                return $"{presenter.GetType().Name}@{monoBehaviour.gameObject.scene.name}";
            }

            return presenter.GetType().Name;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static string DescribeSessionContentName(IntroStageSession session)
        {
            if (session.PhaseDefinitionRef != null)
            {
                return session.PhaseDefinitionRef.name;
            }

            return "<none>";
        }

        private static IIntroStagePresenterScopeResolver ResolvePresenterScopeResolverOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStagePresenterScopeResolver>(out var resolver) && resolver != null)
            {
                return resolver;
            }

            throw new InvalidOperationException("[FATAL][Config][IntroStage] IIntroStagePresenterScopeResolver obrigatorio ausente.");
        }
    }
}
