using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.UI
{
    /// <summary>
    /// Binder de UI (produção) para o botão "Play" do Menu.
    /// IMPORTANTE:
    /// - Este script NÃO registra listeners via código.
    /// - Você DEVE ligar o método OnClick() no Button.OnClick() pelo Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuPlayButtonBinder : MonoBehaviour
    {
        private const string MenuSceneName = "MenuScene";

#if UNITY_EDITOR
        private const float EditorFallbackSeconds = 2f;
#endif

        [Header("Refs")]
        [SerializeField] private Button button;

        [Header("Config")]
        [SerializeField] private string reason = "Menu/PlayButton";

        [Header("Safety")]
        [Tooltip("Ignora cliques por X segundos ao habilitar o objeto (mitiga Submit/Enter preso na volta ao Menu).")]
        [SerializeField] private float ignoreClicksForSecondsAfterEnable = 0.25f;

        [Tooltip("Ignora cliques por X segundos ao detectar retorno ao Menu via SceneFlow (mitiga auto-click).")]
        [SerializeField] private float ignoreClicksForSecondsAfterMenuReturn = 0.35f;

        [Tooltip("Quando true, limpa o Selected do EventSystem ao voltar para o Menu para evitar Submit automático.")]
        [SerializeField] private bool clearEventSystemSelectionOnMenuReturn = true;

        private IGameNavigationService _navigation;
        private EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;
        private bool _transitionCompletedSubscribed;

        private bool _waitingForMenuReturn;
        private float _ignoreClicksUntilUnscaledTime;

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

            if (_navigation == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[Navigation] IGameNavigationService indisponível no Awake. Verifique se o GlobalBootstrap registrou antes do Menu.");
            }

            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);

            if (button == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[Navigation] Button não atribuído e GetComponent<Button>() falhou. OnClick() (Inspector) pode não funcionar.");
            }

            ArmClickGuard(ignoreClicksForSecondsAfterEnable, "Awake/EnableGuard");
        }

        private void OnEnable()
        {
            RegisterSceneFlowEvents();
            ArmClickGuard(ignoreClicksForSecondsAfterEnable, "OnEnable/Guard");
        }

        private void OnDisable()
        {
            UnregisterSceneFlowEvents();
        }

        private void OnDestroy()
        {
            UnregisterSceneFlowEvents();
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() no Inspector.
        /// </summary>
        public void OnClick()
        {
            // Guard contra submit/click fantasma na volta do menu.
            if (Time.unscaledTime < _ignoreClicksUntilUnscaledTime)
            {
                DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                    $"[Navigation] Clique ignorado (cooldown). remaining={( _ignoreClicksUntilUnscaledTime - Time.unscaledTime ):0.000}s",
                    DebugUtility.Colors.Warning);
                return;
            }

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

            // Fire-and-forget: o GameNavigationService faz guard de in-flight/debounce.
            _ = _navigation.RequestToGameplay(reason);

            _waitingForMenuReturn = true;
            StartEditorFallbackReenable();
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

            // Reabilita UI do menu, mas evita auto-click por Submit/Enter "preso".
            SetButtonInteractable(true, "MenuReturn");
            ArmClickGuard(ignoreClicksForSecondsAfterMenuReturn, "MenuReturn/Guard");

            if (clearEventSystemSelectionOnMenuReturn)
            {
                TryClearEventSystemSelection();
            }
        }

        private void ArmClickGuard(float seconds, string label)
        {
            if (seconds <= 0f)
            {
                return;
            }

            _ignoreClicksUntilUnscaledTime = Mathf.Max(_ignoreClicksUntilUnscaledTime, Time.unscaledTime + seconds);

            DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                $"[Navigation] Click-guard armado por {seconds:0.000}s (label='{label}').",
                DebugUtility.Colors.Info);
        }

        private void TryClearEventSystemSelection()
        {
            var es = EventSystem.current;
            if (es == null)
            {
                return;
            }

            // Se o botão estiver selecionado, um Submit pendente pode disparar o onClick automaticamente.
            if (button != null && es.currentSelectedGameObject == button.gameObject)
            {
                es.SetSelectedGameObject(null);
                DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                    "[Navigation] EventSystem selection limpa (evitar Submit automático).",
                    DebugUtility.Colors.Info);
            }
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
            ArmClickGuard(ignoreClicksForSecondsAfterMenuReturn, "EditorFallback/Guard");

            DebugUtility.LogWarning<MenuPlayButtonBinder>(
                "[Navigation] Fallback de editor acionado para reabilitar o botão Play.");
        }
#endif
    }
}
