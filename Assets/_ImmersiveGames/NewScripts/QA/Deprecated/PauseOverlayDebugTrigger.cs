// DEPRECATED QA TOOL — ver Docs/Reports/QA-Audit-2025-12-27.md
/*
 * VALIDACAO / CHECKLIST (UIGlobalScene)
 * - Criar PauseOverlayRoot desativado, adicionar PauseOverlayController e arrastar a referencia.
 * - Conectar botao Resume para PauseOverlayController.Resume().
 */
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Gameplay.Pause;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA
{
    /// <summary>
    /// Trigger simples para depuracao via ContextMenu.
    /// </summary>
    [DisallowMultipleComponent]
    [System.Obsolete("Deprecated QA tool; see QA-Audit-2025-12-27", false)]
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
#endif
