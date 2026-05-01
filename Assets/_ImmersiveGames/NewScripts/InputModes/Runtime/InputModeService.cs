using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    /// <summary>
    /// Controla action maps (Player/UI) alternando entre gameplay, pause overlay e frontend menu.
    ///
    /// Regras de arquitetura:
    /// - Nao existe PlayerInput "global".
    /// - Em gameplay, podem existir multiplos PlayerInput (multiplayer).
    /// - Menu/UI deve funcionar via EventSystem + InputSystemUIInputModule (sem PlayerInput obrigatorio).
    /// </summary>
    public sealed class InputModeService : IInputModeService, IInputModeStateService
    {
        private readonly IPlayerInputLocator _playerInputLocator;
        private readonly string _playerMapName;
        private readonly string _menuMapName;

        private InputModeRequestKind _currentMode = InputModeRequestKind.Unspecified;

        public InputModeService(IPlayerInputLocator playerInputLocator, string playerMapName, string menuMapName)
        {
            _playerInputLocator = playerInputLocator ?? throw new ArgumentNullException(nameof(playerInputLocator));
            _playerMapName = InputModesDefaults.NormalizeOrDefault(playerMapName, InputModesDefaults.PlayerActionMapName);
            _menuMapName = InputModesDefaults.NormalizeOrDefault(menuMapName, InputModesDefaults.MenuActionMapName);
        }

        public void SetFrontendMenu(string reason) => ApplyMode(InputModeRequestKind.FrontendMenu, reason);
        public void SetGameplay(string reason) => ApplyMode(InputModeRequestKind.Gameplay, reason);
        public void SetPauseOverlay(string reason) => ApplyMode(InputModeRequestKind.PauseOverlay, reason);

        public InputModeRequestKind CurrentMode => _currentMode;

        private void ApplyMode(InputModeRequestKind mode, string reason)
        {
            string resolvedReason = NormalizeReason(reason);
            InputModeRequestKind previousMode = _currentMode;

            bool isRepeat = previousMode == mode;
            PlayerInput[] inputs = _playerInputLocator.GetActivePlayerInputs();
            string targetMapName = ShouldUseMenuMap(mode) ? _menuMapName : _playerMapName;

            if (inputs.Length == 0)
            {
                if (mode == InputModeRequestKind.FrontendMenu)
                {
                    DebugUtility.LogVerbose<InputModeService>(
                        $"[InputMode] skipped_no_player_input_frontend mode='{mode}' reason='{resolvedReason}'.",
                        DebugUtility.Colors.Info);
                    _currentMode = mode;
                    PublishModeChanged(previousMode, mode, resolvedReason);
                    return;
                }

                FailFastMissingPlayerInput(mode, resolvedReason);
                return;
            }

            if (isRepeat)
            {
                DebugUtility.LogVerbose<InputModeService>(
                    $"[InputMode] Modo '{mode}' ja ativo. Reaplicando ({resolvedReason}).",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.Log<InputModeService>(
                    $"[InputMode] Modo alterado para '{mode}' ({resolvedReason}).",
                    DebugUtility.Colors.Info);
            }

            bool anyHandled = false;
            int switchedCount = 0;

            foreach (var pi in inputs)
            {
                if (pi == null)
                {
                    continue;
                }

                var actions = pi.actions;
                if (actions == null)
                {
                    FailFastMissingActionMap(mode, targetMapName, pi, "actions_null", resolvedReason);
                    return;
                }

                if (!HasActionMap(actions, targetMapName))
                {
                    FailFastMissingActionMap(mode, targetMapName, pi, "action_map_missing", resolvedReason);
                    return;
                }

                pi.SwitchCurrentActionMap(targetMapName);
                switchedCount++;
                anyHandled = true;
            }

            if (anyHandled)
            {
                _currentMode = mode;
                DebugUtility.LogVerbose<InputModeService>(
                    $"[InputMode] applied mode='{mode}' map='{targetMapName}' switchedPlayers='{switchedCount}/{inputs.Length}' reason='{resolvedReason}'.",
                    DebugUtility.Colors.Info);
                PublishModeChanged(previousMode, mode, resolvedReason);
                return;
            }

            FailFastMissingPlayerInput(mode, resolvedReason);
        }

        private static bool HasActionMap(InputActionAsset asset, string mapName)
        {
            if (asset == null || string.IsNullOrWhiteSpace(mapName))
            {
                return false;
            }

            return asset.FindActionMap(mapName) != null;
        }

        private static bool ShouldUseMenuMap(InputModeRequestKind mode) => mode != InputModeRequestKind.Gameplay;

        private static void FailFastMissingPlayerInput(InputModeRequestKind mode, string reason)
        {
            HardFailFastH1.Trigger(typeof(InputModeService),
                $"[FATAL][H1][InputModes] fail_fast_missing_player_input mode='{mode}' reason='{reason}'.");
        }

        private static void FailFastMissingActionMap(
            InputModeRequestKind mode,
            string targetMapName,
            PlayerInput playerInput,
            string failureKind,
            string reason)
        {
            string playerName = playerInput != null && playerInput.gameObject != null
                ? playerInput.gameObject.name
                : "<null>";

            HardFailFastH1.Trigger(typeof(InputModeService),
                $"[FATAL][H1][InputModes] fail_fast_missing_action_map mode='{mode}' map='{targetMapName}' playerInput='{playerName}' failureKind='{failureKind}' reason='{reason}'.");
        }

        private static string NormalizeReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "InputMode/Unknown";
            }

            return reason.Trim();
        }

        private static void PublishModeChanged(InputModeRequestKind previousMode, InputModeRequestKind currentMode, string reason)
        {
            if (previousMode == currentMode)
            {
                return;
            }

            EventBus<InputModeChangedEvent>.Raise(new InputModeChangedEvent(previousMode, currentMode, reason));
        }
    }
}

