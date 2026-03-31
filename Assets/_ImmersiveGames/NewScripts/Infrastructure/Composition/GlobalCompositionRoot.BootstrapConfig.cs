using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static bool _bootstrapConfigResolutionAttempted;
        private static bool _bootstrapConfigResolutionLogged;
        private static BootstrapConfigAsset _cachedBootstrapConfig;
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

        static partial void RequestEditorStopPlayMode();

        private static bool TryResolveBootstrapConfigFromSources(out BootstrapConfigAsset bootstrapConfig, out string via, out string reason)
        {
            bootstrapConfig = null;
            via = "None";
            reason = string.Empty;

            if (_cachedBootstrapConfig != null)
            {
                bootstrapConfig = _cachedBootstrapConfig;
                via = _cachedBootstrapConfigVia;
                return true;
            }

            if (DependencyManager.HasInstance)
            {
                var provider = DependencyManager.Provider;
                if (provider != null && provider.TryGetGlobal<BootstrapConfigAsset>(out var diConfig) && diConfig != null)
                {
                    bootstrapConfig = diConfig;
                    via = "DI";
                    _cachedBootstrapConfig = diConfig;
                    _cachedBootstrapConfigVia = via;
                    return true;
                }
            }

            bootstrapConfig = Resources.Load<BootstrapConfigAsset>("BootstrapConfig");
            if (bootstrapConfig == null)
            {
                reason = "bootstrap_config_resource_missing";
                return false;
            }

            via = "Resources/BootstrapConfig";
            _cachedBootstrapConfig = bootstrapConfig;
            _cachedBootstrapConfigVia = via;

            if (DependencyManager.HasInstance)
            {
                DependencyManager.Provider.RegisterGlobal(_cachedBootstrapConfig, allowOverride: false);
            }

            return true;
        }

        private static bool TryGetBootstrapConfigForLogging(out BootstrapConfigAsset bootstrapConfig, out string via, out string reason)
        {
            bool resolved = TryResolveBootstrapConfigFromSources(out bootstrapConfig, out via, out reason);
            if (resolved && !_bootstrapConfigResolutionLogged)
            {
                _bootstrapConfigResolutionLogged = true;
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][BOOT] BootstrapConfigResolvedVia={via} asset={bootstrapConfig.name}",
                    DebugUtility.Colors.Info);
            }

            return resolved;
        }

        private static BootstrapConfigAsset GetRequiredBootstrapConfig(out string via)
        {
            if (!_bootstrapConfigResolutionAttempted)
            {
                _bootstrapConfigResolutionAttempted = true;
                if (!TryResolveBootstrapConfigFromSources(out _cachedBootstrapConfig, out _cachedBootstrapConfigVia, out string reason))
                {
                    FailFast($"Missing required BootstrapConfigAsset. reason='{reason}'.");
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
    }
}
