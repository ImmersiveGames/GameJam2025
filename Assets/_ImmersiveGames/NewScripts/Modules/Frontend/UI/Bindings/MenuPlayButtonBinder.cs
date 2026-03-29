using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Bindings
{
    /// <summary>
    /// Binder (produção) para a intent visual "Play" do Frontend/UI.
    /// - OnClick() deve ser ligado no Inspector.
    /// - Sem corrotinas.
    ///
    /// Emite intent visual de start e delega a execução downstream para LevelFlow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuPlayButtonBinder : FrontendButtonBinderBase
    {
        private ILevelFlowRuntimeService _levelFlow;

        protected override void Awake()
        {
            base.Awake();

            DependencyManager.Provider.TryGetGlobal(out _levelFlow);
            if (_levelFlow == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[LevelFlow] ILevelFlowRuntimeService unavailable on Awake. Verify GlobalCompositionRoot registration before Frontend intent emission.");
            }
        }

        protected override bool OnClickCore(string actionReason)
        {
            if (_levelFlow == null)
            {
                DependencyManager.Provider.TryGetGlobal(out _levelFlow);
            }

            if (_levelFlow == null)
            {
                DebugUtility.LogWarning<MenuPlayButtonBinder>(
                    "[LevelFlow] Intent ignored: ILevelFlowRuntimeService unavailable.");
                return false;
            }

            string normalizedReason = string.IsNullOrWhiteSpace(actionReason) ? "Menu/PlayButton" : actionReason.Trim();
            DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                $"[OBS][FrontendUI][Intent] MenuPlay -> StartGameplayDefaultAsync reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            NavigationTaskRunner.FireAndForget(
                _levelFlow.StartGameplayDefaultAsync(normalizedReason),
                typeof(MenuPlayButtonBinder),
                "Menu/Play intent -> LevelFlow");

            return true;
        }
    }
}
