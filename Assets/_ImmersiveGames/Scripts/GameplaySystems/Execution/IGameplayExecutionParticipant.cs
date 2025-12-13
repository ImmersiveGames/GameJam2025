namespace _ImmersiveGames.Scripts.GameplaySystems.Execution
{
    /// <summary>
    /// Contrato para qualquer elemento que participa da execução de gameplay.
    /// O Coordinator chama SetExecutionAllowed(false) quando o Gate fecha,
    /// e SetExecutionAllowed(true) quando o Gate abre.
    /// </summary>
    public interface IGameplayExecutionParticipant
    {
        bool IsExecutionAllowed { get; }
        void SetExecutionAllowed(bool allowed);
    }
}