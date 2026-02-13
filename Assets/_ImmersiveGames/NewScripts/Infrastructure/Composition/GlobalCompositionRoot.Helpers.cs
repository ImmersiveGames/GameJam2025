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

        private static NewScriptsBootstrapConfigAsset GetRequiredBootstrapConfig()
        {
            if (_bootstrapConfigCache != null)
            {
                ValidateRequiredBootstrapConfigFields(_bootstrapConfigCache);
                return _bootstrapConfigCache;
            }

            if (DependencyManager.Provider.TryGetGlobal<NewScriptsBootstrapConfigAsset>(out var globalConfig) && globalConfig != null)
            {
                ValidateRequiredBootstrapConfigFields(globalConfig);
                _bootstrapConfigCache = globalConfig;
                return globalConfig;
            }

            var loadedConfig = Resources.Load<NewScriptsBootstrapConfigAsset>(NewScriptsBootstrapConfigAsset.DefaultResourcesPath);
            if (loadedConfig == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    $"[FATAL][Config] Missing required BootstrapConfig at Resources path='{NewScriptsBootstrapConfigAsset.DefaultResourcesPath}'.");
                throw new InvalidOperationException(
                    $"Required NewScriptsBootstrapConfigAsset not found at 'Assets/Resources/{NewScriptsBootstrapConfigAsset.DefaultResourcesPath}.asset'.");
            }

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

        private static void ValidateRequiredBootstrapConfigFields(NewScriptsBootstrapConfigAsset config)
        {
            if (config == null)
            {
                DebugUtility.LogError(typeof(GlobalCompositionRoot),
                    "[FATAL][Config] BootstrapConfig invalid: missing field 'asset' asset='NewScriptsBootstrapConfig'.");
                throw new InvalidOperationException("NewScriptsBootstrapConfigAsset reference is null during bootstrap validation.");
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
            DebugUtility.LogError(typeof(GlobalCompositionRoot),
                $"[FATAL][Config] BootstrapConfig invalid: missing field '{fieldName}' asset='{assetName}'.");
            throw new InvalidOperationException(
                $"NewScriptsBootstrapConfigAsset is invalid. Missing required field '{fieldName}' in asset '{assetName}'.");
        }

    }
}
