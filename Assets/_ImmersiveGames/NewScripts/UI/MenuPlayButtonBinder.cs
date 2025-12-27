using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.UI
{
    /// <summary>
    /// Binder de UI (produção) para o botão "Play" do Menu.
    /// Chama IGameNavigationService.RequestToGameplay().
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuPlayButtonBinder : MonoBehaviour
    {
        private const string MenuSceneName = "MenuScene";

        [SerializeField] private Button button;
        [SerializeField] private string reason = "Menu/PlayButton";

        private IGameNavigationService _navigation;
        private EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;
        private bool _transitionListenerRegistered;
        private bool _buttonLocked;
        private Coroutine _editorFallbackRoutine;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            DependencyManager.Provider.TryGetGlobal(out _navigation);

            if (button != null)
            {
                button.onClick.AddListener(OnClick);
            }
            else
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[Navigation] Button não atribuído e GetComponent<Button>() falhou.");
            }

            RegisterSceneFlowListener();

            if (_navigation == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[Navigation] IGameNavigationService indisponível no Awake. Verifique se o GlobalBootstrap registrou antes do Menu.");
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }

            if (_transitionListenerRegistered)
            {
                EventBus<SceneTransitionCompletedEvent>.Unregister(_transitionCompletedBinding);
                _transitionListenerRegistered = false;
            }

            if (_editorFallbackRoutine != null)
            {
                StopCoroutine(_editorFallbackRoutine);
                _editorFallbackRoutine = null;
            }
        }

        // Associe este método no Button.OnClick()
        public void OnClick()
        {
            if (_buttonLocked || (button != null && !button.interactable))
            {
                DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                    "[Navigation] Clique ignorado: botão já está desabilitado.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (_navigation == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _navigation);
            }

            if (_navigation == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[Navigation] Clique ignorado: IGameNavigationService indisponível.");
                return;
            }

            SetButtonLocked(true);
            _ = _navigation.RequestToGameplay(reason);

#if UNITY_EDITOR
            if (!_transitionListenerRegistered)
            {
                StartEditorFallback();
            }
#endif
        }

        private void RegisterSceneFlowListener()
        {
            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);
            _transitionListenerRegistered = true;
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (!_buttonLocked || button == null)
            {
                return;
            }

            var targetScene = evt.Context.TargetActiveScene;
            var activeScene = SceneManager.GetActiveScene().name;

            if (!string.Equals(targetScene, MenuSceneName, StringComparison.Ordinal)
                && !string.Equals(activeScene, MenuSceneName, StringComparison.Ordinal))
            {
                return;
            }

            SetButtonLocked(false);
        }

        private void SetButtonLocked(bool locked)
        {
            _buttonLocked = locked;

            if (button == null)
            {
                return;
            }

            button.interactable = !locked;
        }

#if UNITY_EDITOR
        private void StartEditorFallback()
        {
            if (_editorFallbackRoutine != null)
            {
                StopCoroutine(_editorFallbackRoutine);
            }

            _editorFallbackRoutine = StartCoroutine(ReenableAfterDelay(2f));
        }

        private IEnumerator ReenableAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            if (_buttonLocked)
            {
                DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                    "[Navigation] Reabilitando botão via fallback do Editor.",
                    DebugUtility.Colors.Info);
                SetButtonLocked(false);
            }

            _editorFallbackRoutine = null;
        }
#endif
    }
}
