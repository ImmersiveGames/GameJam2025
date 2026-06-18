using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    /// <summary>
    /// Trilha canonica de InputMode nesta etapa.
    /// Aplica ActionMap nos PlayerInput ativos sem assumir ownership de lifecycle.
    /// </summary>
    public sealed class InputModeService : IInputModeService, IInputModeStateService
    {
        private readonly string _menuMapName;
        private readonly string _playerMapName;

        private InputModeRequestKind _currentMode = InputModeRequestKind.Unspecified;

        public InputModeService(string playerMapName, string menuMapName)
        {
            _playerMapName = playerMapName.TrimToEmpty();
            _menuMapName = menuMapName.TrimToEmpty();

            if (string.IsNullOrWhiteSpace(_playerMapName))
            {
                HardFailFastH1.Trigger(typeof(InputModeService), "[FATAL][Config][InputModes] playerActionMapName obrigatorio ausente.");
            }

            if (string.IsNullOrWhiteSpace(_menuMapName))
            {
                HardFailFastH1.Trigger(typeof(InputModeService), "[FATAL][Config][InputModes] menuActionMapName obrigatorio ausente.");
            }
        }

        public void SetFrontendMenu(string reason) => HandleRequest(InputModeRequestKind.FrontendMenu, reason);
        public void SetGameplay(string reason) => HandleRequest(InputModeRequestKind.Gameplay, reason);
        public void SetPauseOverlay(string reason) => HandleRequest(InputModeRequestKind.PauseOverlay, reason);
        public void SetInputLocked(string reason) => HandleRequest(InputModeRequestKind.InputLocked, reason);

        public void ApplyCurrentModeToPlayerInput(PlayerInput playerInput, string reason)
        {
            if (playerInput == null)
            {
                HardFailFastH1.Trigger(typeof(InputModeService),
                    $"[FATAL][Config][InputModes] PlayerInput obrigatorio ausente para reaplicar modo atual reason='{reason.TrimToOrDefault("InputMode/Unknown")}'.");
                return;
            }

            string resolvedReason = reason.TrimToOrDefault("InputMode/Unknown");
            switch (_currentMode)
            {
                case InputModeRequestKind.FrontendMenu:
                    ApplyModeToPlayerInput(_currentMode, _menuMapName, playerInput, resolvedReason);
                    return;

                case InputModeRequestKind.Gameplay:
                    ApplyModeToPlayerInput(_currentMode, _playerMapName, playerInput, resolvedReason);
                    return;

                case InputModeRequestKind.PauseOverlay:
                case InputModeRequestKind.InputLocked:
                    DebugUtility.Log(typeof(InputModeService),
                        $"InputModeCurrentModeAppliedToPlayerInput inputMode='{_currentMode}' reason='{resolvedReason}' target='state_only' playerInput='{playerInput.name}'.",
                        DebugUtility.Colors.Info);
                    return;

                case InputModeRequestKind.Unspecified:
                default:
                    HardFailFastH1.Trigger(typeof(InputModeService),
                        $"[FATAL][H1][InputModes] Current InputModeRequestKind '{_currentMode}' cannot be applied to PlayerInput '{playerInput.name}' reason='{resolvedReason}'.");
                    return;
            }
        }

        public InputModeRequestKind CurrentMode => _currentMode;

        private void HandleRequest(InputModeRequestKind mode, string reason)
        {
            string resolvedReason = reason.TrimToOrDefault("InputMode/Unknown");

            switch (mode)
            {
                case InputModeRequestKind.FrontendMenu:
                    ApplyModeToActivePlayerInputs(mode, _menuMapName, resolvedReason);
                    return;

                case InputModeRequestKind.Gameplay:
                    ApplyModeToActivePlayerInputs(mode, _playerMapName, resolvedReason);
                    return;

                case InputModeRequestKind.PauseOverlay:
                    _currentMode = mode;
                    DebugUtility.Log(typeof(InputModeService),
                        $"InputModeApplied inputMode='{mode}' reason='{resolvedReason}' target='state_only' detail='pause overlay does not switch action maps in Base 1.1 operational scope'.",
                        DebugUtility.Colors.Info);
                    return;

                case InputModeRequestKind.InputLocked:
                    _currentMode = mode;
                    DebugUtility.Log(typeof(InputModeService),
                        $"InputModeApplied inputMode='{mode}' reason='{resolvedReason}' target='state_only' detail='input locked policy does not switch action maps'.",
                        DebugUtility.Colors.Info);
                    return;

                case InputModeRequestKind.Unspecified:
                default:
                    HardFailFastH1.Trigger(typeof(InputModeService),
                        $"[FATAL][H1][InputModes] Unsupported InputModeRequestKind '{mode}' reason='{resolvedReason}'.");
                    return;
            }
        }

        private void ApplyModeToPlayerInput(InputModeRequestKind mode, string actionMapName, PlayerInput playerInput, string reason)
        {
            if (string.IsNullOrWhiteSpace(actionMapName))
            {
                HardFailFastH1.Trigger(typeof(InputModeService),
                    $"[FATAL][Config][InputModes] Action map obrigatorio ausente inputMode='{mode}' reason='{reason}'.");
                return;
            }

            if (playerInput == null || playerInput.actions == null)
            {
                HardFailFastH1.Trigger(typeof(InputModeService),
                    $"[FATAL][Config][InputModes] PlayerInput/actions obrigatorio ausente inputMode='{mode}' reason='{reason}'.");
                return;
            }

            if (playerInput.actions.FindActionMap(actionMapName, throwIfNotFound: false) == null)
            {
                HardFailFastH1.Trigger(typeof(InputModeService),
                    $"[FATAL][Config][InputModes] ActionMap ausente no PlayerInput. playerInput='{playerInput.name}' inputMode='{mode}' actionMap='{actionMapName}' reason='{reason}'.");
                return;
            }

            playerInput.SwitchCurrentActionMap(actionMapName);
            DebugUtility.Log(typeof(InputModeService),
                $"InputModeCurrentModeAppliedToPlayerInput inputMode='{mode}' reason='{reason}' actionMap='{actionMapName}' playerInput='{playerInput.name}'.",
                DebugUtility.Colors.Success);
        }

        private void ApplyModeToActivePlayerInputs(InputModeRequestKind mode, string actionMapName, string reason)
        {
            if (string.IsNullOrWhiteSpace(actionMapName))
            {
                HardFailFastH1.Trigger(typeof(InputModeService),
                    $"[FATAL][Config][InputModes] Action map obrigatorio ausente inputMode='{mode}' reason='{reason}'.");
                return;
            }

            PlayerInput[] playerInputs = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int switchedCount = 0;
            int observedCount = playerInputs?.Length ?? 0;

            if (playerInputs != null)
            {
                for (int i = 0; i < playerInputs.Length; i++)
                {
                    var playerInput = playerInputs[i];
                    if (playerInput == null || playerInput.actions == null)
                    {
                        continue;
                    }

                    if (playerInput.actions.FindActionMap(actionMapName, throwIfNotFound: false) == null)
                    {
                        HardFailFastH1.Trigger(typeof(InputModeService),
                            $"[FATAL][Config][InputModes] ActionMap ausente no PlayerInput. playerInput='{playerInput.name}' inputMode='{mode}' actionMap='{actionMapName}' reason='{reason}'.");
                        return;
                    }

                    playerInput.SwitchCurrentActionMap(actionMapName);
                    switchedCount += 1;
                }
            }

            _currentMode = mode;
            DebugUtility.Log(typeof(InputModeService),
                $"InputModeApplied inputMode='{mode}' reason='{reason}' actionMap='{actionMapName}' observedPlayerInputs='{observedCount}' switchedPlayerInputs='{switchedCount}'.",
                DebugUtility.Colors.Success);
        }
    }
}

