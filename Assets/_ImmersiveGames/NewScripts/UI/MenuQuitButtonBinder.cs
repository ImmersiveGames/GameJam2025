using _ImmersiveGames.NewScripts.Core.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.UI
{
    /// <summary>
    /// Binder (produção) para o botão "Quit" do Frontend.
    /// - OnClick() deve ser ligado no Inspector.
    /// - Sem corrotinas.
    ///
    /// Em build: Application.Quit().
    /// No Editor: encerra Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuQuitButtonBinder : FrontendButtonBinderBase
    {
        protected override bool OnClickCore(string actionReason)
        {
            DebugUtility.Log<MenuQuitButtonBinder>(
                $"[Quit] Quit solicitado. reason='{actionReason}'.");

#if UNITY_EDITOR
            // Editor: encerrar Play Mode.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // Build: encerrar aplicação.
            Application.Quit();
#endif
            return true;
        }
    }
}
