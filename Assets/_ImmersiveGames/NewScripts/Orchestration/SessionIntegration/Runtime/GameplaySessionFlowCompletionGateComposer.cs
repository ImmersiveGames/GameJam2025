using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.ResetInterop.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.SessionIntegration.Runtime
{
    public static class GameplaySessionFlowCompletionGateComposer
    {
        public static void ComposeOrValidate()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneTransitionCompletionGate>(out var existingGate) && existingGate != null)
            {
                if (existingGate is GameplaySessionFlowCompletionGate)
                {
                    return;
                }

                if (existingGate is WorldResetCompletionGate)
                {
                    DebugUtility.LogVerbose(typeof(GameplaySessionFlowCompletionGateComposer),
                        "[OBS][SessionIntegration][SceneFlow] Fallback WorldResetCompletionGate sera substituido por GameplaySessionFlowCompletionGate.",
                        DebugUtility.Colors.Info);
                }
                else
                {
                    throw new System.InvalidOperationException(
                        $"[FATAL][Config][SessionIntegration] ISceneTransitionCompletionGate mismatch: existing gate type='{existingGate.GetType().Name}'.");
                }
            }

            var fallbackGate = new WorldResetCompletionGate(timeoutMs: 20000);
            var composedGate = new GameplaySessionFlowCompletionGate(fallbackGate);
            composedGate.ConfigureGameplaySessionFlowGate(new GameplaySessionFlowPrepareCompletionGate());

            DependencyManager.Provider.RegisterGlobal<ISceneTransitionCompletionGate>(composedGate, allowOverride: true);

            DebugUtility.LogVerbose(typeof(GameplaySessionFlowCompletionGateComposer),
                "[OBS][SessionIntegration][SceneFlow] GameplaySessionFlowCompletionGate registrado como gate canonico.",
                DebugUtility.Colors.Info);
        }
    }
}
