/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Criar PauseOverlayRoot desativado, adicionar PauseOverlayController e arrastar a referencia.
 * - Conectar botao Resume para PauseOverlayController.Resume().
 */
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.Infrastructure.InputSystems
{
    /// <summary>
    /// Controla action maps (Player/UI) para alternar entre gameplay, pause overlay e frontend menu.
    /// </summary>
    public sealed class InputModeService : IInputModeService
    {
        private const string DefaultPlayerMapName = "Player";
        private const string DefaultMenuMapName = "UI";

        private readonly string _playerMapName;
        private readonly string _menuMapName;
        private PlayerInput _playerInput;
        private InputActionAsset _inputActions;
        private InputMode _currentMode = InputMode.None;

        public InputModeService(
            PlayerInput playerInput,
            InputActionAsset inputActions,
            string playerMapName,
            string menuMapName)
        {
            _playerInput = playerInput;
            _inputActions = inputActions;
            _playerMapName = string.IsNullOrWhiteSpace(playerMapName) ? DefaultPlayerMapName : playerMapName;
            _menuMapName = string.IsNullOrWhiteSpace(menuMapName) ? DefaultMenuMapName : menuMapName;
        }

        public void SetFrontendMenu(string reason) => ApplyMode(InputMode.FrontendMenu, reason);
        public void SetGameplay(string reason) => ApplyMode(InputMode.Gameplay, reason);
        public void SetPauseOverlay(string reason) => ApplyMode(InputMode.PauseOverlay, reason);

        private void ApplyMode(InputMode mode, string reason)
        {
            if (mode == InputMode.Gameplay || mode == InputMode.PauseOverlay)
            {
                TryResolvePlayerInputForGameplay(mode);
            }

            EnsureReferences();

            var resolvedReason = string.IsNullOrWhiteSpace(reason) ? "InputMode/Unknown" : reason;
            var isRepeat = _currentMode == mode;
            _currentMode = mode;

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

            bool handled = false;

            if (_playerInput != null)
            {
                handled |= TrySwitchPlayerInput(mode, resolvedReason);
            }

            bool skipAssetIfSameAsPlayerInput = _playerInput != null && _inputActions != null && ReferenceEquals(_playerInput.actions, _inputActions);
            if (!skipAssetIfSameAsPlayerInput)
            {
                handled |= TryApplyAssetMaps(mode, resolvedReason);
            }

            if (!handled)
            {
                DebugUtility.LogWarning<InputModeService>(
                    "[InputMode] Nenhum PlayerInput ou InputActionAsset disponivel para alternar action maps.");
            }
        }

        private void EnsureReferences()
        {
            if (_playerInput == null)
            {
                _playerInput = Object.FindFirstObjectByType<PlayerInput>();
                if (_playerInput != null)
                {
                    DebugUtility.LogVerbose<InputModeService>(
                        "[InputMode] PlayerInput encontrado dinamicamente.",
                        DebugUtility.Colors.Info);
                }
            }

            if (_inputActions == null && _playerInput != null && _playerInput.actions != null)
            {
                _inputActions = _playerInput.actions;
                DebugUtility.LogVerbose<InputModeService>(
                    "[InputMode] InputActionAsset resolvido via PlayerInput.",
                    DebugUtility.Colors.Info);
            }
        }

        private void TryResolvePlayerInputForGameplay(InputMode mode)
        {
            if (_playerInput != null && _playerInput.gameObject.activeInHierarchy)
            {
                return;
            }

            var found = Object.FindFirstObjectByType<PlayerInput>();
            if (found == null)
            {
                DebugUtility.LogWarning<InputModeService>(
                    $"[InputMode] PlayerInput nao encontrado na cena ao entrar em '{mode}'.");
                return;
            }

            _playerInput = found;
            DebugUtility.LogVerbose<InputModeService>(
                $"[InputMode] PlayerInput encontrado e cacheado para '{mode}'.",
                DebugUtility.Colors.Info);
        }

        private bool TrySwitchPlayerInput(InputMode mode, string reason)
        {
            if (_playerInput == null)
            {
                return false;
            }

            var targetMapName = ShouldUseMenuMap(mode) ? _menuMapName : _playerMapName;
            if (!HasActionMap(_playerInput.actions, targetMapName))
            {
                DebugUtility.LogWarning<InputModeService>(
                    $"[InputMode] ActionMap '{targetMapName}' nao encontrada no PlayerInput.");
                return true;
            }

            _playerInput.SwitchCurrentActionMap(targetMapName);
            DebugUtility.LogVerbose<InputModeService>(
                $"[InputMode] PlayerInput -> CurrentActionMap='{targetMapName}' ({reason}).",
                DebugUtility.Colors.Info);
            return true;
        }

        private bool TryApplyAssetMaps(InputMode mode, string reason)
        {
            if (_inputActions == null)
            {
                return false;
            }

            var playerMap = _inputActions.FindActionMap(_playerMapName, false);
            var menuMap = _inputActions.FindActionMap(_menuMapName, false);
            var useMenu = ShouldUseMenuMap(mode);
            bool anyMapResolved = false;

            if (playerMap == null)
            {
                DebugUtility.LogWarning<InputModeService>(
                    $"[InputMode] ActionMap '{_playerMapName}' nao encontrada no InputActionAsset.");
            }
            else
            {
                anyMapResolved = true;
                if (useMenu)
                {
                    playerMap.Disable();
                }
                else
                {
                    playerMap.Enable();
                }
            }

            if (menuMap == null)
            {
                DebugUtility.LogWarning<InputModeService>(
                    $"[InputMode] ActionMap '{_menuMapName}' nao encontrada no InputActionAsset.");
            }
            else
            {
                anyMapResolved = true;
                if (useMenu)
                {
                    menuMap.Enable();
                }
                else
                {
                    menuMap.Disable();
                }
            }

            if (anyMapResolved)
            {
                DebugUtility.LogVerbose<InputModeService>(
                    $"[InputMode] InputActionAsset -> Player='{_playerMapName}' {(useMenu ? "OFF" : "ON")}, UI='{_menuMapName}' {(useMenu ? "ON" : "OFF")} ({reason}).",
                    DebugUtility.Colors.Info);
            }

            return true;
        }

        private static bool HasActionMap(InputActionAsset asset, string mapName)
        {
            if (asset == null || string.IsNullOrWhiteSpace(mapName))
            {
                return false;
            }

            return asset.FindActionMap(mapName, false) != null;
        }

        private static bool ShouldUseMenuMap(InputMode mode) => mode != InputMode.Gameplay;

        private enum InputMode
        {
            None,
            Gameplay,
            PauseOverlay,
            FrontendMenu
        }
    }
}
