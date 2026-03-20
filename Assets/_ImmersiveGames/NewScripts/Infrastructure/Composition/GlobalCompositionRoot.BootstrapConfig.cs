using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static bool _bootstrapConfigResolutionAttempted;
        private static bool _bootstrapConfigResolutionLogged;
        private static NewScriptsBootstrapConfigAsset _cachedBootstrapConfig;
        private static string _cachedBootstrapConfigVia = "None";
        private static bool _fatalAbortRequested;

        private static void FailFast(string message)
        {
            _fatalAbortRequested = true;

            DebugUtility.LogError(typeof(GlobalCompositionRoot), $"[FATAL][Config] {message}");
            StopPlayModeOrQuit();

            throw new InvalidOperationException($"[FATAL][Config] {message}");
        }

        private static void StopPlayModeOrQuit()
        {
            RequestEditorStopPlayMode();

            if (!Application.isEditor)
            {
                Application.Quit();
            }
        }

        private static void RequestEditorStopPlayMode()
        {
            throw new NotImplementedException();
        }

        private static NewScriptsBootstrapConfigAsset GetRequiredBootstrapConfig(out string via)
        {
            if (!_bootstrapConfigResolutionAttempted)
            {
                _bootstrapConfigResolutionAttempted = true;

                if (DependencyManager.Provider.TryGetGlobal<NewScriptsBootstrapConfigAsset>(out var diConfig) && diConfig != null)
                {
                    _cachedBootstrapConfig = diConfig;
                    _cachedBootstrapConfigVia = "DI";
                }
                else
                {
                    ResolveBootstrapConfigFromRuntimeModeConfigOrFail();
                    DependencyManager.Provider.RegisterGlobal(_cachedBootstrapConfig, allowOverride: false);
                }
            }

            via = _cachedBootstrapConfigVia;

            if (!_bootstrapConfigResolutionLogged)
            {
                _bootstrapConfigResolutionLogged = true;
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] BootstrapConfigResolvedVia={via} asset={_cachedBootstrapConfig.name}",
                    DebugUtility.Colors.Info);
            }

            if (_cachedBootstrapConfig == null || _fatalAbortRequested)
            {
                throw new InvalidOperationException("[FATAL][Config] Bootstrap config resolution aborted.");
            }

            return _cachedBootstrapConfig;
        }

        private static void ResolveBootstrapConfigFromRuntimeModeConfigOrFail()
        {
            RuntimeModeConfig runtimeModeConfig = RuntimeModeConfigLoader.LoadOrNull();
            if (runtimeModeConfig == null)
            {
                FailFast(
                    "Missing required RuntimeModeConfig. Create/place RuntimeModeConfig in Resources (path='RuntimeModeConfig').");
            }

            if (runtimeModeConfig.NewScriptsBootstrapConfig == null)
            {
                FailFast("RuntimeModeConfig.NewScriptsBootstrapConfig is null. Assign a valid NewScriptsBootstrapConfigAsset.");
            }

            _cachedBootstrapConfig = runtimeModeConfig.NewScriptsBootstrapConfig;
            _cachedBootstrapConfigVia = "RuntimeModeConfig";
        }
    }
}
