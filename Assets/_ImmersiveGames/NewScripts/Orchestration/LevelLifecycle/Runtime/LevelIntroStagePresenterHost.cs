#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public sealed class LevelIntroStagePresenterHost : ILevelIntroStagePresenterRegistry, IDisposable
    {
        private readonly object _sync = new();
        private readonly ILevelIntroStagePresenterScopeResolver _presenterScopeResolver;
        private readonly EventBinding<LevelIntroCompletedEvent> _levelIntroCompletedBinding;

        private GameObject? _currentPresenterInstance;
        private ILevelIntroStagePresenter? _currentPresenter;
        private bool _currentPresenterOwnedByHost;
        private string _currentSessionSignature = string.Empty;
        private bool _disposed;

        public LevelIntroStagePresenterHost()
            : this(ResolvePresenterScopeResolverOrFail())
        {
        }

        public LevelIntroStagePresenterHost(ILevelIntroStagePresenterScopeResolver presenterScopeResolver)
        {
            _presenterScopeResolver = presenterScopeResolver ?? throw new ArgumentNullException(nameof(presenterScopeResolver));
            _levelIntroCompletedBinding = new EventBinding<LevelIntroCompletedEvent>(OnLevelIntroCompleted);
            EventBus<LevelIntroCompletedEvent>.Register(_levelIntroCompletedBinding);

            DebugUtility.LogVerbose<LevelIntroStagePresenterHost>(
                "[OBS][IntroStage] LevelIntroStagePresenterHost registrado (IntroStage presenter lifecycle bridge).",
                DebugUtility.Colors.Info);
        }

        public bool TryGetCurrentPresenter(out ILevelIntroStagePresenter presenter)
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

        public bool TryEnsureCurrentPresenter(_ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime.LevelIntroStageSession session, string source, out ILevelIntroStagePresenter presenter)
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

                if (IsCurrentPresenterQueryable(session.LevelSignature))
                {
                    presenter = _currentPresenter!;
                    return true;
                }

                DestroyCurrentPresenterLocked("session_changed");
                CreatePresenterInstanceLocked(session, source);

                if (!IsCurrentPresenterQueryable(session.LevelSignature))
                {
                    HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][IntroStage] Intro presenter contract not queryable after adoption/bind. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.LevelSignature}' registered='{(_currentPresenter != null).ToString().ToLowerInvariant()}' queryable='{(_currentPresenter != null && _currentPresenter.CanServe(session.LevelSignature)).ToString().ToLowerInvariant()}' presenterSignature='{Normalize(_currentPresenter?.PresenterSignature ?? string.Empty)}' currentSignature='{_currentSessionSignature}'.");
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
            EventBus<LevelIntroCompletedEvent>.Unregister(_levelIntroCompletedBinding);
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

        private void CreatePresenterInstanceLocked(_ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime.LevelIntroStageSession session, string source)
        {
            if (_presenterScopeResolver.TryResolvePresenters(session, out IReadOnlyList<ILevelIntroStagePresenter> scopedPresenters))
            {
                if (scopedPresenters.Count > 1)
                {
                    HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                        $"[FATAL][H1][IntroStage] Multiple ILevelIntroStagePresenter components found in local content. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.LevelSignature}' contentId='{session.LocalContentId}' presenters='{DescribePresenters(scopedPresenters)}'.");
                    return;
                }

                if (scopedPresenters.Count == 1 && scopedPresenters[0] != null)
                {
                    ILevelIntroStagePresenter existingPresenter = scopedPresenters[0];
                    AdoptPresenterLocked(existingPresenter, session, source, ownedByHost: false);

                    if (!IsCurrentPresenterQueryable(session.LevelSignature))
                    {
                        HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                        $"[FATAL][H1][IntroStage] Intro presenter from local content bound but not queryable. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.LevelSignature}'.");
                        return;
                    }

                    return;
                }
            }

            if (session.IntroPresenterPrefab == null)
            {
                ClearCurrentPresenterLocked(destroyOwnedInstance: false, reason: "missing_presenter_prefab");
                return;
            }

            GameObject? prefab = session.IntroPresenterPrefab;
            if (prefab == null)
            {
                ClearCurrentPresenterLocked(destroyOwnedInstance: false, reason: "missing_presenter_prefab");
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.MoveGameObjectToScene(instance, activeScene);

            if (!TryFindPresentersOnObject(instance, out List<ILevelIntroStagePresenter> presenters))
            {
                UnityEngine.Object.Destroy(instance);
                ClearCurrentPresenterLocked(destroyOwnedInstance: false, reason: "prefab_missing_presenter");
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                $"[FATAL][H1][IntroStage] Intro presenter prefab does not expose ILevelIntroStagePresenter. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.LevelSignature}' prefabType='GameObject'.");
                return;
            }

            if (presenters.Count > 1)
            {
                UnityEngine.Object.Destroy(instance);
                ClearCurrentPresenterLocked(destroyOwnedInstance: false, reason: "prefab_multiple_presenters");
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][IntroStage] Intro presenter prefab exposes multiple ILevelIntroStagePresenter components. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.LevelSignature}' presenters='{DescribePresenters(presenters)}'.");
                return;
            }

            ILevelIntroStagePresenter presenter = presenters[0];
            if (presenter is not MonoBehaviour presenterComponent)
            {
                UnityEngine.Object.Destroy(instance);
                ClearCurrentPresenterLocked(destroyOwnedInstance: false, reason: "prefab_non_monobehaviour");
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][IntroStage] Intro presenter prefab does not derive from MonoBehaviour. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.LevelSignature}' presenterType='{presenter.GetType().FullName}'.");
                return;
            }

            AdoptPresenterLocked(presenter, session, source, ownedByHost: true, presenterComponent.gameObject);

            if (!IsCurrentPresenterQueryable(session.LevelSignature))
            {
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][IntroStage] Intro presenter prefab bound but not queryable. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.LevelSignature}' presenterType='{presenter.GetType().Name}'.");
                return;
            }

            DebugUtility.Log<LevelIntroStagePresenterHost>(
                $"[OBS][IntroStage] IntroStagePresenterSpawned source='{source}' contentName='{DescribeSessionContentName(session)}' contentId='{session.LocalContentId}' signature='{session.LevelSignature}' presenterType='{presenter.GetType().Name}' scope='scene_local'.",
                DebugUtility.Colors.Info);
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

        private void AdoptPresenterLocked(ILevelIntroStagePresenter presenter, _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime.LevelIntroStageSession session, string source, bool ownedByHost, GameObject? presenterInstanceOverride = null)
        {
            if (presenter is not MonoBehaviour presenterComponent)
            {
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][IntroStage] Intro presenter does not derive from MonoBehaviour. source='{source}' contentName='{DescribeSessionContentName(session)}' signature='{session.LevelSignature}' presenterType='{presenter.GetType().FullName}'.");
                return;
            }

            LevelStagePresentationContract contract = BuildPresentationContract(session);
            presenter.AttachPresentation(contract);

            _currentPresenterInstance = presenterInstanceOverride ?? presenterComponent.gameObject;
            _currentPresenterOwnedByHost = ownedByHost;
            _currentSessionSignature = session.LevelSignature;
            _currentPresenter = presenter;

            DebugUtility.Log<LevelIntroStagePresenterHost>(
                $"[OBS][IntroStage] IntroStagePresenterRegistered source='{source}' contentName='{DescribeSessionContentName(session)}' contentId='{session.LocalContentId}' signature='{session.LevelSignature}' presenterType='{presenter.GetType().Name}' scope='{(ownedByHost ? "host_owned" : "scene_local")}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<LevelIntroStagePresenterHost>(
                $"[OBS][IntroStage] IntroStagePresenterAdopted source='{source}' contentName='{DescribeSessionContentName(session)}' contentId='{session.LocalContentId}' signature='{session.LevelSignature}' presenterType='{presenter.GetType().Name}' scope='{(ownedByHost ? "host_owned" : "scene_local")}'.",
                DebugUtility.Colors.Info);
        }

        private void OnLevelIntroCompleted(LevelIntroCompletedEvent evt)
        {
            if (!evt.Session.IsValid)
            {
                return;
            }

            lock (_sync)
            {
                if (!string.Equals(_currentSessionSignature, evt.Session.LevelSignature, StringComparison.Ordinal) ||
                    _currentPresenter == null)
                {
                    return;
                }

                string detachReason = Normalize(evt.Reason);
                string presenterType = _currentPresenter.GetType().Name;
                bool destroyOwnedInstance = _currentPresenterOwnedByHost;

                ClearCurrentPresenterLocked(destroyOwnedInstance, detachReason);

                DebugUtility.Log<LevelIntroStagePresenterHost>(
                    $"[OBS][IntroStage] IntroStagePresenterDetached reason='{detachReason}' signature='{Normalize(evt.Session.LevelSignature)}' presenterType='{presenterType}'.",
                    DebugUtility.Colors.Info);
            }
        }

        private static LevelStagePresentationContract BuildPresentationContract(_ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime.LevelIntroStageSession session)
        {
            bool hasRunResultStage = session.PhaseDefinitionRef != null &&
                                     session.PhaseDefinitionRef.RunResultStage != null &&
                                     session.PhaseDefinitionRef.RunResultStage.hasRunResultStage;

            return new LevelStagePresentationContract(
                session.PhaseDefinitionRef,
                session.LevelRef,
                session.LevelSignature,
                session.SelectionVersion,
                session.LocalContentId,
                session.HasIntroStage,
                hasRunResultStage);
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
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][IntroStage] Presenter detach failed. reason='{reason}' presenter='{_currentPresenter.GetType().FullName}' ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static string DescribePresenters(IEnumerable<ILevelIntroStagePresenter> presenters)
            => string.Join(", ", presenters.Select(DescribePresenter));

        private static string DescribePresenter(ILevelIntroStagePresenter presenter)
        {
            return presenter.GetType().Name;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static string DescribeSessionContentName(_ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime.LevelIntroStageSession session)
        {
            if (session.PhaseDefinitionRef != null)
            {
                return session.PhaseDefinitionRef.name;
            }

            if (session.LevelRef != null)
            {
                return session.LevelRef.name;
            }

            return "<none>";
        }

        private static ILevelIntroStagePresenterScopeResolver ResolvePresenterScopeResolverOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILevelIntroStagePresenterScopeResolver>(out var resolver) && resolver != null)
            {
                return resolver;
            }

            throw new InvalidOperationException("[FATAL][Config][IntroStage] ILevelIntroStagePresenterScopeResolver obrigatorio ausente.");
        }
    }
}
