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

            if (!context.RouteId.IsValid)
            {
                return;
            }

            if (DependencyManager.Provider == null)
            {
                string detail = "[SceneFlow] MacroLoadingPhase='LevelPrepare' blocked: DependencyManager.Provider unavailable.";
                FailFastH1(detail, context);
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelMacroPrepareService>(out var prepareService) || prepareService == null)
            {
                string detail = $"[SceneFlow] MacroLoadingPhase='LevelPrepare' blocked: ILevelMacroPrepareService missing. routeId='{context.RouteId}'.";
                FailFastH1(detail, context);
            }

            string reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "SceneFlow/LevelPrepare"
                : context.Reason.Trim();
            string signature = SceneTransitionSignature.Compute(context);

            DebugUtility.Log<MacroLevelPrepareCompletionGate>(
                $"[OBS][SceneFlow] MacroLoadingPhase='LevelPrepare' routeId='{context.RouteId}' signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            await prepareService.PrepareOrClearAsync(context.RouteId, reason);
        }

        private static void FailFastH1(string detail, SceneTransitionContext context)
        {
            HardFailFastH1.Trigger(typeof(MacroLevelPrepareCompletionGate),
                $"{detail} routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
        }
    }
}
