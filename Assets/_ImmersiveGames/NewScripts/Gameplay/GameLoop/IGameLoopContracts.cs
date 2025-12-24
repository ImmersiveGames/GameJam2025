// (arquivo completo no download)
// Atualização: adiciona GameLoopStateId.Menu para suportar Boot → Menu → Playing → Paused.

using System;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    public enum GameLoopStateId
    {
        Boot,
        Menu,
        Playing,
        Paused
    }

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
}
