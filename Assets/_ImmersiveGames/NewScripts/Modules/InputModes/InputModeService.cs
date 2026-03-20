using System;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Modules.InputModes
{
    /// <summary>
    /// Controla action maps (Player/UI) alternando entre gameplay, pause overlay e frontend menu.
    ///
    /// Regras de arquitetura:
    /// - Nao existe PlayerInput "global".
    /// - Em gameplay, podem existir multiplos PlayerInput (multiplayer).
    /// - Menu/UI deve funcionar via EventSystem + InputSystemUIInputModule (sem PlayerInput obrigatorio).
    /// </summary>
    public sealed class InputModeService : IInputModeService
    {
        private readonly string _playerMapName;
        private readonly string _menuMapName;

        private InputMode _currentMode = InputMode.None;

        public InputModeService(string playerMapName, string menuMapName)
        {
            _playerMapName = InputModesDefaults.NormalizeOrDefault(playerMapName, InputModesDefaults.PlayerActionMapName);
            _menuMapName = InputModesDefaults.NormalizeOrDefault(menuMapName, InputModesDefaults.MenuActionMapName);
        }

        public void SetFrontendMenu(string reason) => ApplyMode(InputMode.FrontendMenu, reason);
        public void SetGameplay(string reason) => ApplyMode(InputMode.Gameplay, reason);
        public void SetPauseOverlay(string reason) => ApplyMode(InputMode.PauseOverlay, reason);

        private void ApplyMode(InputMode mode, string reason)
        {
            string resolvedReason = string.IsNullOrWhiteSpace(reason) ? "InputMode/Unknown" : reason;

            bool isRepeat = _currentMode == mode;
            _currentMode = mode;
            PlayerInput[] preResolvedInputs = null;

            if (isRepeat && IsFrontendCompletedReason(resolvedReason))
            {
                DebugUtility.LogVerbose<InputModeService>(
                    $"[InputMode] Modo '{mode}' ja ativo. Skip reapply ({resolvedReason}).",
                    DebugUtility.Colors.Info);
                return;
            }

            if (isRepeat && mode != InputMode.Gameplay)
            {
                preResolvedInputs = FindActivePlayerInputs();
                if (preResolvedInputs.Length == 0)
                {
                    DebugUtility.LogVerbose<InputModeService>(
                        $"[InputMode] Modo '{mode}' ja ativo e sem PlayerInput ativo. Skip reapply ({resolvedReason}).",
                        DebugUtility.Colors.Info);
                    return;
                }
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

            string targetMapName = ShouldUseMenuMap(mode) ? _menuMapName : _playerMapName;
            PlayerInput[] inputs = preResolvedInputs ?? FindActivePlayerInputs();
            if (inputs.Length == 0)
            {
                DebugUtility.LogVerbose<InputModeService>(
                    $"[InputMode] Nenhum PlayerInput ativo encontrado ao aplicar modo '{mode}'. " +
                    "Isto e esperado em Menu/Frontend. Em Gameplay, verifique se o Player foi spawnado.",
                    DebugUtility.Colors.Info);
                return;
            }

            bool anyHandled = false;
            bool anyMissingActions = false;
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
                    anyMissingActions = true;
                    continue;
                }

                if (!HasActionMap(actions, targetMapName))
                {
                    DebugUtility.LogWarning<InputModeService>(
                        $"[InputMode] ActionMap '{targetMapName}' nao encontrada no PlayerInput '{pi.gameObject.name}'.");
                    anyHandled = true;
                    continue;
                }

                pi.SwitchCurrentActionMap(targetMapName);
                switchedCount++;
                anyHandled = true;
            }

            if (anyMissingActions)
            {
                DebugUtility.LogWarning<InputModeService>(
                    "[InputMode] PlayerInput encontrado sem 'actions' atribuidas; nao e possivel alternar action map. " +
                    "Corrija o prefab do Player (PlayerInput -> Actions = seu InputActionAsset).");
                anyHandled = true;
            }

            if (anyHandled)
            {
                DebugUtility.LogVerbose<InputModeService>(
                    $"[InputMode] Applied map '{targetMapName}' em {switchedCount}/{inputs.Length} PlayerInput(s) ({resolvedReason}).",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.LogWarning<InputModeService>(
                "[InputMode] Nenhum PlayerInput pode ser processado para alternar action maps (todos nulos/desabilitados/sem actions).");
        }

        private static PlayerInput[] FindActivePlayerInputs()
        {
            PlayerInput[] all = Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                return Array.Empty<PlayerInput>();
            }

            int count = all.Count(pi => pi != null && pi.enabled && pi.gameObject.activeInHierarchy);
            if (count == 0)
            {
                return Array.Empty<PlayerInput>();
            }

            var result = new PlayerInput[count];
            int idx = 0;
            foreach (var pi in all)
            {
                if (pi != null && pi.enabled && pi.gameObject.activeInHierarchy)
                {
                    result[idx++] = pi;
                }
            }

            return result;
        }

        private static bool HasActionMap(InputActionAsset asset, string mapName)
        {
            if (asset == null || string.IsNullOrWhiteSpace(mapName))
            {
                return false;
            }

            return asset.FindActionMap(mapName) != null;
        }

        private static bool ShouldUseMenuMap(InputMode mode) => mode != InputMode.Gameplay;

        private static bool IsFrontendCompletedReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return false;
            }

            return reason.StartsWith("SceneFlow/Completed:Frontend", StringComparison.OrdinalIgnoreCase);
        }

        private enum InputMode
        {
            None,
            Gameplay,
            PauseOverlay,
            FrontendMenu
        }
    }
}
