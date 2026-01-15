#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageSceneTransitionCompletionGate : ISceneTransitionCompletionGate
    {
        private readonly ISceneTransitionCompletionGate _innerGate;

        public IntroStageSceneTransitionCompletionGate(ISceneTransitionCompletionGate innerGate)
        {
            _innerGate = innerGate ?? new NoOpSceneTransitionCompletionGate();
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            await _innerGate.AwaitBeforeFadeOutAsync(context);

            IIntroStageCoordinator coordinator = null;
            if (DependencyManager.HasInstance)
            {
                DependencyManager.Provider.TryGetGlobal<IIntroStagePolicyResolver>(out var policyResolver);
                DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out coordinator);

                var policy = policyResolver?.Resolve(
                    context.TransitionProfileId,
                    context.TargetActiveScene,
                    "SceneFlow/IntroStage") ?? IntroStagePolicy.Manual;

                if (policy == IntroStagePolicy.Disabled)
                {
                    DebugUtility.LogVerbose<IntroStageSceneTransitionCompletionGate>(
                        "[SceneFlowGate] IntroStage policy disabled; gate ignorado.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (coordinator == null)
                {
                    DebugUtility.LogWarning<IntroStageSceneTransitionCompletionGate>(
                        "[SceneFlowGate] IIntroStageCoordinator indisponível; gate ignorado.");
                    return;
                }
            }
            if (coordinator == null)
            {
                DebugUtility.LogWarning<IntroStageSceneTransitionCompletionGate>(
                    "[SceneFlowGate] DependencyManager indisponível; gate ignorado.");
                return;
            }

            var introStageContext = new IntroStageContext(
                contextSignature: context.ContextSignature,
                profileId: context.TransitionProfileId,
                targetScene: context.TargetActiveScene,
                reason: "SceneFlow/IntroStage");

            try
            {
                await coordinator.RunIntroStageAsync(introStageContext);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<IntroStageSceneTransitionCompletionGate>(
                    $"[SceneFlowGate] Falha ao executar IntroStage. signature='{context.ContextSignature}' " +
                    $"profile='{context.TransitionProfileName}' target='{context.TargetActiveScene}' " +
                    $"ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }
    }
}
