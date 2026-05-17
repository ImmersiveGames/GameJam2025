using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SaveRuntime.Authoring;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    public static class SaveRuntimeConfigResolver
    {
        public static SaveConfigAsset ResolveSaveConfigOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SaveRuntime] RuntimeModeConfig obrigatorio ausente para resolver SaveConfig.");
            }

            if (!RuntimeConfigRegistry.TryGetSnapshot(out IRuntimeConfigSnapshotReadOnly snapshot) || snapshot == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SaveRuntime] RuntimeConfigRegistry snapshot obrigatorio ausente para SaveConfig migrado.");
            }

            ISaveRuntimeConfigGroupReadOnly saveRuntime = snapshot.SaveRuntime
                ?? throw new InvalidOperationException("[FATAL][Config][SaveRuntime] RuntimeConfigRegistry invariant breach: snapshot.SaveRuntime obrigatorio ausente.");

            SaveConfigAsset saveConfig = saveRuntime.SaveConfig
                ?? throw new InvalidOperationException("[FATAL][Config][SaveRuntime] RuntimeConfigRegistry invariant breach: SaveConfig obrigatorio ausente no snapshot.");

            try
            {
                saveConfig.ValidateOrThrow();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SaveRuntime] RuntimeConfigRegistry invariant breach: SaveConfig invalido no snapshot. detail='{ex.Message}'.");
            }

            DebugUtility.Log(typeof(SaveRuntimeConfigResolver),
                $"[OBS][SaveRuntime][Config] SaveConfig resolved via RuntimeConfigRegistry. asset='{saveConfig.name}'.",
                DebugUtility.Colors.Info);

            return saveConfig;
        }
    }
}
