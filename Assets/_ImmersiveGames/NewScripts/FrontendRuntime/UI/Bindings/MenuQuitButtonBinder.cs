using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.FrontendRuntime.UI.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.FrontendRuntime.UI.Bindings
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
        private bool _isCanonicalOperationalProfile;

        protected override void Awake()
        {
            base.Awake();

            _isCanonicalOperationalProfile = IsCanonicalOperationalProfile();
            if (_isCanonicalOperationalProfile)
            {
                if (button != null)
                {
                    button.interactable = false;
                }

                DebugUtility.Log<MenuQuitButtonBinder>(
                    "[OBS][FrontendUI][Intent] Quit button observed_noop reason='canonical_no_frontend_quit'.");
                return;
            }

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

            if (_isCanonicalOperationalProfile)
            {
                DebugUtility.Log<MenuQuitButtonBinder>(
                    "[OBS][FrontendUI][Intent] Quit button observed_noop reason='canonical_no_frontend_quit'.");
                return false;
            }

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

        private static bool IsCanonicalOperationalProfile()
        {
            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<RuntimeModeConfig>(out var runtimeModeConfig) || runtimeModeConfig == null)
            {
                return false;
            }

            return runtimeModeConfig.compositionProfile == CompositionProfileKind.Base11Sandbox;
        }
    }
}

