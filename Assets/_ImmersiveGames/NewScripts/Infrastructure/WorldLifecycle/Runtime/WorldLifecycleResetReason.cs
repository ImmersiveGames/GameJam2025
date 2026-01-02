using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime
{
    /// <summary>
    /// Strings padronizadas para o campo Reason do WorldLifecycleResetCompletedEvent.
    /// Mantém compatibilidade total com os logs/testes atuais.
    /// </summary>
    public static class WorldLifecycleResetReason
    {

        public static string ScenesReadyFor(string activeSceneName)
        {
            return $"ScenesReady/{activeSceneName}";
        }

        public static string SkippedStartupOrFrontend(string profile, string activeSceneName)
        {
            var safeProfile = string.IsNullOrWhiteSpace(profile) ? "<null>" : profile;
            var safeScene = string.IsNullOrWhiteSpace(activeSceneName) ? "<unknown>" : activeSceneName;
            return $"Skipped_StartupOrFrontend:profile={safeProfile};scene={safeScene}";
        }

        public static string SkippedStartupOrFrontend(SceneFlowProfileId profileId, string activeSceneName)
        {
            return SkippedStartupOrFrontend(profileId.Value, activeSceneName);
        }

        public static string FailedNoController(string activeSceneName)
        {
            var safeScene = string.IsNullOrWhiteSpace(activeSceneName) ? "<unknown>" : activeSceneName;
            return $"Failed_NoController:{safeScene}";
        }

        public static string FailedReset(string exceptionTypeName)
        {
            return $"Failed_Reset:{exceptionTypeName}";
        }
    }
}
