using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Experience.Frontend.UI.Bindings
{
    /// <summary>
    /// Binder (produção) para a intent visual "Play" do Frontend/UI.
    /// - OnClick() deve ser ligado no Inspector.
    /// - Sem corrotinas.
    ///
    /// Emite intent visual de start e delega a execução downstream para o backbone.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuPlayButtonBinder : FrontendButtonBinderBase
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override bool OnClickCore(string actionReason)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(actionReason) ? "Menu/PlayButton" : actionReason.Trim();
            GameplaySessionFlowSmokeReporter.ReportCurrentState("MenuPlayButton/BeforeRaise", normalizedReason);
            DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                $"[OBS][FrontendUI][Intent] MenuPlay -> GamePlayRequestedEvent reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                "[OBS][FrontendUI][Delegate] Intent de Play delegada downstream para o backbone canonico.",
                DebugUtility.Colors.Info);

            EventBus<GamePlayRequestedEvent>.Raise(new GamePlayRequestedEvent(normalizedReason));

            return true;
        }
    }
}
