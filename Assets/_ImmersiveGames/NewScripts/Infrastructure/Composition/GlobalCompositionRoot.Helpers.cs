using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static NewScriptsBootstrapConfigAsset _bootstrapConfigCache;

        // --------------------------------------------------------------------
        // DI helper
        // --------------------------------------------------------------------

        private static void RegisterIfMissing<T>(Func<T> factory) where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot), $"Global service already present: {typeof(T).Name}.");
                return;
            }

            var instance = factory();
            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot), $"Registered global service: {typeof(T).Name}.");
        }

        private static bool TryGetBootstrapConfig(out NewScriptsBootstrapConfigAsset config)
        {
            if (_bootstrapConfigCache != null)
            {
                config = _bootstrapConfigCache;
                return true;
            }

            if (DependencyManager.Provider.TryGetGlobal<NewScriptsBootstrapConfigAsset>(out var globalConfig) && globalConfig != null)
            {
                _bootstrapConfigCache = globalConfig;
                config = globalConfig;
                return true;
            }

            var loadedConfig = Resources.Load<NewScriptsBootstrapConfigAsset>(NewScriptsBootstrapConfigAsset.DefaultResourcesPath);
            if (loadedConfig == null)
            {
                config = null;
                return false;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Config] BootstrapConfigResolvedVia=Resources path='{NewScriptsBootstrapConfigAsset.DefaultResourcesPath}' assetName='{loadedConfig.name}'.",
                DebugUtility.Colors.Info);

            if (!DependencyManager.Provider.TryGetGlobal<NewScriptsBootstrapConfigAsset>(out var existing) || existing == null)
            {
                DependencyManager.Provider.RegisterGlobal(loadedConfig);
            }

            _bootstrapConfigCache = loadedConfig;
            config = loadedConfig;
            return true;
        }

    }
}
