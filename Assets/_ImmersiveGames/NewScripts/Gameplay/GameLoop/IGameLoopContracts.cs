using System;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    public enum GameLoopStateId { Boot, Ready, Playing, Paused }

    public interface IGameLoopSignals
    {
        bool StartRequested { get; }
        bool PauseRequested { get; }
        bool ResumeRequested { get; }
        bool ResetRequested { get; }
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
        void RequestReset();
        string CurrentStateIdName { get; }
    }
}
