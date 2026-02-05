/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Criar PauseOverlayRoot desativado, adicionar PauseOverlayController e arrastar a referencia.
 * - Conectar botao Resume para PauseOverlayController.Resume().
 */
namespace _ImmersiveGames.NewScripts.Modules.InputModes
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
