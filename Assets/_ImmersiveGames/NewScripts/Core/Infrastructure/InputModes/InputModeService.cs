using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Infrastructure.InputModes.Runtime;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.NewScripts.Core.Infrastructure.InputModes
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
            _currentMode = mode;
            PlayerInput[] preResolvedInputs = null;

            if (isRepeat && mode != InputModeRequestKind.Gameplay)
            {
                preResolvedInputs = _playerInputLocator.GetActivePlayerInputs();
                if (preResolvedInputs.Length == 0)
                {
                    DebugUtility.LogVerbose<InputModeService>(
                        $"[InputMode] Modo '{mode}' ja ativo e sem PlayerInput ativo. Skip reapply ({resolvedReason}).",
                        DebugUtility.Colors.Info);
                    PublishModeChanged(previousMode, mode, resolvedReason);
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
            PlayerInput[] inputs = preResolvedInputs ?? _playerInputLocator.GetActivePlayerInputs();
            if (inputs.Length == 0)
            {
                DebugUtility.LogVerbose<InputModeService>(
                    $"[InputMode] Nenhum PlayerInput ativo encontrado ao aplicar modo '{mode}'. " +
                    "Isto e esperado em Menu/Frontend. Em Gameplay, verifique se o Player foi spawnado.",
                    DebugUtility.Colors.Info);
                PublishModeChanged(previousMode, mode, resolvedReason);
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
                PublishModeChanged(previousMode, mode, resolvedReason);
                return;
            }

            DebugUtility.LogWarning<InputModeService>(
                "[InputMode] Nenhum PlayerInput pode ser processado para alternar action maps (todos nulos/desabilitados/sem actions).");
            PublishModeChanged(previousMode, mode, resolvedReason);
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
