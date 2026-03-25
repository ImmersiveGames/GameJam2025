using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// OWNER: composicao de gates no fim da transicao macro.
    /// NAO E OWNER: policy de LevelPrepare/Clear (responsabilidade do boundary de LevelFlow).
    /// PUBLISH/CONSUME: nao publica eventos; apenas encadeia gates.
    /// Fases tocadas: Gate entre ScenesReady e BeforeFadeOut.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class MacroLevelPrepareCompletionGate : ISceneTransitionCompletionGate
    {
        private readonly ISceneTransitionCompletionGate _innerGate;
        private ISceneTransitionCompletionGate _levelFlowGate;

        public MacroLevelPrepareCompletionGate(ISceneTransitionCompletionGate innerGate)
        {
            _innerGate = innerGate ?? throw new ArgumentNullException(nameof(innerGate));
        }

        public void ConfigureLevelFlowGate(ISceneTransitionCompletionGate levelFlowGate)
        {
            _levelFlowGate = levelFlowGate ?? throw new ArgumentNullException(nameof(levelFlowGate));

            DebugUtility.LogVerbose<MacroLevelPrepareCompletionGate>(
                $"[SceneFlow] Gate de LevelFlow configurado ({levelFlowGate.GetType().Name}).",
                DebugUtility.Colors.Info);
        }

        public async Task AwaitBeforeFadeOutAsync(SceneTransitionContext context)
        {
            await _innerGate.AwaitBeforeFadeOutAsync(context);

            if (_levelFlowGate == null)
            {
                HardFailFastH1.Trigger(typeof(MacroLevelPrepareCompletionGate),
                    $"[FATAL][H1][SceneFlow] LevelFlow completion gate missing. routeId='{context.RouteId}' signature='{SceneTransitionSignature.Compute(context)}' reason='{context.Reason}'.");
            }

            await _levelFlowGate.AwaitBeforeFadeOutAsync(context);
        }
    }
}


