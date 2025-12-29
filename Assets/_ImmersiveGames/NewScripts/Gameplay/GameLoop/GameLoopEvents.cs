using System;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// REQUEST (intenção): "quero iniciar a simulação".
    /// O start definitivo é o COMMAND executado pelo Coordinator via IGameLoopService.RequestStart() quando ready.
    /// </summary>
    public sealed class GameStartRequestedEvent : IEvent { }

    /// <summary>
    /// Evento definitivo para pausa / despausa.
    /// </summary>
    public sealed class GamePauseCommandEvent : IEvent
    {
        public GamePauseCommandEvent(bool isPaused) => IsPaused = isPaused;
        public bool IsPaused { get; }
    }

    /// <summary>
    /// Resultado final da run atual.
    /// </summary>
    public enum GameRunOutcome
    {
        Unknown = 0,
        Victory = 1,
        Defeat = 2,
    }

    /// <summary>
    /// Representa o fim da run atual do jogo, para orquestrar pós-gameplay.
    /// </summary>
    public sealed class GameRunEndedEvent : IEvent
    {
        public GameRunEndedEvent(GameRunOutcome outcome, string reason = null)
        {
            Outcome = outcome;
            Reason = reason;
        }

        /// <summary>
        /// Resultado da run (vitória/derrota).
        /// </summary>
        public GameRunOutcome Outcome { get; }

        /// <summary>
        /// Texto livre para logs (ex.: "AllPlanetsDestroyed", "BossDefeated", "QA_ForcedEnd").
        /// </summary>
        public string Reason { get; }
    }

    public sealed class GameResumeRequestedEvent : IEvent { }
    /// <summary>
    /// REQUEST (intenção): "quero sair do gameplay e voltar ao frontend/menu".
    /// </summary>
    public sealed class GameExitToMenuRequestedEvent : IEvent { }
    public sealed class GameResetRequestedEvent : IEvent { }
}
