using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Frontend.UI.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Experience.Frontend.UI.Bindings
{
    /// <summary>
    /// Binder (produção) para a intent visual "Quit" do Frontend/UI.
    /// - OnClick() deve ser ligado no Inspector.
    /// - Sem corrotinas.
    ///
    /// Em build: Application.Quit().
    /// No Editor: encerra Play Mode.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuQuitButtonBinder : FrontendButtonBinderBase
    {
        private IFrontendQuitService _quitService;

        protected override void Awake()
        {
            base.Awake();

            DependencyManager.Provider.TryGetGlobal(out _quitService);
            if (_quitService == null)
            {
                DebugUtility.LogWarning<MenuQuitButtonBinder>(
                    "[FATAL][Config][FrontendUI] IFrontendQuitService indisponivel no Awake. Quit deve ser registrado antes do Frontend UI.");
            }
        }

        protected override bool OnClickCore(string actionReason)
        {
            DebugUtility.Log<MenuQuitButtonBinder>(
                $"[OBS][FrontendUI][Intent] Quit solicitado. reason='{actionReason}'.");

            if (_quitService == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _quitService);
            }

            if (_quitService == null)
            {
                throw new System.InvalidOperationException(
                    "[FATAL][Config][FrontendUI] IFrontendQuitService ausente. Nao foi possivel delegar a quit intent.");
            }

            DebugUtility.Log<MenuQuitButtonBinder>(
                "[OBS][FrontendUI][Delegate] Intent de Quit delegada ao executor tecnico IFrontendQuitService.");

            _quitService.Quit(actionReason);
            return true;
        }
    }
}
