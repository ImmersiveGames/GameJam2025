using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Modules.Preferences.Contracts;

namespace _ImmersiveGames.NewScripts.Modules.Preferences.Bootstrap
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

            if (!stateService.HasSnapshot)
            {
                throw new InvalidOperationException("[FATAL][Preferences] Snapshot indisponivel apos bootstrap.");
            }

            _runtimeComposed = true;

            DebugUtility.Log(typeof(PreferencesBootstrap),
                $"[Preferences] Runtime preparation concluded. backend='{saveService.BackendId}' snapshot={stateService.CurrentSnapshot}.",
                DebugUtility.Colors.Info);
        }
    }
}
