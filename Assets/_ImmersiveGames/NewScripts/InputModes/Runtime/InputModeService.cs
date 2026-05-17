using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
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
            _playerMapName = InputModesDefaults.Normalize(playerMapName);
            _menuMapName = InputModesDefaults.Normalize(menuMapName);

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

        public InputModeRequestKind CurrentMode => _currentMode;

        private void HandleRequest(InputModeRequestKind mode, string reason)
        {
            string resolvedReason = NormalizeReason(reason);

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
                        $"[OBS][InputModes] InputModeApplied inputMode='{mode}' reason='{resolvedReason}' target='state_only' detail='pause overlay does not switch action maps in Base 1.1 operational scope'.",
                        DebugUtility.Colors.Info);
                    return;

                case InputModeRequestKind.Unspecified:
                default:
                    HardFailFastH1.Trigger(typeof(InputModeService),
                        $"[FATAL][H1][InputModes] Unsupported InputModeRequestKind '{mode}' reason='{resolvedReason}'.");
                    return;
            }
        }

        private static string NormalizeReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "InputMode/Unknown";
            }

            return reason.Trim();
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
                    PlayerInput playerInput = playerInputs[i];
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
                $"[OBS][InputModes] InputModeApplied inputMode='{mode}' reason='{reason}' actionMap='{actionMapName}' observedPlayerInputs='{observedCount}' switchedPlayerInputs='{switchedCount}'.",
                DebugUtility.Colors.Success);
        }
    }
}

