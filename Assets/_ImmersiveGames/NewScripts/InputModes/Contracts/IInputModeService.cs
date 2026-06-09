/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Recriar a UI de pause em formato canônico futuro.
 * - Conectar o fluxo de resume ao producer canônico adequado quando ele existir.
 */

using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.InputModes.Contracts
{
    /// <summary>
    /// Contrato para alternancia de modo de input entre gameplay, pause overlay e frontend.
    /// </summary>
    public interface IInputModeService
    {
        void SetFrontendMenu(string reason);
        void SetGameplay(string reason);
        void SetPauseOverlay(string reason);
        void SetInputLocked(string reason);
        void ApplyCurrentModeToPlayerInput(PlayerInput playerInput, string reason);
    }
}
