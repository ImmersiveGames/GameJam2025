using System;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Core
{
    public enum GameLoopStateId
    {
        Boot,
        Ready,
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
        void RequestSceneFlowCompletionSync(SceneRouteKind routeKind);
        void RequestReset();
        void RequestRunEnd();
        string CurrentStateIdName { get; }
    }

    public interface IPauseStateService
    {
        bool IsPaused { get; }
    }

    /// <summary>
    /// Policy compartilhada para validar se o GameLoop está em gameplay ativo.
    /// Centraliza a regra de "Playing" para serviços de run.
    /// </summary>
    public interface IGameRunPlayingStateGuard
    {
        /// <summary>
        /// Retorna true quando o GameLoop está em gameplay ativo (Playing).
        /// Também devolve o nome atual do estado para logs/diagnóstico.
        /// </summary>
        bool IsInActiveGameplay(out string stateName);
    }

    public interface IGameRunResultSnapshotService
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
        void ClearSnapshot();
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
