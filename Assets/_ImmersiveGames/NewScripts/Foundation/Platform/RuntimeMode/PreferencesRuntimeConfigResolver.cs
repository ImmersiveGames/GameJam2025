using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Config;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    public static class PreferencesRuntimeConfigResolver
    {
        public static AudioDefaultsAsset ResolveAudioDefaultsOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            var preferencesRuntime = ResolvePreferencesRuntimeGroupOrFail(runtimeModeConfig);

            var audioDefaults = preferencesRuntime.AudioDefaults
                ?? throw new InvalidOperationException("[FATAL][Config][PreferencesRuntime] RuntimeConfigRegistry invariant breach: AudioDefaults obrigatorio ausente no snapshot.");

            DebugUtility.LogVerbose(typeof(PreferencesRuntimeConfigResolver),
                $"AudioDefaults resolved via RuntimeConfigRegistry. asset='{audioDefaults.name}'.",
                DebugUtility.Colors.Info);

            return audioDefaults;
        }

        public static VideoDefaultsAsset ResolveVideoDefaultsOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            var preferencesRuntime = ResolvePreferencesRuntimeGroupOrFail(runtimeModeConfig);

            var videoDefaults = preferencesRuntime.VideoDefaults
                ?? throw new InvalidOperationException("[FATAL][Config][PreferencesRuntime] RuntimeConfigRegistry invariant breach: VideoDefaults obrigatorio ausente no snapshot.");

            DebugUtility.LogVerbose(typeof(PreferencesRuntimeConfigResolver),
                $"VideoDefaults resolved via RuntimeConfigRegistry. asset='{videoDefaults.name}'.",
                DebugUtility.Colors.Info);

            return videoDefaults;
        }

        private static IPreferencesRuntimeConfigGroupReadOnly ResolvePreferencesRuntimeGroupOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PreferencesRuntime] RuntimeModeConfig obrigatorio ausente para resolver defaults.");
            }

            if (!RuntimeConfigRegistry.TryGetSnapshot(out var snapshot) || snapshot == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PreferencesRuntime] RuntimeConfigRegistry snapshot obrigatorio ausente para PreferencesRuntime defaults migrados.");
            }

            return snapshot.PreferencesRuntime
                ?? throw new InvalidOperationException("[FATAL][Config][PreferencesRuntime] RuntimeConfigRegistry invariant breach: snapshot.PreferencesRuntime obrigatorio ausente.");
        }
    }
}
