using System;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    public enum GameLoopStateId
    {
        Boot,
        Ready,
        /// <summary>
        /// Fase opcional antes do gameplay jogável (IntroStage/PostReveal).
        /// </summary>
        IntroStage,
        Playing,
        Paused,
        /// <summary>
        /// Estado pós-gameplay, após o fim da run (Game Over / Victory), antes de reiniciar ou sair para o menu.
        /// </summary>
        PostPlay
    }

    public interface IGameLoopSignals
    {
        bool StartRequested { get; }
        bool PauseRequested { get; }
        bool ResumeRequested { get; }
        bool ReadyRequested { get; }
        bool ResetRequested { get; }
        bool EndRequested { get; set; }
        bool IntroStageRequested { get; }
        bool IntroStageCompleted { get; }
    }

    public interface IGameLoopStateObserver
    {
        void OnStateEntered(GameLoopStateId stateId, bool isActive);
        void OnStateExited(GameLoopStateId stateId);
        void OnGameActivityChanged(bool isActive);
    }

    public interface IGameLoopService : IDisposable
    {
        void Initialize();
        void Tick(float dt);
        void RequestStart();
        void RequestPause();
        void RequestResume();
        void RequestReady();
        void RequestReset();
        void RequestEnd();
        void RequestIntroStageStart();
        void RequestIntroStageComplete();
        string CurrentStateIdName { get; }
    }

    public interface IGameRunStatusService
    {
        /// <summary>
        /// Indica se já existe um resultado registrado para a run atual/anterior.
        /// </summary>
        bool HasResult { get; }

        /// <summary>
        /// Resultado da última run finalizada.
        /// Se HasResult == false, deve ser GameRunOutcome.Unknown.
        /// </summary>
        GameRunOutcome Outcome { get; }

        /// <summary>
        /// Motivo textual do fim da run (ex.: "AllPlanetsDestroyed", "BossDefeated", "QA_ForcedEnd").
        /// Pode ser null.
        /// </summary>
        string Reason { get; }

        /// <summary>
        /// Limpa o estado, voltando para "sem resultado".
        /// </summary>
        void Clear();
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
