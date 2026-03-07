#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Pause.Bindings
{
    public partial class PauseOverlayController
    {
        private const string QaPauseReason = "QA/PauseOverlay/Show";
        private const string QaResumeReason = "QA/PauseOverlay/Hide";

        [ContextMenu("QA/Pause/Enter (TC: PauseOverlay)")]
        private void QaPause()
        {
            DebugUtility.Log(typeof(PauseOverlayController),
                $"[QA][PauseOverlay] Pause solicitado. reason='{QaPauseReason}'.",
                DebugUtility.Colors.Info);
            Show();
        }

        [ContextMenu("QA/Pause/Resume (TC: PauseOverlay Resume)")]
        private void QaResume()
        {
            DebugUtility.Log(typeof(PauseOverlayController),
                $"[QA][PauseOverlay] Resume solicitado. reason='{QaResumeReason}'.",
                DebugUtility.Colors.Info);
            Hide();
        }
    }
}
#endif
