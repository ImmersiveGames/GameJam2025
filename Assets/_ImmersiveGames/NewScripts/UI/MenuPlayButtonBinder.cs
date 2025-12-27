using System.Collections;
using UnityEngine;
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
#if UNITY_EDITOR
        private const float EditorFallbackSeconds = 2f;
#endif

        [SerializeField] private Button button;
        [SerializeField] private string reason = "Menu/PlayButton";

        private IGameNavigationService _navigation;
        private EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;
        private bool _transitionCompletedSubscribed;
        private bool _waitingForMenuReturn;
#if UNITY_EDITOR
        private Coroutine _editorFallbackRoutine;
#endif

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

            if (_navigation == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[Navigation] IGameNavigationService indisponível no Awake. Verifique se o GlobalBootstrap registrou antes do Menu.");
            }

            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);
        }

        private void OnEnable()
        {
            RegisterSceneFlowEvents();
        }

        private void OnDisable()
        {
            UnregisterSceneFlowEvents();
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }

            UnregisterSceneFlowEvents();
        }

        // Associe este método no Button.OnClick()
        public void OnClick()
        {
            if (button != null && !button.interactable)
            {
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
                SetButtonInteractable(true, "NavigationUnavailable");
                return;
            }

            SetButtonInteractable(false, "RequestToGameplay");
            _ = _navigation.RequestToGameplay(reason);

            _waitingForMenuReturn = true;
            if (!_transitionCompletedSubscribed)
            {
                StartEditorFallbackReenable();
            }
        }

        private void RegisterSceneFlowEvents()
        {
            if (_transitionCompletedBinding == null || _transitionCompletedSubscribed)
            {
                return;
            }

            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);
            _transitionCompletedSubscribed = true;
        }

        private void UnregisterSceneFlowEvents()
        {
            if (!_transitionCompletedSubscribed)
            {
                return;
            }

            EventBus<SceneTransitionCompletedEvent>.Unregister(_transitionCompletedBinding);
            _transitionCompletedSubscribed = false;
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (!_waitingForMenuReturn)
            {
                return;
            }

            var targetScene = evt.Context.TargetActiveScene;
            if (!string.Equals(targetScene, MenuSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _waitingForMenuReturn = false;
            SetButtonInteractable(true, "MenuReturn");
        }

        private void SetButtonInteractable(bool value, string reasonLabel)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = value;

            DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                $"[Navigation] Button interactable={(value ? "ON" : "OFF")} (reason='{reasonLabel}').",
                DebugUtility.Colors.Info);
        }

        private void StartEditorFallbackReenable()
        {
#if UNITY_EDITOR
            if (!Application.isEditor)
            {
                return;
            }

            if (_editorFallbackRoutine != null)
            {
                StopCoroutine(_editorFallbackRoutine);
            }

            _editorFallbackRoutine = StartCoroutine(EditorFallbackRoutine());
#endif
        }

#if UNITY_EDITOR
        private IEnumerator EditorFallbackRoutine()
        {
            yield return new WaitForSeconds(EditorFallbackSeconds);

            if (!_waitingForMenuReturn)
            {
                yield break;
            }

            _waitingForMenuReturn = false;
            SetButtonInteractable(true, "EditorFallback");

            DebugUtility.LogWarning<MenuPlayButtonBinder>(
                "[Navigation] Fallback de editor acionado para reabilitar o botão Play.");
        }
#endif
    }
}
