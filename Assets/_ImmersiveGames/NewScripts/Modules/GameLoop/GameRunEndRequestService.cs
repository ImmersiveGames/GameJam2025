// Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameRunEndRequestService.cs

using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop
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
        public void RequestEnd(GameRunOutcome outcome, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                // Mantém o fluxo funcional sem impor enumerações/contratos adicionais neste estágio.
                reason = "Unspecified";
            }

            DebugUtility.LogVerbose(typeof(GameRunEndRequestService),
                "RequestEnd(" + outcome + ", reason='" + reason + "')");

            EventBus<GameRunEndRequestedEvent>.Raise(new GameRunEndRequestedEvent(outcome, reason));
        }
    }
}
