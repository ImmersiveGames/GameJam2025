using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static bool bootstrapConfigResolutionAttempted;
        private static bool bootstrapConfigResolutionLogged;
        private static NewScriptsBootstrapConfigAsset cachedBootstrapConfig;
        private static string cachedBootstrapConfigVia = "None";

        private static bool TryResolveBootstrapConfig(out NewScriptsBootstrapConfigAsset config, out string via)
        {
            if (!bootstrapConfigResolutionAttempted)
            {
                bootstrapConfigResolutionAttempted = true;

                if (DependencyManager.Provider.TryGetGlobal<NewScriptsBootstrapConfigAsset>(out var diConfig) && diConfig != null)
                {
                    cachedBootstrapConfig = diConfig;
                    cachedBootstrapConfigVia = "DI";
                }
                else
                {
                    var resourcesConfig = Resources.Load<NewScriptsBootstrapConfigAsset>(NewScriptsBootstrapConfigAsset.DefaultResourcesPath);
                    if (resourcesConfig != null)
                    {
                        cachedBootstrapConfig = resourcesConfig;
                        cachedBootstrapConfigVia = "LegacyResources";
                        DependencyManager.Provider.RegisterGlobal(resourcesConfig);
                    }
                    else
                    {
                        cachedBootstrapConfig = null;
                        cachedBootstrapConfigVia = "None";
                    }
                }
            }

            config = cachedBootstrapConfig;
            via = cachedBootstrapConfigVia;

            if (!bootstrapConfigResolutionLogged)
            {
                bootstrapConfigResolutionLogged = true;
                var assetName = config != null ? config.name : "<null>";
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] BootstrapConfigResolvedVia={via} asset={assetName} path={NewScriptsBootstrapConfigAsset.DefaultResourcesPath}",
                    DebugUtility.Colors.Info);
            }

            return config != null;
        }
    }
}
