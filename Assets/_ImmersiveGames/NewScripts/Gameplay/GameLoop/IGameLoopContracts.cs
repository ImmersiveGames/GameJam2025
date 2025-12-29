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
        string CurrentStateIdName { get; }
    }
}
