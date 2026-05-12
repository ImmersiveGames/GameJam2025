using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    /// <summary>
    /// Trilha canonica de InputMode nesta etapa.
    /// Nao aplica ActionMap real; apenas registra requests e outcomes observados/deferred.
    /// </summary>
    public sealed class InputModeService : IInputModeService, IInputModeStateService
    {
        private readonly string _menuMapName;
        private readonly string _playerMapName;

        private InputModeRequestKind _currentMode = InputModeRequestKind.Unspecified;

        public InputModeService(string playerMapName, string menuMapName)
        {
            _playerMapName = InputModesDefaults.NormalizeOrDefault(playerMapName, InputModesDefaults.PlayerActionMapName);
            _menuMapName = InputModesDefaults.NormalizeOrDefault(menuMapName, InputModesDefaults.MenuActionMapName);
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
                    DebugUtility.LogVerbose<InputModeService>(
                        $"[OBS][InputModes] outcomeKind='deferred_no_runtime_target' mode='FrontendMenu' reason='{resolvedReason}' target='<none>' actionMap='{_menuMapName}'.",
                        DebugUtility.Colors.Info);
                    return;

                case InputModeRequestKind.Gameplay:
                    DebugUtility.LogVerbose<InputModeService>(
                        $"[OBS][InputModes] outcomeKind='observed_noop' mode='Gameplay' reason='{resolvedReason}' target='<none>' actionMap='{_playerMapName}'.",
                        DebugUtility.Colors.Info);
                    return;

                case InputModeRequestKind.PauseOverlay:
                    DebugUtility.LogVerbose<InputModeService>(
                        $"[OBS][InputModes] outcomeKind='observed_noop' mode='PauseOverlay' reason='{resolvedReason}' target='<none>' actionMap='<none>' detail='pause overlay remains observed_noop in this stage.'.",
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
    }
}

