/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Recriar a UI de pause em formato canônico futuro.
 * - Conectar o fluxo de resume ao producer canônico adequado quando ele existir.
 */
using _ImmersiveGames.NewScripts.InputModes.Runtime;

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
    }
}

