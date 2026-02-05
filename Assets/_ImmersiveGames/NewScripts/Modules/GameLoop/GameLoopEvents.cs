using _ImmersiveGames.NewScripts.Core.Events;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop
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
    /// Evento de alto nível para solicitar o encerramento da run.
    ///
    /// Este evento é a "entrada" recomendada em produção para vitória/derrota.
    /// Diferentes condições podem dispará-lo (timer, morte do player, objetivos, sequência de eventos etc.)
    /// sem amarrar a lógica de encerramento a um único sistema.
    /// </summary>
    public sealed class GameRunEndRequestedEvent : IEvent
    {
        public GameRunEndRequestedEvent(GameRunOutcome outcome, string reason = null)
        {
            Outcome = outcome;
            Reason = reason;
        }

        public GameRunOutcome Outcome { get; }
        public string Reason { get; }
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

    /// <summary>
    /// Evento de telemetria para mudanças de atividade do GameLoop.
    /// Permite que outros sistemas (UI, QA, etc.) observem quando o loop entra/sai de estados ativos.
    /// </summary>
    public sealed class GameLoopActivityChangedEvent : IEvent
    {
        public GameLoopActivityChangedEvent(GameLoopStateId currentStateId, bool isActive)
        {
            CurrentStateId = currentStateId;
            IsActive = isActive;
        }

        /// <summary>
        /// Estado atual do GameLoop após a mudança.
        /// </summary>
        public GameLoopStateId CurrentStateId { get; }

        /// <summary>
        /// Indica se o jogo está em um estado considerado "ativo" (ex.: Playing).
        /// </summary>
        public bool IsActive { get; }
    }

    /// <summary>
    /// Representa o início de uma nova run do jogo.
    /// É emitido quando o GameLoop entra em um estado de gameplay ativo (Playing).
    /// </summary>
    public sealed class GameRunStartedEvent : IEvent
    {
        public GameRunStartedEvent(GameLoopStateId stateId)
        {
            StateId = stateId;
        }

        /// <summary>
        /// Estado atual do GameLoop no momento em que a run é iniciada.
        /// </summary>
        public GameLoopStateId StateId { get; }
    }

    public sealed class GameResumeRequestedEvent : IEvent { }
    /// <summary>
    /// REQUEST (intenção): "quero sair do gameplay e voltar ao frontend/menu".
    /// </summary>
    public sealed class GameExitToMenuRequestedEvent : IEvent
    {
        public GameExitToMenuRequestedEvent(string reason = null)
        {
            Reason = NormalizeReason(reason);
        }

        public string Reason { get; }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "ExitToMenu/Unspecified" : reason.Trim();
        }
    }
    /// <summary>
    /// REQUEST (intenção): "reiniciar a run" (Restart).
    /// </summary>
    public sealed class GameResetRequestedEvent : IEvent
    {
        public GameResetRequestedEvent(string reason = null)
        {
            Reason = NormalizeReason(reason);
        }

        public string Reason { get; }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "Restart/Unspecified" : reason.Trim();
        }
    }
}
