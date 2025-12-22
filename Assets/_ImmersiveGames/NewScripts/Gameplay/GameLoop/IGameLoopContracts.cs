namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Contrato mínimo para sinalização de transições do GameLoop.
    /// </summary>
    public interface IGameLoopSignals
    {
        bool StartRequested { get; }
        bool PauseRequested { get; }
        bool ResumeRequested { get; }
        bool ResetRequested { get; }
    }

    /// <summary>
    /// Observador opcional para receber callbacks de mudança de estado.
    /// </summary>
    public interface IGameLoopStateObserver
    {
        void OnStateEntered(GameLoopStateId stateId, bool isActive);
        void OnStateExited(GameLoopStateId stateId);
        void OnGameActivityChanged(bool isActive);
    }

    /// <summary>
    /// Identificadores de estado utilizados pela FSM concreta.
    /// </summary>
    public enum GameLoopStateId
    {
        Boot = 0,
        Playing = 1,
        Paused = 2
    }
}
