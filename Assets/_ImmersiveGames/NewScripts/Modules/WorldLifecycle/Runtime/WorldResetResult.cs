namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    /// <summary>
    /// Resultado explícito da execução do pipeline de reset.
    /// </summary>
    public enum WorldResetResult
    {
        Completed = 0,
        SkippedValidation = 1,
        Failed = 2
    }
}
