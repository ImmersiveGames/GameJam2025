using _ImmersiveGames.NewProject.Infrastructure.Logging;

namespace _ImmersiveGames.NewProject.Infrastructure.Simulation
{
    /// <summary>
    /// Gate global para habilitar ou bloquear a simulação. Não cria nem controla cenas.
    /// </summary>
    public sealed class SimulationGate : ISimulationGate
    {
        private readonly ILogger _logger;

        public bool IsOpen { get; private set; } = true;

        public SimulationGate(ILogger logger)
        {
            _logger = logger;
        }

        public void Open()
        {
            IsOpen = true;
            _logger?.Log(LogLevel.Info, "Simulation gate aberto.");
        }

        public void Close(string reason = null)
        {
            IsOpen = false;
            var message = string.IsNullOrEmpty(reason) ? "Simulation gate fechado." : $"Simulation gate fechado: {reason}";
            _logger?.Log(LogLevel.Warning, message);
        }
    }
}
