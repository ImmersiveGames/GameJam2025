/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Criar PauseOverlayRoot desativado, adicionar PauseOverlayController e arrastar a referencia.
 * - Conectar botao Resume para PauseOverlayController.Resume().
 */
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.Infrastructure.Input
{
    /// <summary>
    /// Bootstrap para registrar IInputModeService no DI global.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class InputModeBootstrap : MonoBehaviour
    {
        [Header("Input Sources")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private InputActionAsset inputActionAsset;

        [Header("Action Map Names")]
        [SerializeField] private string playerMapName = "Player";
        [SerializeField] private string menuMapName = "UI";

        private static bool _initialized;

        public static void EnsureRegistered(
            PlayerInput playerInput,
            InputActionAsset inputActionAsset,
            string playerMapName,
            string menuMapName)
        {
            if (_initialized)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var existing) && existing != null)
            {
                _initialized = true;
                DebugUtility.LogVerbose(typeof(InputModeBootstrap),
                    "[InputMode] IInputModeService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var service = new InputModeService(playerInput, inputActionAsset, playerMapName, menuMapName);
            DependencyManager.Provider.RegisterGlobal<IInputModeService>(service);
            _initialized = true;

            DebugUtility.Log(typeof(InputModeBootstrap),
                "[InputMode] IInputModeService registrado no DI global.",
                DebugUtility.Colors.Success);
        }

        private void Awake()
        {
            EnsureRegistered(playerInput, inputActionAsset, playerMapName, menuMapName);
            DontDestroyOnLoad(gameObject);
        }

        private void Reset()
        {
            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }
        }
    }
}
