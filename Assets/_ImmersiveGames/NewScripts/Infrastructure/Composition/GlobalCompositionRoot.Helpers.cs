using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        private static NewScriptsBootstrapConfigAsset GetRequiredBootstrapConfig()
        {
            if (RuntimeFailFastUtility.IsFatalLatched)
            {
                throw new InvalidOperationException("Bootstrap resolution aborted because fatal latch is active.");
            }

            if (_bootstrapConfigCache != null)
            {
                return _bootstrapConfigCache;
            }

            var loadedConfig = Resources.Load<NewScriptsBootstrapConfigAsset>(NewScriptsBootstrapConfigAsset.DefaultResourcesPath);
            if (loadedConfig != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] BootstrapConfigResolvedVia=Resources path='{NewScriptsBootstrapConfigAsset.DefaultResourcesPath}' assetName='{loadedConfig.name}'.",
                    DebugUtility.Colors.Info);

                ValidateRequiredBootstrapConfigFields(loadedConfig);

                if (!DependencyManager.Provider.TryGetGlobal<NewScriptsBootstrapConfigAsset>(out var existing) || existing == null)
                {
                    DependencyManager.Provider.RegisterGlobal(loadedConfig);
                }

                _bootstrapConfigCache = loadedConfig;
                return loadedConfig;
            }

            if (DependencyManager.Provider.TryGetGlobal<NewScriptsBootstrapConfigAsset>(out var harnessConfig) && harnessConfig != null)
            {
                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    $"[OBS][Config] BootstrapConfigResolvedVia=DI assetName='{harnessConfig.name}'.",
                    DebugUtility.Colors.Info);

                ValidateRequiredBootstrapConfigFields(harnessConfig);
                _bootstrapConfigCache = harnessConfig;
                return harnessConfig;
            }

            const string fatalMessage = "Missing required BootstrapConfig at Resources path='Config/NewScriptsBootstrapConfig'.";
            throw RuntimeFailFastUtility.FailFastAndCreateException("Config", fatalMessage);
        }

        private static void ValidateRequiredBootstrapConfigFields(NewScriptsBootstrapConfigAsset config)
        {
            if (config == null)
            {
                throw RuntimeFailFastUtility.FailFastAndCreateException(
                    "Config",
                    "BootstrapConfig invalid: missing field 'asset' asset='NewScriptsBootstrapConfig'.");
            }

            if (config.NavigationCatalog == null)
            {
                ReportMissingBootstrapField(GetBootstrapAssetName(config), "navigationCatalog");
            }

            if (config.TransitionStyleCatalog == null)
            {
                ReportMissingBootstrapField(GetBootstrapAssetName(config), "transitionStyleCatalog");
            }

            if (config.LevelCatalog == null)
            {
                ReportMissingBootstrapField(GetBootstrapAssetName(config), "levelCatalog");
            }

            if (config.SceneRouteCatalog == null)
            {
                ReportMissingBootstrapField(GetBootstrapAssetName(config), "sceneRouteCatalog");
            }

            if (config.TransitionProfileCatalog == null)
            {
                ReportMissingBootstrapField(GetBootstrapAssetName(config), "transitionProfileCatalog");
            }

            ValidateRequiredEssentialScenes(config);
        }

        private static void ValidateRequiredEssentialScenes(NewScriptsBootstrapConfigAsset config)
        {
            var essential = config.EssentialScenes;
            if (essential == null)
            {
                throw RuntimeFailFastUtility.FailFastAndCreateException(
                    "Config",
                    $"BootstrapConfig invalid: missing field 'essentialScenes' asset='{GetBootstrapAssetName(config)}'.");
            }

            ValidateRequiredSceneReference(config, "essentialScenes.fadeScene", essential.FadeScene);
            ValidateRequiredSceneReference(config, "essentialScenes.uiGlobalScene", essential.UiGlobalScene);
            ValidateRequiredSceneReference(config, "essentialScenes.menuScene", essential.MenuScene);
            ValidateRequiredSceneReference(config, "essentialScenes.bootEntryScene", essential.BootEntryScene);
        }

        private static void ValidateRequiredSceneReference(
            NewScriptsBootstrapConfigAsset config,
            string fieldName,
            SceneReference sceneReference)
        {
            if (sceneReference == null || !sceneReference.IsAssigned)
            {
                throw RuntimeFailFastUtility.FailFastAndCreateException(
                    "Config",
                    $"BootstrapConfig invalid: missing field '{fieldName}' asset='{GetBootstrapAssetName(config)}'.");
            }

            var scenePath = sceneReference.ScenePath;
            var buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Config] EssentialScenes field='{fieldName}' path='{scenePath}' BuildIndexByScenePath={buildIndex}.",
                DebugUtility.Colors.Info);

            if (buildIndex < 0)
            {
                throw RuntimeFailFastUtility.FailFastAndCreateException(
                    "Config",
                    $"BootstrapConfig invalid: scene '{fieldName}' path='{scenePath}' not found in Build Settings.");
            }
        }

        private static string GetBootstrapAssetName(NewScriptsBootstrapConfigAsset config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.name))
            {
                return "NewScriptsBootstrapConfig";
            }

            return config.name;
        }

        private static void ReportMissingBootstrapField(string assetName, string fieldName)
        {
            throw RuntimeFailFastUtility.FailFastAndCreateException(
                "Config",
                $"BootstrapConfig invalid: missing field '{fieldName}' asset='{assetName}'.");
        }

        private static bool AbortBootstrapIfFatalLatched(string step)
        {
            if (!RuntimeFailFastUtility.IsFatalLatched)
            {
                return false;
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[OBS][Boot] Aborting bootstrap due to fatal latch. step='{step}'.",
                DebugUtility.Colors.Info);
            return true;
        }
    }
}
