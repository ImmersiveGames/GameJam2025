/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Criar PauseOverlayRoot desativado, adicionar PauseOverlayController e arrastar a referencia.
 * - Conectar botao Resume para PauseOverlayController.Resume().
 */
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Pause
{
    /// <summary>
    /// Trigger simples para depuracao via ContextMenu.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseOverlayDebugTrigger : MonoBehaviour
    {
        [SerializeField] private PauseOverlayController controller;

        [ContextMenu("Toggle Pause Overlay")]
        public void TogglePauseOverlay()
        {
            var target = ResolveController();
            if (target == null)
            {
                DebugUtility.LogWarning(typeof(PauseOverlayDebugTrigger),
                    "[PauseOverlay] PauseOverlayController nao encontrado.");
                return;
            }

            DebugUtility.Log(typeof(PauseOverlayDebugTrigger),
                "[PauseOverlay] ContextMenu -> Toggle",
                DebugUtility.Colors.Info);
            target.Toggle();
        }

        private PauseOverlayController ResolveController()
        {
            if (controller != null)
            {
                return controller;
            }

            controller = FindFirstObjectByType<PauseOverlayController>();
            return controller;
        }
    }
}
