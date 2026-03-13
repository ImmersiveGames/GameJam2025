namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain
{
    /// <summary>
    /// Reasons canÃ´nicas para reset do WorldLifecycle (evita strings mÃ¡gicas).
    /// </summary>
    public static class WorldResetReasons
    {
        public const string SceneFlowScenesReady = "SceneFlow/ScenesReady";

        public const string SkippedNonGameplayRoutePrefix = "Skipped_NonGameplayRoute";
        public const string FailedNoControllerPrefix = "Failed_NoController";
        public const string FailedNoResetService = "Failed_NoResetService";
        public const string GuardDuplicatePrefix = "Guard_DuplicateScenesReady";

        public const string ProductionTriggerPrefix = "ProductionTrigger/";
        public const string ManualProfile = "manual";
    }
}

