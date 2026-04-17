/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Criar PauseOverlayRoot desativado, adicionar GamePauseOverlayController e arrastar a referencia.
 * - Conectar botao Resume para GamePauseOverlayController.Resume().
 */
namespace ImmersiveGames.GameJam2025.Infrastructure.InputModes
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

