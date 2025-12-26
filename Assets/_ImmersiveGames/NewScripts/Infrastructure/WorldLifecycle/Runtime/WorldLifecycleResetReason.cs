namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime
{
    /// <summary>
    /// Strings padronizadas para o campo Reason do WorldLifecycleResetCompletedEvent.
    /// Mantém compatibilidade total com os logs/testes atuais.
    /// </summary>
    public static class WorldLifecycleResetReason
    {
        public const string SkippedStartupOrFrontend = "Skipped_StartupOrFrontend";
        public const string FailedNoController = "Failed_NoController";

        public static string ScenesReadyFor(string activeSceneName)
        {
            return $"ScenesReady/{activeSceneName}";
        }

        public static string FailedReset(string exceptionTypeName)
        {
            return $"Failed_Reset:{exceptionTypeName}";
        }
    }
}
