using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core
{
    /// <summary>
    /// Efeitos auxiliares do GameLoop.
    ///
    /// Mantém apenas a projeção de input mode do estado Playing.
    /// O handoff de pós-run pertence ao rail canônico de RunResultStage/RunDecision.
    /// </summary>
    public sealed class GameLoopStateTransitionEffects
    {
        public void ApplyGameplayInputMode()
        {
            DebugUtility.Log<GameLoopStateTransitionEffects>(
                "[OBS][InputMode] Request mode='Gameplay' map='Player' phase='Playing' reason='GameLoop/Playing'.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(InputModeRequestKind.Gameplay, "GameLoop/Playing", "GameLoop"));
        }
    }
}

