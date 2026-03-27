#nullable enable
using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public sealed class LevelIntroStagePresenterHost : ILevelIntroStagePresenterRegistry, IDisposable
    {
        private readonly object _sync = new();

        private GameObject _currentPresenterInstance;
        private ILevelIntroStagePresenter _currentPresenter;
        private bool _currentPresenterOwnedByHost;
        private string _currentSessionSignature = string.Empty;

        public LevelIntroStagePresenterHost()
        {
            DebugUtility.LogVerbose<LevelIntroStagePresenterHost>(
                "[OBS][LevelFlow] LevelIntroStagePresenterHost registrado (level content presenter lifecycle).",
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

                presenter = null;
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

        public bool TryEnsureCurrentPresenter(LevelIntroStageSession session, string source)
        {
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

                if (!string.IsNullOrWhiteSpace(_currentSessionSignature) &&
                    string.Equals(_currentSessionSignature, session.LevelSignature, StringComparison.Ordinal) &&
                    _currentPresenterInstance != null &&
                    _currentPresenter != null &&
                    _currentPresenter.IsReady)
                {
                    return true;
                }

                DestroyCurrentPresenterLocked();
                CreatePresenterInstanceLocked(session, source);
                return _currentPresenter != null && _currentPresenter.IsReady;
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
                    _currentPresenter = null;
                }
            }
        }

        public void Dispose()
        {
            DestroyCurrentPresenter();
        }

        private void CreatePresenterInstanceLocked(LevelIntroStageSession session, string source)
        {
            LevelIntroStageMockPresenter existingPresenter = UnityEngine.Object.FindFirstObjectByType<LevelIntroStageMockPresenter>(FindObjectsInactive.Include);
            if (existingPresenter != null)
            {
                existingPresenter.BindToSession(session.LevelSignature);
                _currentPresenterInstance = existingPresenter.gameObject;
                _currentPresenterOwnedByHost = false;
                _currentSessionSignature = session.LevelSignature;

                DebugUtility.Log<LevelIntroStagePresenterHost>(
                    $"[OBS][LevelFlow] IntroPresenterAdopted source='{source}' levelRef='{session.LevelRef.name}' contentId='{session.LocalContentId}' signature='{session.LevelSignature}' presenter='{existingPresenter.name}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            GameObject prefab = session.PresenterPrefab;
            if (prefab == null)
            {
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.MoveGameObjectToScene(instance, activeScene);

            bool hasPresenterComponent = false;
            foreach (var component in instance.GetComponents<MonoBehaviour>())
            {
                if (component is ILevelIntroStagePresenter)
                {
                    hasPresenterComponent = true;
                    break;
                }
            }

            if (!hasPresenterComponent)
            {
                UnityEngine.Object.Destroy(instance);
                HardFailFastH1.Trigger(typeof(LevelIntroStagePresenterHost),
                    $"[FATAL][H1][LevelFlow] Intro presenter prefab does not expose ILevelIntroStagePresenter. source='{source}' levelRef='{session.LevelRef.name}' signature='{session.LevelSignature}' prefab='{prefab.name}'.");
            }

            _currentPresenterInstance = instance;
            _currentPresenterOwnedByHost = true;
            _currentSessionSignature = session.LevelSignature;
            MonoBehaviour[] components = instance.GetComponents<MonoBehaviour>();
            if (components != null)
            {
                foreach (var component in components)
                {
                    if (component is ILevelIntroStagePresenter presenter)
                    {
                        presenter.BindToSession(session.LevelSignature);
                        break;
                    }
                }
            }

            DebugUtility.Log<LevelIntroStagePresenterHost>(
                $"[OBS][LevelFlow] IntroPresenterSpawned source='{source}' levelRef='{session.LevelRef.name}' contentId='{session.LocalContentId}' signature='{session.LevelSignature}' presenter='{prefab.name}'.",
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
            if (_currentPresenterInstance != null && _currentPresenterOwnedByHost)
            {
                UnityEngine.Object.Destroy(_currentPresenterInstance);
            }

            _currentPresenterInstance = null;
            _currentPresenter = null;
            _currentPresenterOwnedByHost = false;
            _currentSessionSignature = string.Empty;
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
