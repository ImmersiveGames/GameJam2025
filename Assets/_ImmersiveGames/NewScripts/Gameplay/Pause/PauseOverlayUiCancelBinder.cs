using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.Pause
{
    /// <summary>
    /// Binder de produção para alternar o PauseOverlay usando o "Cancel" do InputSystemUIInputModule (ex.: ESC).
    /// Não depende de PlayerInput (evita "PlayerInput global" e problemas de action map).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseOverlayUiCancelBinder : MonoBehaviour
    {
        [Header("Scene Gate")]
        [Tooltip("Toggle só funciona quando a cena ativa for esta (ex.: GameplayScene).")]
        [SerializeField] private string requiredActiveSceneName = "GameplayScene";

        [Header("Refs (opcional)")]
        [SerializeField] private PauseOverlayController overlay;
        [SerializeField] private InputSystemUIInputModule uiInputModule;

        [Header("Action fallback (se cancel não estiver exposto)")]
        [SerializeField] private string cancelActionName = "Cancel";

        private InputAction _cancelAction;

        private void Awake()
        {
            if (overlay == null)
            {
                overlay = FindFirstObjectByType<PauseOverlayController>();
            }

            if (uiInputModule == null)
            {
                uiInputModule = FindFirstObjectByType<InputSystemUIInputModule>();
            }

            if (overlay == null)
            {
                DebugUtility.LogWarning<PauseOverlayUiCancelBinder>(
                    "[PauseOverlay] PauseOverlayController não encontrado. Binder ficará inativo.");
            }

            if (uiInputModule == null)
            {
                DebugUtility.LogWarning<PauseOverlayUiCancelBinder>(
                    "[PauseOverlay] InputSystemUIInputModule não encontrado. Binder ficará inativo.");
                return;
            }

            // Preferência: usar a referência direta do módulo
            if (uiInputModule.cancel != null && uiInputModule.cancel.action != null)
            {
                _cancelAction = uiInputModule.cancel.action;
            }
            else if (uiInputModule.actionsAsset != null)
            {
                // Fallback: procurar pelo nome "Cancel" dentro do asset do UI module
                _cancelAction = uiInputModule.actionsAsset.FindAction(cancelActionName, throwIfNotFound: false);
            }

            if (_cancelAction == null)
            {
                DebugUtility.LogWarning<PauseOverlayUiCancelBinder>(
                    $"[PauseOverlay] Action de Cancel não encontrada (name='{cancelActionName}'). Binder ficará inativo.");
            }
        }

        private void OnEnable()
        {
            if (_cancelAction == null)
            {
                return;
            }

            _cancelAction.performed += OnCancelPerformed;
            if (!_cancelAction.enabled)
            {
                _cancelAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (_cancelAction == null)
            {
                return;
            }

            _cancelAction.performed -= OnCancelPerformed;
        }

        private void OnCancelPerformed(InputAction.CallbackContext _)
        {
            if (overlay == null)
            {
                return;
            }

            // Evita abrir o overlay no Menu.
            var active = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(requiredActiveSceneName) && active != requiredActiveSceneName)
            {
                return;
            }

            overlay.Toggle();
        }
    }
}
