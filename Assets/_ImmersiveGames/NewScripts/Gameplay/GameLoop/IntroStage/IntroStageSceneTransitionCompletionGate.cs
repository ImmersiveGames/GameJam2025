#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Gate legado/descontinuado: NÃO deve executar IntroStage durante a transição de cena.
    /// 
    /// ADR-0016 define IntroStage como etapa PostReveal (após SceneTransitionCompletedEvent),
    /// para não prender FadeOut/Completed nem "rodar com cortina fechada".
    ///
    /// Mantido apenas para evitar que referências antigas quebrem compilação.
    /// </summary>
    [Obsolete("Descontinuado (ADR-0016): não registrar IntroStage como ISceneTransitionCompletionGate; executar PostReveal (Completed).", false)]
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
            // Sempre respeita o gate interno (ex.: ResetCompletionGate).
            await _innerGate.AwaitBeforeFadeOutAsync(context);

            // Não executar IntroStage aqui. Se alguém registrar este gate por engano, o log denuncia a regressão.
            DebugUtility.LogWarning<IntroStageSceneTransitionCompletionGate>(
                "[SceneFlowGate] IntroStage no completion gate foi descontinuado (ADR-0016). " +
                "Ignorando execução; use PostReveal via SceneTransitionCompletedEvent (InputModeSceneFlowBridge).");
        }
    }
}
