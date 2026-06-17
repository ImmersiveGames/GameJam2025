using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    public static class RuntimeConfigRegistry
    {
        private static RuntimeConfigSnapshot _snapshot;

        public static bool IsInitialized => _snapshot != null;
        public static IRuntimeConfigSnapshotReadOnly Snapshot => _snapshot;

        public static void ValidateOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            _ = ResolveConfigSetOrFail(runtimeModeConfig);
        }

        public static void InitializeOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            var configSet = ResolveConfigSetOrFail(runtimeModeConfig);
            _snapshot = new RuntimeConfigSnapshot(configSet);

            DebugUtility.LogVerbose(typeof(RuntimeConfigRegistry),
                $"[RuntimeConfigRegistry] initialized sourceAsset='{configSet.name}'.",
                DebugUtility.Colors.Info);
        }

        public static bool TryGetSnapshot(out IRuntimeConfigSnapshotReadOnly snapshot)
        {
            snapshot = _snapshot;
            return snapshot != null;
        }

        public static void ResetForTests()
        {
            _snapshot = null;
        }

        private static RuntimeConfigSetAsset ResolveConfigSetOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][RuntimeConfigRegistry] RuntimeModeConfig obrigatorio ausente.");
            }

            var configSet = runtimeModeConfig.RuntimeConfigSet;
            if (configSet == null)
            {
                string message =
                    $"[FATAL][Config][RuntimeConfigRegistry] RuntimeConfigSetAsset obrigatorio ausente em RuntimeModeConfig. runtimeModeConfig='{runtimeModeConfig.name}'.";
                DebugUtility.LogError(typeof(RuntimeConfigRegistry), message);
                throw new InvalidOperationException(message);
            }

            if (!configSet.TryValidate(out string validationError))
            {
                string message =
                    $"[FATAL][Config][RuntimeConfigRegistry] RuntimeConfigSetAsset invalido. runtimeModeConfig='{runtimeModeConfig.name}' configSet='{configSet.name}' detail='{validationError}'.";
                DebugUtility.LogError(typeof(RuntimeConfigRegistry), message);
                throw new InvalidOperationException(message);
            }

            return configSet;
        }
    }
}
