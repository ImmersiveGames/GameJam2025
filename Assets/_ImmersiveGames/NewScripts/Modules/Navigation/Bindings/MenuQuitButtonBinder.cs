using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Navigation.Bindings
{
    /// <summary>
    /// Binder (produção) para o botão "Quit" do Frontend.
    /// - OnClick() deve ser ligado no Inspector.
    /// - Sem coroutines.
    ///
    /// Em build: Application.Quit().
    /// No Editor: encerra Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class MenuQuitButtonBinder : FrontendButtonBinderBase
    {
        protected override bool OnClickCore(string actionReason)
        {
            DebugUtility.Log<MenuQuitButtonBinder>(
                $"[Quit] Quit solicitado. reason='{actionReason}'.");

#if UNITY_EDITOR
            DevStopPlayModeInEditor();
#else
            // Build: encerrar aplicação.
            Application.Quit();
#endif
            return true;
        }

        partial void DevStopPlayModeInEditor();
    }
}
