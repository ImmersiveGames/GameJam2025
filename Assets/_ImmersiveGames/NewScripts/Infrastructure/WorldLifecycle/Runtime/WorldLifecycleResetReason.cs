using System;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime
{
    /// <summary>
    /// Strings padronizadas para o campo Reason do WorldLifecycleResetCompletedEvent.
    ///
    /// Regra: manter compatibilidade com o Baseline 2.0 (logs/spec) — alterar formato aqui
    /// implica atualizar evidências e regras de matching.
    /// </summary>
    public static class WorldLifecycleResetReason
    {
        // Prefixos canônicos (congelados por evidência).
        public const string PrefixScenesReady = "ScenesReady/";
        public const string PrefixSkippedStartupOrFrontend = "Skipped_StartupOrFrontend:";
        public const string PrefixFailedNoController = "Failed_NoController:";
        public const string PrefixFailedReset = "Failed_Reset:";
        public const string PrefixProductionTrigger = "ProductionTrigger/";

        public static string ScenesReadyFor(string activeSceneName)
        {
            var safeScene = NormalizeSceneName(activeSceneName);
            return $"{PrefixScenesReady}{safeScene}";
        }

        public static string SkippedStartupOrFrontend(string profile, string activeSceneName)
        {
            var safeProfile = NormalizeProfile(profile);
            var safeScene = NormalizeSceneName(activeSceneName);
            return $"{PrefixSkippedStartupOrFrontend}profile={safeProfile};scene={safeScene}";
        }

        public static string SkippedStartupOrFrontend(SceneFlowProfileId profileId, string activeSceneName)
        {
            return SkippedStartupOrFrontend(profileId.Value, activeSceneName);
        }

        public static string FailedNoController(string activeSceneName)
        {
            var safeScene = NormalizeSceneName(activeSceneName);
            return $"{PrefixFailedNoController}{safeScene}";
        }

        public static string FailedReset(string exceptionTypeName)
        {
            var safeType = string.IsNullOrWhiteSpace(exceptionTypeName) ? "<unknown>" : exceptionTypeName.Trim();
            return $"{PrefixFailedReset}{safeType}";
        }

        public static string ProductionTrigger(string source)
        {
            var safeSource = string.IsNullOrWhiteSpace(source) ? "<unspecified>" : source.Trim();
            return $"{PrefixProductionTrigger}{safeSource}";
        }

        private static string NormalizeProfile(string profile)
        {
            var normalized = SceneFlowProfileId.Normalize(profile);
            return string.IsNullOrEmpty(normalized) ? "<null>" : normalized;
        }

        private static string NormalizeSceneName(string sceneName)
        {
            return string.IsNullOrWhiteSpace(sceneName) ? "<unknown>" : sceneName.Trim();
        }
    }
}
