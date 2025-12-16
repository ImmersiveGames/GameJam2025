namespace _ImmersiveGames.NewProject.Infrastructure.Simulation
{
    /// <summary>
    /// Controla a abertura/fechamento da simulação global, sem criar cenas ou atores.
    /// </summary>
    public interface ISimulationGate
    {
        bool IsOpen { get; }
        void Open();
        void Close(string reason = null);
    }
}
