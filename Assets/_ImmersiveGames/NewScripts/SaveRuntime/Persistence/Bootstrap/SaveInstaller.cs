using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SaveRuntime.Authoring;
using _ImmersiveGames.NewScripts.SaveRuntime.Core;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
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

            var saveConfig = SaveRuntimeConfigResolver.ResolveSaveConfigOrFail(runtimeModeConfig);

            var backendAsset = saveConfig.Backend
                ?? throw new InvalidOperationException($"[FATAL][Save] SaveConfigAsset '{saveConfig.name}' sem backend.");
            var backend = backendAsset.CreateBackend()
                ?? throw new InvalidOperationException($"[FATAL][Save] backend asset '{backendAsset.name}' retornou backend nulo.");

            RegisterIfMissing<ISaveBackend>(
                () => backend,
                "[Save][BOOT] ISaveBackend already registered.",
                $"[Save][BOOT] ISaveBackend registered ({backend.BackendId}).");

            var coreService = ResolveOrCreateSaveCoreService(backend);
            EnsureCurrentStateInitializedOrFail(coreService, saveConfig);

            RegisterIfMissing<ISaveService>(
                () => coreService,
                "[Save][BOOT] ISaveService already registered.",
                "[Save][BOOT] ISaveService registered (SaveCoreService).");

            RegisterIfMissing<ISaveStateService>(
                () => coreService,
                "[Save][BOOT] ISaveStateService already registered.",
                "[Save][BOOT] ISaveStateService registered (SaveCoreService).");

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

        private static void EnsureCurrentStateInitializedOrFail(
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

            DebugUtility.LogVerbose(typeof(SaveInstaller),
                $"Initializing CurrentState from SaveConfigAsset defaults profile='{saveConfig.DefaultProfileId}' slot='{saveConfig.DefaultSlotId}'.",
                DebugUtility.Colors.Info);

            var currentState = saveConfig.BuildDefaultCurrentStateOrFail();

            if (!coreService.TrySetCurrent(currentState, "Save/BootstrapStateInit", out string error))
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
