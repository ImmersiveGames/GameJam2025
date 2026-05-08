namespace _ImmersiveGames.NewScripts.RunLifecycle.Core
{
    public enum RunLifecycleStateId
    {
        Boot,
        Ready,
        Playing,
        Paused,
        /// <summary>
        /// Estado terminal técnico após o fim da run, antes de qualquer novo start ou reset.
        /// </summary>
        RunEnded
    }

    public interface IRunLifecycleSignals
    {
        bool StartRequested { get; }
        bool PauseRequested { get; }
        bool ResumeRequested { get; }
        bool ReadyRequested { get; }
        bool ResetRequested { get; }
        bool EndRequested { get; set; }
    }

    public interface IRunLifecycleStateObserver
    {
        void OnStateEntered(RunLifecycleStateId stateId, bool isActive);
        void OnStateExited(RunLifecycleStateId stateId);
        void OnGameActivityChanged(bool isActive);
    }

    /// <summary>
    /// Helpers canônicos para classificar o ciclo macro do RunLifecycle.
    /// Mantém a leitura dos estados explícita sem alterar os contratos públicos existentes.
    /// </summary>
    public static class RunLifecycleStateIdExtensions
    {
        public static bool IsPreGameplayState(this RunLifecycleStateId stateId)
            => stateId == RunLifecycleStateId.Boot || stateId == RunLifecycleStateId.Ready;

        public static bool IsActiveGameplayState(this RunLifecycleStateId stateId)
            => stateId == RunLifecycleStateId.Playing;

        public static bool IsPausedState(this RunLifecycleStateId stateId)
            => stateId == RunLifecycleStateId.Paused;

        public static bool IsTerminalRunState(this RunLifecycleStateId stateId)
            => stateId == RunLifecycleStateId.RunEnded;
    }

    /// <summary>
    /// Serviço de domínio para encerrar a run atual (vitória/derrota) de forma idempotente.
    ///
    /// Regras:
    /// - Publica <see cref="GameRunEndedEvent"/> no máximo uma vez por run.
    /// - Um novo <see cref="GameRunStartedEvent"/> deve rearmar o serviço para a próxima run.
    /// </summary>
    public interface IGameRunOutcomeService
    {
        /// <summary>
        /// Indica se o fim de run já foi solicitado/publicado para a run atual.
        /// </summary>
        bool HasEnded { get; }

        /// <summary>
        /// Tenta finalizar a run com o outcome informado.
        /// Retorna true quando o evento foi efetivamente publicado.
        /// </summary>
        bool TryEnd(GameRunOutcome outcome, string reason = null);

        /// <summary>
        /// Atalho para vitória.
        /// </summary>
        bool RequestVictory(string reason = null);

        /// <summary>
        /// Atalho para derrota.
        /// </summary>
        bool RequestDefeat(string reason = null);
    }
}

