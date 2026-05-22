using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private const string RuntimeModeConfigResourcesPath = "RuntimeMode/RuntimeModeConfig";

        private static bool _runtimeModeConfigResolutionAttempted;
        private static bool _runtimeModeConfigResolutionLogged;
        private static RuntimeModeConfig _cachedRuntimeModeConfig;
        private static string _cachedRuntimeModeConfigVia = "None";
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

        private static bool TryResolveRuntimeModeConfigFromSources(out RuntimeModeConfig runtimeModeConfig, out string via, out string reason)
        {
            runtimeModeConfig = null;
            via = "None";
            reason = string.Empty;

            if (_cachedRuntimeModeConfig != null)
            {
                runtimeModeConfig = _cachedRuntimeModeConfig;
                via = _cachedRuntimeModeConfigVia;
                return true;
            }

            if (DependencyManager.HasInstance)
            {
                var provider = DependencyManager.Provider;
                if (provider != null && provider.TryGetGlobal<RuntimeModeConfig>(out var diConfig) && diConfig != null)
                {
                    runtimeModeConfig = diConfig;
                    via = "DI";
                    _cachedRuntimeModeConfig = diConfig;
                    _cachedRuntimeModeConfigVia = via;
                    return true;
                }
            }

            runtimeModeConfig = Resources.Load<RuntimeModeConfig>(RuntimeModeConfigResourcesPath);
            if (runtimeModeConfig == null)
            {
                reason = "runtime_mode_config_resource_missing";
                return false;
            }

            via = $"Resources/{RuntimeModeConfigResourcesPath}";
            _cachedRuntimeModeConfig = runtimeModeConfig;
            _cachedRuntimeModeConfigVia = via;

            if (DependencyManager.HasInstance)
            {
                DependencyManager.Provider.RegisterGlobal(_cachedRuntimeModeConfig, allowOverride: false);
            }

            return true;
        }

        private static bool TryGetRuntimeModeConfigForLogging(out RuntimeModeConfig runtimeModeConfig, out string via, out string reason)
        {
            bool resolved = TryResolveRuntimeModeConfigFromSources(out runtimeModeConfig, out via, out reason);
            if (resolved && !_runtimeModeConfigResolutionLogged)
            {
                _runtimeModeConfigResolutionLogged = true;
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][BOOT] RuntimeModeConfigResolvedVia={via} asset={runtimeModeConfig.name}",
                    DebugUtility.Colors.Info);
            }

            return resolved;
        }

        private static RuntimeModeConfig GetRequiredRuntimeModeConfig(out string via)
        {
            if (!_runtimeModeConfigResolutionAttempted)
            {
                _runtimeModeConfigResolutionAttempted = true;
                if (!TryResolveRuntimeModeConfigFromSources(out _cachedRuntimeModeConfig, out _cachedRuntimeModeConfigVia, out string reason))
                {
                    FailFast($"Missing required RuntimeModeConfig. reason='{reason}'.");
                }
            }

            via = _cachedRuntimeModeConfigVia;

            if (!_runtimeModeConfigResolutionLogged)
            {
                _runtimeModeConfigResolutionLogged = true;
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] RuntimeModeConfigResolvedVia={via} asset={_cachedRuntimeModeConfig.name}",
                    DebugUtility.Colors.Info);
            }

            if (_cachedRuntimeModeConfig == null || _fatalAbortRequested)
            {
                throw new InvalidOperationException("[FATAL][Config] RuntimeModeConfig resolution aborted.");
            }

            return _cachedRuntimeModeConfig;
        }
    }
}

