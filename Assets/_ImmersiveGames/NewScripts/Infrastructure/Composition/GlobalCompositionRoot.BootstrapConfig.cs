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
        private static bool fatalAbortRequested;


        private static void FailFast(string message)
        {
            fatalAbortRequested = true;

            DebugUtility.LogError(typeof(GlobalCompositionRoot), $"[FATAL][Config] {message}");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            if (!Application.isEditor)
            {
                Application.Quit();
            }

            throw new InvalidOperationException($"[FATAL][Config] {message}");
        }

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
                    ResolveBootstrapConfigFromSceneProviderOrFail();
                    DependencyManager.Provider.RegisterGlobal(cachedBootstrapConfig);
                }
            }

            via = cachedBootstrapConfigVia;

            if (!bootstrapConfigResolutionLogged)
            {
                bootstrapConfigResolutionLogged = true;
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] BootstrapConfigResolvedVia={via} asset={cachedBootstrapConfig.name}",
                    DebugUtility.Colors.Info);
            }

            if (cachedBootstrapConfig == null || fatalAbortRequested)
            {
                throw new InvalidOperationException("[FATAL][Config] Bootstrap config resolution aborted.");
            }

            return cachedBootstrapConfig;
        }

        private static void ResolveBootstrapConfigFromSceneProviderOrFail()
        {
            var provider = UnityEngine.Object.FindFirstObjectByType<NewScriptsBootstrapConfigProvider>(FindObjectsInactive.Include);
            if (provider == null)
            {
                FailFast("Missing required NewScriptsBootstrapConfigProvider in loaded scene. Configure a direct reference to NewScriptsBootstrapConfigAsset.");
            }

            if (provider.Config == null)
            {
                FailFast("Missing required NewScriptsBootstrapConfigAsset in NewScriptsBootstrapConfigProvider.config.");
            }

            cachedBootstrapConfig = provider.Config;
            cachedBootstrapConfigVia = "SceneProvider";
        }
    }
}
