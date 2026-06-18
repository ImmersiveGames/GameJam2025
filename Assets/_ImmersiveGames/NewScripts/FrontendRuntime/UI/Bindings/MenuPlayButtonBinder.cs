using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.RunPipeline.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.FrontendRuntime.UI.Bindings
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
            string normalizedReason = actionReason.TrimToOrDefault("Menu/PlayButton");
            DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                $"MenuPlay -> RunActivationRequestedEvent reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.LogVerbose<MenuPlayButtonBinder>(
                "Intent de Play delegada downstream para o backbone canonico.",
                DebugUtility.Colors.Info);

            EventBus<RunActivationRequestedEvent>.Raise(new RunActivationRequestedEvent(normalizedReason));

            return true;
        }
    }
}

