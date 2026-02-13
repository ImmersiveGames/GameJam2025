using System;
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

        private static NewScriptsBootstrapConfigAsset GetRequiredBootstrapConfig(out string via)
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
                        cachedBootstrapConfigVia = "ResourcesRoot";
                        DependencyManager.Provider.RegisterGlobal(resourcesConfig);
                    }
                    else
                    {
                        DebugUtility.LogError(typeof(GlobalCompositionRoot),
                            $"[FATAL][Config] Missing NewScriptsBootstrapConfigAsset. path={NewScriptsBootstrapConfigAsset.DefaultResourcesPath}");
                        throw new InvalidOperationException(
                            "Missing required NewScriptsBootstrapConfigAsset. " +
                            $"Create/configure it at Resources path '{NewScriptsBootstrapConfigAsset.DefaultResourcesPath}' " +
                            "or pre-register it in global DI.");
                    }
                }
            }

            via = cachedBootstrapConfigVia;

            if (!bootstrapConfigResolutionLogged)
            {
                bootstrapConfigResolutionLogged = true;
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] BootstrapConfigResolvedVia={via} asset={cachedBootstrapConfig.name} path={NewScriptsBootstrapConfigAsset.DefaultResourcesPath}",
                    DebugUtility.Colors.Info);
            }

            return cachedBootstrapConfig;
        }
    }
}
