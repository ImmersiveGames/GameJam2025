#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PregameSceneTransitionCompletionGate : ISceneTransitionCompletionGate
    {
        private readonly ISceneTransitionCompletionGate _innerGate;

        public PregameSceneTransitionCompletionGate(ISceneTransitionCompletionGate innerGate)
        {
            _innerGate = innerGate ?? new NoOpSceneTransitionCompletionGate();
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            await _innerGate.AwaitBeforeFadeOutAsync(context);

            IPregamePolicyResolver policyResolver = null;
            if (DependencyManager.HasInstance)
            {
                DependencyManager.Provider.TryGetGlobal<IPregamePolicyResolver>(out policyResolver);
            }

            IPregameCoordinator coordinator = null;
            if (DependencyManager.HasInstance)
            {
                DependencyManager.Provider.TryGetGlobal<IPregameCoordinator>(out coordinator);
            }

            var policy = policyResolver?.Resolve(context.TransitionProfileId, context.TargetActiveScene, "SceneFlow/Pregame");
            if (coordinator == null)
            {
                if (policy == PregamePolicy.Disabled)
                {
                    DebugUtility.LogVerbose<PregameSceneTransitionCompletionGate>(
                        "[SceneFlowGate] Pregame policy disabled; gate ignorado.",
                        DebugUtility.Colors.Info);
                }
                else
                {
                    DebugUtility.LogWarning<PregameSceneTransitionCompletionGate>(
                        "[SceneFlowGate] IPregameCoordinator indisponível; gate ignorado.");
                }

                return;
            }

            var pregameContext = new PregameContext(
                contextSignature: context.ContextSignature,
                profileId: context.TransitionProfileId,
                targetScene: context.TargetActiveScene,
                reason: "SceneFlow/Pregame");

            try
            {
                await coordinator.RunPregameAsync(pregameContext);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PregameSceneTransitionCompletionGate>(
                    $"[SceneFlowGate] Falha ao executar Pregame. signature='{context.ContextSignature}' " +
                    $"profile='{context.TransitionProfileName}' target='{context.TargetActiveScene}' " +
                    $"ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }
    }
}
