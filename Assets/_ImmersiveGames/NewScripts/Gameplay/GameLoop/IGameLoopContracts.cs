namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// IDs dos estados da GameLoop.
    ///
    /// Nota:
    /// - Mantemos os valores existentes para evitar quebra de possíveis usos que dependam do int do enum.
    /// - O fluxo alvo deste projeto é Boot -> Menu -> Playing -> Paused (mas pode ser estendido).
    /// </summary>
    public enum GameLoopStateId
    {
        Boot = 0,
        Playing = 1,
        Paused = 2,

        // Adicionado depois para preservar os ints anteriores.
        Menu = 3
    }

    /// <summary>
    /// Sinais lidos pela FSM para decidir transições (inputs "transientes" do loop).
    /// </summary>
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
