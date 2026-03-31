using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Preferences.Contracts;
namespace _ImmersiveGames.NewScripts.Experience.Preferences.Bootstrap
{
    public static class PreferencesBootstrap
    {
        private static bool _runtimeComposed;

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(PreferencesBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            _ = bootstrapConfig;

            if (!DependencyManager.Provider.TryGetGlobal<IPreferencesStateService>(out var stateService) || stateService == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesStateService obrigatorio ausente para bootstrap.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPreferencesSaveService>(out var saveService) || saveService == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesSaveService obrigatorio ausente para bootstrap.");
            }

            DebugUtility.LogVerbose(typeof(PreferencesBootstrap),
                $"[Preferences] load requested. backend='{saveService.BackendId}' profile='{AudioPreferencesSnapshot.BootstrapProfileId}' slot='{AudioPreferencesSnapshot.BootstrapSlotId}'.",
                DebugUtility.Colors.Info);

            bool loaded = saveService.TryLoad(
                AudioPreferencesSnapshot.BootstrapProfileId,
                AudioPreferencesSnapshot.BootstrapSlotId,
                out var loadedSnapshot,
                out string loadReason);

            if (loaded && loadedSnapshot != null)
            {
                stateService.SetCurrent(loadedSnapshot, "Preferences/BootstrapLoad");
            }
            else
            {
                DebugUtility.LogVerbose(typeof(PreferencesBootstrap),
                    $"[Preferences] bootstrap kept installer seed. backend='{saveService.BackendId}' reason='{loadReason}'.",
                    DebugUtility.Colors.Info);
            }

            DebugUtility.LogVerbose(typeof(PreferencesBootstrap),
                $"[Preferences] video load requested. backend='{saveService.BackendId}' profile='{VideoPreferencesSnapshot.BootstrapProfileId}' slot='{VideoPreferencesSnapshot.BootstrapSlotId}'.",
                DebugUtility.Colors.Info);

            bool videoLoaded = saveService.TryLoadVideo(
                VideoPreferencesSnapshot.BootstrapProfileId,
                VideoPreferencesSnapshot.BootstrapSlotId,
                out var loadedVideoSnapshot,
                out string videoLoadReason);

            if (videoLoaded && loadedVideoSnapshot != null)
            {
                stateService.SetCurrent(loadedVideoSnapshot, "Preferences/BootstrapLoad");
            }
            else
            {
                DebugUtility.LogVerbose(typeof(PreferencesBootstrap),
                    $"[Preferences] bootstrap kept installer seed for video. backend='{saveService.BackendId}' reason='{videoLoadReason}'.",
                    DebugUtility.Colors.Info);
            }

            stateService.ApplyCurrentVideoToRuntime("Preferences/BootstrapApply");

            if (!stateService.HasSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Snapshot indisponivel apos bootstrap.");
            }

            if (!stateService.HasVideoSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Video snapshot indisponivel apos bootstrap.");
            }

            _runtimeComposed = true;

            DebugUtility.Log(typeof(PreferencesBootstrap),
                $"[Preferences] Runtime preparation concluded. backend='{saveService.BackendId}' audio={stateService.CurrentSnapshot} video={stateService.CurrentVideoSnapshot}.",
                DebugUtility.Colors.Info);
        }
    }
}
