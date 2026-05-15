using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.PreferencesRuntime.Contracts;
namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Bootstrap
{
    public static class PreferencesBootstrap
    {
        private static bool _runtimeComposed;

        public static void ComposeRuntime(RuntimeModeConfig runtimeModeConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(PreferencesBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            _ = runtimeModeConfig;

            if (!DependencyManager.Provider.TryGetGlobal<IPreferencesStateService>(out var stateService) || stateService == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesStateService obrigatorio ausente para bootstrap.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPreferencesRuntimePipeline>(out var runtimePipeline) || runtimePipeline == null)
            {
                throw new InvalidOperationException("[FATAL][Preferences] IPreferencesRuntimePipeline obrigatorio ausente para bootstrap.");
            }

            runtimePipeline.RequestBootstrapLoadAudio("Preferences/BootstrapLoadAudio");
            runtimePipeline.RequestBootstrapLoadVideo("Preferences/BootstrapLoadVideo");

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
                $"[Preferences] Runtime preparation concluded. audio={stateService.CurrentSnapshot} video={stateService.CurrentVideoSnapshot}.",
                DebugUtility.Colors.Info);
        }
    }
}

