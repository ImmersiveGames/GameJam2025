using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class MacroLevelPrepareCompletionGate : ISceneTransitionCompletionGate
    {
        private readonly ISceneTransitionCompletionGate _innerGate;

        public MacroLevelPrepareCompletionGate(ISceneTransitionCompletionGate innerGate)
        {
            _innerGate = innerGate ?? throw new ArgumentNullException(nameof(innerGate));
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            await _innerGate.AwaitBeforeFadeOutAsync(context);

            if (context.TransitionProfileId != SceneFlowProfileId.Gameplay || !context.RouteId.IsValid)
            {
                return;
            }

            if (DependencyManager.Provider == null)
            {
                DebugUtility.LogWarning<MacroLevelPrepareCompletionGate>(
                    "[WARN][OBS][SceneFlow] MacroLoadingPhase='LevelPrepare' skipped: DependencyManager.Provider indisponível.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelMacroPrepareService>(out var prepareService) || prepareService == null)
            {
                DebugUtility.LogWarning<MacroLevelPrepareCompletionGate>(
                    $"[WARN][OBS][SceneFlow] MacroLoadingPhase='LevelPrepare' skipped: ILevelMacroPrepareService ausente. routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}'.");
                return;
            }

            string reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "SceneFlow/LevelPrepare"
                : context.Reason.Trim();
            string signature = SceneTransitionSignature.Compute(context);

            DebugUtility.Log<MacroLevelPrepareCompletionGate>(
                $"[OBS][SceneFlow] MacroLoadingPhase='LevelPrepare' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            await prepareService.PrepareAsync(context.RouteId, reason);
        }
    }
}
