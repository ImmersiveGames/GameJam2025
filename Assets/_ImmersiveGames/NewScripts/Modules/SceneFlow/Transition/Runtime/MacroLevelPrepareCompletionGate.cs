using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using UnityEngine;

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
                string detail = "[SceneFlow] MacroLoadingPhase='LevelPrepare' blocked: DependencyManager.Provider unavailable.";
                if (!IsDevelopmentEscapeHatch())
                {
                    FailFastH1(detail, context);
                }

                DebugUtility.LogWarning<MacroLevelPrepareCompletionGate>(
                    $"[WARN][DEGRADED][SceneFlow] MacroLoadingPhase='LevelPrepare' skipped (DEV escape hatch). detail='{detail}' recommendation='register DI provider before transition'.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILevelMacroPrepareService>(out var prepareService) || prepareService == null)
            {
                string detail = $"[SceneFlow] MacroLoadingPhase='LevelPrepare' blocked: ILevelMacroPrepareService missing. routeId='{context.RouteId}'.";
                if (!IsDevelopmentEscapeHatch())
                {
                    FailFastH1(detail, context);
                }

                DebugUtility.LogWarning<MacroLevelPrepareCompletionGate>(
                    $"[WARN][DEGRADED][SceneFlow] MacroLoadingPhase='LevelPrepare' skipped (DEV escape hatch). routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}' recommendation='register ILevelMacroPrepareService in global DI'.");
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

        private static bool IsDevelopmentEscapeHatch()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return Debug.isDebugBuild;
#endif
        }

        private static void FailFastH1(string detail, SceneTransitionContext context)
        {
            string message =
                $"[FATAL][H1] {detail} routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.";
            DebugUtility.LogError<MacroLevelPrepareCompletionGate>(message);
            throw new InvalidOperationException(message);
        }

    }
}
