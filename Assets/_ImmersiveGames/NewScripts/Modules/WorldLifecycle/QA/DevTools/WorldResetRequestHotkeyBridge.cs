#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_DEV
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Core;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.QA.DevTools
{
    [DisallowMultipleComponent]
    public sealed class WorldResetRequestHotkeyBridge : MonoBehaviour
    {
        private const string Source = "Gameplay/HotkeyR";

        [SerializeField] private bool requireShift = true;

        private IWorldResetRequestService _resetRequestService;

        private void Update()
        {
            if (!IsHotkeyPressed())
            {
                return;
            }

            if (!EnsureService())
            {
                DebugUtility.LogWarning(typeof(WorldResetRequestHotkeyBridge),
                    "[WorldLifecycle] IWorldResetRequestService não disponível. Hotkey ignorado.");
                return;
            }

            _ = _resetRequestService.RequestResetAsync(Source);
        }

        private bool EnsureService()
        {
            if (_resetRequestService != null)
            {
                return true;
            }

            return DependencyManager.Provider.TryGetGlobal(out _resetRequestService) &&
                   _resetRequestService != null;
        }

        private bool IsHotkeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            if (requireShift && !keyboard.leftShiftKey.isPressed && !keyboard.rightShiftKey.isPressed)
            {
                return false;
            }

            return keyboard.rKey.wasPressedThisFrame;
#else
            if (requireShift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                return false;
            }

            return Input.GetKeyDown(KeyCode.R);
#endif
        }
    }

    public static class WorldResetRequestHotkeyDevBootstrap
    {
        private const string HotkeyObjectName = "[WorldLifecycle] Reset Hotkey (Dev)";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Object.FindAnyObjectByType<WorldResetRequestHotkeyBridge>() != null)
            {
                return;
            }

            var go = new GameObject(HotkeyObjectName)
            {
                hideFlags = HideFlags.DontSave
            };
            Object.DontDestroyOnLoad(go);
            go.AddComponent<WorldResetRequestHotkeyBridge>();

            DebugUtility.LogVerbose(typeof(WorldResetRequestHotkeyDevBootstrap),
                "[WorldLifecycle] Hotkey DEV instalado (Shift+R) para RequestResetAsync.",
                DebugUtility.Colors.Info);
        }
    }
}
#endif



