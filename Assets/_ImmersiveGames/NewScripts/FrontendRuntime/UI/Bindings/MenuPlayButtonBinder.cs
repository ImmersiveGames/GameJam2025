using ImmersiveGames.GameJam2025.Core.Events;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Orchestration.GameLoop.RunLifecycle.Core;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Experience.Frontend.UI.Bindings
{
    /// <summary>
    /// Binder (produção) para a intent visual "Play" do Frontend/UI.
    /// - OnClick() deve ser ligado no Inspector.
    /// - Sem coroutines.
    ///
    /// Emite intent visual de start e delega a execução downstream para a backbone.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuPlayButtonBinder : FrontendButtonBinderBase
    {

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

