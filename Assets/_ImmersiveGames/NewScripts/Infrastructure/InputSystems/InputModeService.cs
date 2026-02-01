using System;
using _ImmersiveGames.NewScripts.Core.DebugLog;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Infrastructure.InputSystems
{
    /// <summary>
    /// Controla action maps (Player/UI) alternando entre gameplay, pause overlay e frontend menu.
    ///
    /// Regras de arquitetura:
    /// - Não existe PlayerInput "global".
    /// - Em gameplay, podem existir múltiplos PlayerInput (multiplayer).
    /// - Menu/UI deve funcionar via EventSystem + InputSystemUIInputModule (sem PlayerInput obrigatório).
    /// </summary>
    public sealed class InputModeService : IInputModeService
    {
        private const string DefaultPlayerMapName = "Player";
        private const string DefaultMenuMapName = "UI";

        private readonly string _playerMapName;
        private readonly string _menuMapName;

        private InputMode _currentMode = InputMode.None;

        public InputModeService(string playerMapName, string menuMapName)
        {
            _playerMapName = string.IsNullOrWhiteSpace(playerMapName) ? DefaultPlayerMapName : playerMapName;
            _menuMapName = string.IsNullOrWhiteSpace(menuMapName) ? DefaultMenuMapName : menuMapName;
        }

        public void SetFrontendMenu(string reason) => ApplyMode(InputMode.FrontendMenu, reason);
        public void SetGameplay(string reason) => ApplyMode(InputMode.Gameplay, reason);
        public void SetPauseOverlay(string reason) => ApplyMode(InputMode.PauseOverlay, reason);

        private void ApplyMode(InputMode mode, string reason)
        {
            string resolvedReason = string.IsNullOrWhiteSpace(reason) ? "InputMode/Unknown" : reason;

            bool isRepeat = _currentMode == mode;
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

            // Menu/Pause = UI map. Gameplay = Player map.
            string targetMapName = ShouldUseMenuMap(mode) ? _menuMapName : _playerMapName;

            // Multiplayer-safe: aplica em todos os PlayerInput ativos.
            PlayerInput[] inputs = FindActivePlayerInputs();
            if (inputs.Length == 0)
            {
                DebugUtility.LogVerbose<InputModeService>(
                    $"[InputMode] Nenhum PlayerInput ativo encontrado ao aplicar modo '{mode}'. " +
                    "Isto é esperado em Menu/Frontend. Em Gameplay, verifique se o Player foi spawnado.",
                    DebugUtility.Colors.Info);
                return;
            }

            bool anyHandled = false;
            bool anyMissingActions = false;
            int switchedCount = 0;

            for (int i = 0; i < inputs.Length; i++)
            {
                var pi = inputs[i];
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
                    // Action map ausente nesse asset (config).
                    DebugUtility.LogWarning<InputModeService>(
                        $"[InputMode] ActionMap '{targetMapName}' nao encontrada no PlayerInput '{pi.gameObject.name}'.");
                    anyHandled = true; // já diagnosticou algo útil
                    continue;
                }

                pi.SwitchCurrentActionMap(targetMapName);
                switchedCount++;
                anyHandled = true;
            }

            if (anyMissingActions)
            {
                // Esse warning é exatamente o que o seu log mostrou.
                DebugUtility.LogWarning<InputModeService>(
                    "[InputMode] PlayerInput encontrado sem 'actions' atribuídas; não é possível alternar action map. " +
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

            // Só cai aqui se inputs[] não estava vazio mas nada pôde ser processado (caso extremo).
            DebugUtility.LogWarning<InputModeService>(
                "[InputMode] Nenhum PlayerInput pôde ser processado para alternar action maps (todos nulos/desabilitados/sem actions).");
        }

        private static PlayerInput[] FindActivePlayerInputs()
        {
            // Inclui inativos; filtramos por activeInHierarchy.
            // FindObjectsOfType(true) funciona em versões mais antigas do Unity.
            PlayerInput[] all = Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                return Array.Empty<PlayerInput>();
            }

            // Filtra para ativos.
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                var pi = all[i];
                if (pi != null && pi.enabled && pi.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return Array.Empty<PlayerInput>();
            }

            var result = new PlayerInput[count];
            int idx = 0;
            for (int i = 0; i < all.Length; i++)
            {
                var pi = all[i];
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
