using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunOutcome
{
    /// <summary>
    /// Implementacao padrao de <see cref="IGameRunEndRequestService"/>.
    ///
    /// Responsabilidade:
    /// - Publicar <see cref="GameRunEndRequestedEvent"/>.
    /// - Nao decide condicoes, nao executa transicoes e nao aplica pause.
    ///
    /// Regras:
    /// - E intencionalmente "thin" para manter o modelo event-driven do projeto.
    /// </summary>
    public sealed class GameRunEndRequestService : IGameRunEndRequestService
    {
        public void RequestRunEnd(GameRunOutcome outcome, string reason)
        {
            string normalizedReason = GameLoopReasonFormatter.NormalizeRequired(reason);

            DebugUtility.LogVerbose(typeof(GameRunEndRequestService),
                "[OBS][GameLoop][Operational] GameRunEndRequestedEvent requested outcome='" + outcome + "' reason='" + normalizedReason + "'.");

            EventBus<GameRunEndRequestedEvent>.Raise(new GameRunEndRequestedEvent(outcome, normalizedReason));
        }
    }
}
