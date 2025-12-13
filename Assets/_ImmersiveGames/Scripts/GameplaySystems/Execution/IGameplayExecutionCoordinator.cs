namespace _ImmersiveGames.Scripts.GameplaySystems.Execution
{
    /// <summary>
    /// Contrato para o orquestrador de execução (por cena).
    /// Participantes se registram/desregistram automaticamente.
    /// </summary>
    public interface IGameplayExecutionCoordinator
    {
        bool IsExecutionAllowed { get; }

        void Register(IGameplayExecutionParticipant participant);
        void Unregister(IGameplayExecutionParticipant participant);
    }
    /// <summary>
    /// Marker interface: Behaviours que implementam essa interface nunca serão incluídos
    /// na auto-coleta do GameplayExecutionParticipantBehaviour.
    ///
    /// Use para infra: AnimationControllers, registradores, binders, etc.
    /// </summary>
    public interface IExecutionToggleIgnored
    {
    }
}