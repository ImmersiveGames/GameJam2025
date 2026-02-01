using _ImmersiveGames.NewScripts.Core.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.UI
{
    /// <summary>
    /// Binder genérico para trocar painéis do Frontend (Main/Options/HowTo).
    /// - OnClick() deve ser ligado no Inspector.
    /// - Sem corrotinas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FrontendShowPanelButtonBinder : FrontendButtonBinderBase
    {
        [Header("Panel")]
        [SerializeField] private FrontendPanelsController controller;

        [SerializeField] private string targetPanelId = "main";

        protected override void Awake()
        {
            base.Awake();

            if (controller == null)
            {
                controller = GetComponentInParent<FrontendPanelsController>();
            }

            if (controller == null)
            {
                DebugUtility.LogWarning<FrontendShowPanelButtonBinder>(
                    "[FrontendPanels] Controller não atribuído e GetComponentInParent<FrontendPanelsController>() falhou.");
            }
        }

        protected override bool OnClickCore(string actionReason)
        {
            if (controller == null)
            {
                DebugUtility.LogWarning<FrontendShowPanelButtonBinder>(
                    "[FrontendPanels] Clique ignorado: FrontendPanelsController indisponível.");
                return false;
            }

            DebugUtility.LogVerbose<FrontendShowPanelButtonBinder>(
                $"[FrontendPanels] ShowPanel solicitado. target='{targetPanelId}', reason='{actionReason}'.",
                DebugUtility.Colors.Info);

            controller.Show(targetPanelId, actionReason);
            return true;
        }
    }
}
