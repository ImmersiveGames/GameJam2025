using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core
{
    /// <summary>
    /// Efeitos auxiliares do GameLoop.
    ///
    /// Mantem apenas a observabilidade do estado Playing; InputMode Gameplay pertence ao rail operacional de ActorsExecution.
    /// O handoff de pos-run pertence ao rail canonico de RunResultStage/RunDecision.
    /// </summary>
    public sealed class GameLoopStateTransitionEffects
    {
        public void ApplyGameplayInputMode()
        {
            DebugUtility.Log<GameLoopStateTransitionEffects>(
                "[OBS][InputMode] Gameplay input request skipped owner='ActorsExecution' phase='Playing' reason='GameLoop/Playing' detail='GameLoop state is not operational input readiness'.",
                DebugUtility.Colors.Info);
        }
    }
}
