namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle
{
    /// <summary>
    /// Assinaturas-base estáveis para observabilidade de fases do WorldLifecycle (Marco 0).
    /// </summary>
    public static class WorldLifecyclePhaseObservabilitySignatures
    {
        public const string PhaseRequested = "[OBS][Phase] PhaseRequested";
        public const string ResetRequested = "[OBS][Phase] ResetRequested";
        public const string PreRevealStarted = "[OBS][Phase] PreRevealStarted";
        public const string PreRevealCompleted = "[OBS][Phase] PreRevealCompleted";
        public const string PreRevealSkipped = "[OBS][Phase] PreRevealSkipped";
        public const string PreRevealTimeout = "[OBS][Phase] PreRevealTimeout";
    }
}
