using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SaveRuntime.Authoring;
using _ImmersiveGames.NewScripts.SaveRuntime.Core;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Persistence.Bootstrap
{
    public static class SaveInstaller
    {
        private static bool _installed;

        public static void Install(RuntimeModeConfig runtimeModeConfig)
        {
            if (_installed)
            {
                return;
            }

            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Save] RuntimeModeConfig obrigatorio ausente antes de instalar Save.");
            }

            SaveConfigAsset saveConfig = SaveRuntimeConfigResolver.ResolveSaveConfigOrFail(runtimeModeConfig);

            SaveBackendAsset backendAsset = saveConfig.Backend
                ?? throw new InvalidOperationException($"[FATAL][Save] SaveConfigAsset '{saveConfig.name}' sem backend.");
            ISaveBackend backend = backendAsset.CreateBackend()
                ?? throw new InvalidOperationException($"[FATAL][Save] backend asset '{backendAsset.name}' retornou backend nulo.");

            RegisterIfMissing<ISaveBackend>(
                factory: () => backend,
                alreadyRegisteredMessage: "[Save][BOOT] ISaveBackend already registered.",
                registeredMessage: $"[Save][BOOT] ISaveBackend registered ({backend.BackendId}).");

            SaveCoreService coreService = ResolveOrCreateSaveCoreService(backend);
            SeedDefaultCurrentStateIfMissing(coreService, saveConfig);

            RegisterIfMissing<ISaveService>(
                factory: () => coreService,
                alreadyRegisteredMessage: "[Save][BOOT] ISaveService already registered.",
                registeredMessage: "[Save][BOOT] ISaveService registered (SaveCoreService).");

            RegisterIfMissing<ISaveStateService>(
                factory: () => coreService,
                alreadyRegisteredMessage: "[Save][BOOT] ISaveStateService already registered.",
                registeredMessage: "[Save][BOOT] ISaveStateService registered (SaveCoreService).");

            _installed = true;

            DebugUtility.Log(typeof(SaveInstaller),
                $"[Save] Module installer concluded. backend='{backend.BackendId}' profile='{saveConfig.DefaultProfileId}' slot='{saveConfig.DefaultSlotId}' schemaVersion='{saveConfig.SchemaVersion}'.",
                DebugUtility.Colors.Info);
        }

        private static SaveCoreService ResolveOrCreateSaveCoreService(ISaveBackend backend)
        {
            if (backend == null)
            {
                throw new InvalidOperationException("[FATAL][Save] ISaveBackend obrigatorio ausente para construir SaveCoreService.");
            }

            if (DependencyManager.Provider.TryGetGlobal<SaveCoreService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(SaveInstaller),
                    "[Save][BOOT] SaveCoreService already registered.",
                    DebugUtility.Colors.Info);
                return existing;
            }

            var instance = new SaveCoreService(backend);
            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(SaveInstaller),
                "[Save][BOOT] SaveCoreService registered.",
                DebugUtility.Colors.Info);
            return instance;
        }

        private static void SeedDefaultCurrentStateIfMissing(
            SaveCoreService coreService,
            SaveConfigAsset saveConfig)
        {
            if (coreService == null)
            {
                throw new ArgumentNullException(nameof(coreService));
            }

            if (saveConfig == null)
            {
                throw new ArgumentNullException(nameof(saveConfig));
            }

            if (coreService.HasCurrent)
            {
                return;
            }

            DebugUtility.Log(typeof(SaveInstaller),
                $"[OBS][Save][LegacySeed] Seeding CurrentState from SaveConfigAsset defaults as technical bootstrap fallback profile='{saveConfig.DefaultProfileId}' slot='{saveConfig.DefaultSlotId}'. This is not canonical progression slot policy.",
                DebugUtility.Colors.Warning);

            SaveCurrentState currentState = saveConfig.BuildDefaultCurrentStateOrFail();

            if (!coreService.TrySetCurrent(currentState, "Save/LegacyBootstrapSeed", out string error))
            {
                throw new InvalidOperationException($"[FATAL][Save] Failed to seed current save state. reason='{error}'.");
            }
        }

        private static void RegisterIfMissing<T>(
            Func<T> factory,
            string alreadyRegisteredMessage,
            string registeredMessage) where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(SaveInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return;
            }

            var instance = factory();
            if (instance == null)
            {
                throw new InvalidOperationException($"Factory returned null while registering {typeof(T).Name}.");
            }

            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(SaveInstaller), registeredMessage, DebugUtility.Colors.Info);
        }

    }
}

