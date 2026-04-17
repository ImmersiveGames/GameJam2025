using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Transition;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    /// <summary>
    /// OWNER: composicao de gates no fim da transicao macro.
    /// NAO E OWNER: policy de prepare/clear (responsabilidade do boundary de GameplaySessionFlow).
    /// PUBLISH/CONSUME: nao publica eventos; apenas encadeia gates.
    /// Fases tocadas: Gate entre ScenesReady e BeforeFadeOut.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplaySessionFlowCompletionGate : ISceneTransitionCompletionGate
    {
        private readonly ISceneTransitionCompletionGate _innerGate;
        private ISceneTransitionCompletionGate _sessionFlowGate;

        public GameplaySessionFlowCompletionGate(ISceneTransitionCompletionGate innerGate)
        {
            _innerGate = innerGate ?? throw new ArgumentNullException(nameof(innerGate));
        }

        public void ConfigureGameplaySessionFlowGate(ISceneTransitionCompletionGate sessionFlowGate)
        {
            _sessionFlowGate = sessionFlowGate ?? throw new ArgumentNullException(nameof(sessionFlowGate));

            DebugUtility.LogVerbose<GameplaySessionFlowCompletionGate>(
                $"[SceneFlow] GameplaySessionFlow completion gate configured ({sessionFlowGate.GetType().Name}).",
                DebugUtility.Colors.Info);
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            await _innerGate.AwaitBeforeFadeOutAsync(context);

            if (_sessionFlowGate == null)
            {
                HardFailFastH1.Trigger(typeof(GameplaySessionFlowCompletionGate),
                    $"[FATAL][H1][SceneFlow] GameplaySessionFlow completion gate missing. routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
            }

            await _sessionFlowGate.AwaitBeforeFadeOutAsync(context);
        }
    }
}

