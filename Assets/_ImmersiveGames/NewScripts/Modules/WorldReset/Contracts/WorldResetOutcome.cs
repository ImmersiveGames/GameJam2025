namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Contracts
{
    /// <summary>
    /// Resultado canônico observado ao concluir um WorldReset.
    /// </summary>
    public enum WorldResetOutcome
    {
        Completed = 0,
        SkippedByPolicy = 1,
        SkippedValidation = 2,
        FailedExecution = 3,
        FailedService = 4,
        FailedNoController = 5,
        SkippedInvalidContext = 6,
        Disposed = 7
    }
}
