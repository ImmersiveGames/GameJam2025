using System;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
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
        void RequestReset();
        void RequestEnd();
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
}
