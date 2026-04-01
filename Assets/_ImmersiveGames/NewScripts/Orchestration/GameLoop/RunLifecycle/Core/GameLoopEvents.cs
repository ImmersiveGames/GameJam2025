using _ImmersiveGames.NewScripts.Core.Events;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core
{
    /// <summary>
    /// REQUEST (intencao): "quero iniciar a simulacao".
    /// Contrato canonico: usado apenas pelo boot/start-plan.
    /// </summary>
    public sealed class BootStartPlanRequestedEvent : IEvent { }

    /// <summary>
    /// REQUEST (intencao): "quero entrar em gameplay".
    /// Contrato canonico para intent de Play vinda de UI/Frontend.
    /// </summary>
    public sealed class GamePlayRequestedEvent : IEvent
    {
        public GamePlayRequestedEvent(string reason = null) => Reason = reason;
        public string Reason { get; }
    }

    /// <summary>
    /// Evento definitivo para pausa / despausa.
    /// </summary>
    public sealed class GamePauseCommandEvent : IEvent
    {
        public GamePauseCommandEvent(bool isPaused, string reason = null)
        {
            IsPaused = isPaused;
            Reason = reason;
        }

        public bool IsPaused { get; }
        public string Reason { get; }
    }

    /// <summary>
    /// Hook precoce para preparar sistemas quando a pausa vai entrar.
    /// </summary>
    public sealed class PauseWillEnterEvent : IEvent
    {
        public PauseWillEnterEvent(string reason = null) => Reason = reason;
        public string Reason { get; }
    }

    /// <summary>
    /// Hook precoce para preparar sistemas quando a pausa vai sair.
    /// </summary>
    public sealed class PauseWillExitEvent : IEvent
    {
        public PauseWillExitEvent(string reason = null) => Reason = reason;
        public string Reason { get; }
    }

    /// <summary>
    /// Hook oficial de observacao do estado canonico de pause.
    /// </summary>
    public sealed class PauseStateChangedEvent : IEvent
    {
        public PauseStateChangedEvent(bool isPaused) => IsPaused = isPaused;
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
    /// Evento de alto nivel para solicitar o encerramento da run.
    ///
    /// Este evento e a "entrada" recomendada em producao para vitoria/derrota.
    /// Diferentes condicoes podem dispara-lo (timer, morte do player, objetivos, sequencia de eventos etc.)
    /// sem amarrar a logica de encerramento a um unico sistema.
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
    /// Representa o fim da run atual do jogo, para orquestrar pos-gameplay.
    /// </summary>
    public sealed class GameRunEndedEvent : IEvent
    {
        public GameRunEndedEvent(GameRunOutcome outcome, string reason = null)
        {
            Outcome = outcome;
            Reason = reason;
        }

        /// <summary>
        /// Resultado da run (vitoria/derrota).
        /// </summary>
        public GameRunOutcome Outcome { get; }

        /// <summary>
        /// Texto livre para logs (ex.: "AllPlanetsDestroyed", "BossDefeated", "QA_ForcedEnd").
        /// </summary>
        public string Reason { get; }
    }

    /// <summary>
    /// Evento de telemetria para mudancas de atividade do GameLoop.
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
        /// Estado atual do GameLoop apos a mudanca.
        /// </summary>
        public GameLoopStateId CurrentStateId { get; }

        /// <summary>
        /// Indica se o jogo esta em um estado considerado "ativo" (ex.: Playing).
        /// </summary>
        public bool IsActive { get; }
    }

    public sealed class GameRunStartedEvent : IEvent
    {
        public GameRunStartedEvent(GameLoopStateId stateId)
        {
            StateId = stateId;
        }

        /// <summary>
        /// Estado atual do GameLoop no momento em que a run e iniciada.
        /// </summary>
        public GameLoopStateId StateId { get; }
    }

    public sealed class GameResumeRequestedEvent : IEvent
    {
        public GameResumeRequestedEvent(string reason = null) => Reason = reason;
        public string Reason { get; }
    }

    /// <summary>
    /// REQUEST (intencao): "reiniciar a run" (Restart).
    /// </summary>
    public sealed class GameResetRequestedEvent : IEvent
    {
        public GameResetRequestedEvent(string reason = null)
        {
            Reason = GameLoopReasonFormatter.NormalizeOptional(reason, "Restart/Unspecified");
        }

        public string Reason { get; }
    }
}
