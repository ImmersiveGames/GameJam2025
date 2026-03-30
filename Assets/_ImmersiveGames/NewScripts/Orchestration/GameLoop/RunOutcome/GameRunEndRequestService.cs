// Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameRunEndRequestService.cs

using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunOutcome
{
    /// <summary>
    /// Implementação padrão de <see cref="IGameRunEndRequestService"/>.
    ///
    /// Responsabilidade:
    /// - Publicar <see cref="GameRunEndRequestedEvent"/>.
    /// - NÃO decide condições, não executa transições e não aplica pause.
    ///
    /// Regras:
    /// - É intencionalmente "thin" para manter o modelo event-driven do projeto.
    /// </summary>
    public sealed class GameRunEndRequestService : IGameRunEndRequestService
    {
        public void RequestRunEnd(GameRunOutcome outcome, string reason)
        {
            string normalizedReason = GameLoopReasonFormatter.NormalizeRequired(reason);

            DebugUtility.LogVerbose(typeof(GameRunEndRequestService),
                "[OBS][ExitStage] GameRunEndRequestedEvent requested outcome='" + outcome + "' reason='" + normalizedReason + "'.");

            EventBus<GameRunEndRequestedEvent>.Raise(new GameRunEndRequestedEvent(outcome, normalizedReason));
        }
    }
}
