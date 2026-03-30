namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Domain
{
    /// <summary>
    /// Reasons canônicas para reset do WorldReset (evita strings mágicas).
    /// </summary>
    public static class WorldResetReasons
    {
        public const string SceneFlowScenesReady = "SceneFlow/ScenesReady";

        public const string SkippedNonGameplayRoutePrefix = "Skipped_NonGameplayRoute";
        public const string FailedNoLocalExecutorPrefix = "Failed_NoLocalExecutor";
        public const string FailedNoResetService = "Failed_NoResetService";
        public const string FailedExecutionPrefix = "Failed_Execution";
        public const string FailedServiceExceptionPrefix = "Failed_ServiceException";
        public const string GuardDuplicatePrefix = "Guard_DuplicateScenesReady";
        public const string GateDisposed = "Gate_Disposed";

        public const string ProductionTriggerPrefix = "ProductionTrigger/";
        public const string ManualProfile = "manual";
    }
}
