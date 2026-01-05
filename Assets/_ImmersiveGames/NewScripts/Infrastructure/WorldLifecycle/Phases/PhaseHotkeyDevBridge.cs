#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_DEV
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Phases
{
    [DisallowMultipleComponent]
    public sealed class PhaseHotkeyDevBridge : MonoBehaviour
    {
        private const string Phase1Id = "Phase1";
        private const string Phase2Id = "Phase2";

        private const string Phase1Reason = "Dev/Hotkey1";
        private const string Phase2Reason = "Dev/Hotkey2";

        private const string Phase1ResetSource = "Phase/DevHotkey1";
        private const string Phase2ResetSource = "Phase/DevHotkey2";

        private IWorldPhaseService _phaseService;
        private IWorldResetRequestService _resetRequestService;

        private void Update()
        {
            if (IsHotkeyPressed(out var phaseId, out var reason, out var resetSource))
            {
                HandleRequest(phaseId, reason, resetSource);
            }
        }

        private void HandleRequest(string phaseId, string reason, string resetSource)
        {
            if (!EnsureServices())
            {
                DebugUtility.LogWarning(typeof(PhaseHotkeyDevBridge),
                    "[Phase] Serviços indisponíveis. Hotkey ignorado.");
                return;
            }

            _phaseService.RequestPhase(new PhaseId(phaseId), reason);
            _ = _resetRequestService.RequestResetAsync(resetSource);
        }

        private bool EnsureServices()
        {
            var provider = DependencyManager.Provider;
            if (_phaseService == null)
            {
                provider.TryGetGlobal<IWorldPhaseService>(out _phaseService);
            }

            if (_resetRequestService == null)
            {
                provider.TryGetGlobal<IWorldResetRequestService>(out _resetRequestService);
            }

            return _phaseService != null && _resetRequestService != null;
        }

        private static bool IsHotkeyPressed(out string phaseId, out string reason, out string resetSource)
        {
            phaseId = null;
            reason = null;
            resetSource = null;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            if (!keyboard.leftShiftKey.isPressed && !keyboard.rightShiftKey.isPressed)
            {
                return false;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                phaseId = Phase1Id;
                reason = Phase1Reason;
                resetSource = Phase1ResetSource;
                return true;
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                phaseId = Phase2Id;
                reason = Phase2Reason;
                resetSource = Phase2ResetSource;
                return true;
            }
#else
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                return false;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                phaseId = Phase1Id;
                reason = Phase1Reason;
                resetSource = Phase1ResetSource;
                return true;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                phaseId = Phase2Id;
                reason = Phase2Reason;
                resetSource = Phase2ResetSource;
                return true;
            }
#endif

            return false;
        }
    }

    public static class PhaseHotkeyDevBootstrap
    {
        private const string HotkeyObjectName = "[Phase] Hotkey (Dev)";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Object.FindAnyObjectByType<PhaseHotkeyDevBridge>() != null)
            {
                return;
            }

            var go = new GameObject(HotkeyObjectName);
            go.hideFlags = HideFlags.DontSave;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<PhaseHotkeyDevBridge>();

            DebugUtility.LogVerbose(typeof(PhaseHotkeyDevBootstrap),
                "[Phase] Hotkey DEV instalado (Shift+1/2) para trocar fase e resetar.",
                DebugUtility.Colors.Info);
        }
    }
}
#endif
