namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset.Domain
{
    /// <summary>
    /// Reasons canônicas para reset do WorldLifecycle (evita strings mágicas).
    /// </summary>
    public static class WorldResetReasons
    {
        public const string SceneFlowScenesReady = "SceneFlow/ScenesReady";

        public const string SkippedStartupOrFrontendPrefix = "Skipped_StartupOrFrontend";
        public const string FailedNoControllerPrefix = "Failed_NoController";
        public const string FailedNoResetService = "Failed_NoResetService";
        public const string GuardDuplicatePrefix = "Guard_DuplicateScenesReady";

        public const string ProductionTriggerPrefix = "ProductionTrigger/";
        public const string ManualProfile = "manual";
    }
}
