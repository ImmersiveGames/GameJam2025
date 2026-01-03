using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.UI
{
    /// <summary>
    /// Binder de UI (produção) para o botão "Quit" do Frontend.
    /// IMPORTANTE:
    /// - Este script NÃO registra listeners via código.
    /// - Você DEVE ligar o método OnClick() no Button.OnClick() pelo Inspector.
    ///
    /// Observação:
    /// - Em build: chama Application.Quit().
    /// - No Editor: para o Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuQuitButtonBinder : MonoBehaviour
    {
#if UNITY_EDITOR
        private const float EditorFallbackSeconds = 2f;
#endif

        [Header("Refs")]
        [SerializeField] private Button button;

        [Header("Config")]
        [SerializeField] private string reason = "Frontend/QuitButton";

        [Header("Safety")]
        [Tooltip("Ignora cliques por X segundos ao habilitar o objeto (mitiga Submit/Enter preso ao voltar para o Frontend).")]
        [SerializeField] private float ignoreClicksForSecondsAfterEnable = 0.25f;

        [Tooltip("Quando true, limpa o Selected do EventSystem ao habilitar (mitiga Submit automático).")]
        [SerializeField] private bool clearEventSystemSelectionOnEnable = true;

        private float _ignoreClicksUntilUnscaledTime;

#if UNITY_EDITOR
        private Coroutine _editorFallbackRoutine;
        private bool _waitingQuit;
#endif

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button == null)
            {
                DebugUtility.LogWarning<MenuQuitButtonBinder>(
                    "[Quit] Button não atribuído e GetComponent<Button>() falhou. OnClick() (Inspector) pode não funcionar.");
            }

            ArmClickGuard(ignoreClicksForSecondsAfterEnable, "Awake/EnableGuard");
        }

        private void OnEnable()
        {
            ArmClickGuard(ignoreClicksForSecondsAfterEnable, "OnEnable/Guard");

            if (clearEventSystemSelectionOnEnable)
            {
                TryClearEventSystemSelection();
            }
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() no Inspector.
        /// </summary>
        public void OnClick()
        {
            if (Time.unscaledTime < _ignoreClicksUntilUnscaledTime)
            {
                DebugUtility.LogVerbose<MenuQuitButtonBinder>(
                    $"[Quit] Clique ignorado (cooldown). remaining={( _ignoreClicksUntilUnscaledTime - Time.unscaledTime ):0.000}s",
                    DebugUtility.Colors.Warning);
                return;
            }

            if (button != null && !button.interactable)
            {
                return;
            }

            SetButtonInteractable(false, "QuitRequested");

            DebugUtility.Log<MenuQuitButtonBinder>(
                $"[Quit] Quit solicitado. reason='{reason}'.");

#if UNITY_EDITOR
            _waitingQuit = true;
            StartEditorFallbackStop();
#endif

            RequestQuit();
        }

        private void RequestQuit()
        {
#if UNITY_EDITOR
            // Editor: encerrar Play Mode.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // Build: encerrar aplicação.
            Application.Quit();
#endif
        }

        private void ArmClickGuard(float seconds, string label)
        {
            if (seconds <= 0f)
            {
                return;
            }

            _ignoreClicksUntilUnscaledTime = Mathf.Max(_ignoreClicksUntilUnscaledTime, Time.unscaledTime + seconds);

            DebugUtility.LogVerbose<MenuQuitButtonBinder>(
                $"[Quit] Click-guard armado por {seconds:0.000}s (label='{label}').",
                DebugUtility.Colors.Info);
        }

        private void TryClearEventSystemSelection()
        {
            var es = EventSystem.current;
            if (es == null)
            {
                return;
            }

            if (button != null && es.currentSelectedGameObject == button.gameObject)
            {
                es.SetSelectedGameObject(null);
                DebugUtility.LogVerbose<MenuQuitButtonBinder>(
                    "[Quit] EventSystem selection limpa (evitar Submit automático).",
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

            DebugUtility.LogVerbose<MenuQuitButtonBinder>(
                $"[Quit] Button interactable={(value ? "ON" : "OFF")} (reason='{reasonLabel}').",
                DebugUtility.Colors.Info);
        }

#if UNITY_EDITOR
        private void StartEditorFallbackStop()
        {
            if (!Application.isEditor)
            {
                return;
            }

            if (_editorFallbackRoutine != null)
            {
                StopCoroutine(_editorFallbackRoutine);
            }

            _editorFallbackRoutine = StartCoroutine(EditorFallbackRoutine());
        }

        private System.Collections.IEnumerator EditorFallbackRoutine()
        {
            yield return new WaitForSeconds(EditorFallbackSeconds);

            if (!_waitingQuit)
            {
                yield break;
            }

            _waitingQuit = false;

            // Se por algum motivo o play mode não parou (ex.: interceptado por script),
            // reabilita o botão para evitar UI travada durante testes.
            SetButtonInteractable(true, "EditorFallback");

            DebugUtility.LogWarning<MenuQuitButtonBinder>(
                "[Quit] Fallback de editor acionado: reabilitando botão (Play Mode não encerrou como esperado).");
        }
#endif
    }
}
