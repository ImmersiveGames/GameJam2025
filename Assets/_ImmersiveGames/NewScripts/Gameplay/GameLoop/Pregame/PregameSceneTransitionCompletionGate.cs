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

            try
            {
                var coordinator = ResolveCoordinator();
                var signature = SceneTransitionSignatureUtil.Compute(context);
                var pregameContext = new PregameContext(
                    contextSignature: signature,
                    profileId: context.TransitionProfileId,
                    targetScene: context.TargetActiveScene,
                    reason: "SceneFlow/BeforeFadeOut");

                await coordinator.RunPregameAsync(pregameContext);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PregameSceneTransitionCompletionGate>(
                    $"[Pregame] Falha ao executar pregame no gate. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static IPregameCoordinator ResolveCoordinator()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPregameCoordinator>(out var coordinator) && coordinator != null)
            {
                return coordinator;
            }

            return new PregameCoordinator();
        }
    }
}
