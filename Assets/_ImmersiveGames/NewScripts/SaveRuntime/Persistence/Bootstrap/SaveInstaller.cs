using System;
using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;
using ImmersiveGames.GameJam2025.Infrastructure.Config;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Experience.Preferences.Contracts;
using ImmersiveGames.GameJam2025.Experience.Save.Contracts;
using ImmersiveGames.GameJam2025.Experience.Save.Models;
using ImmersiveGames.GameJam2025.Experience.Save.Orchestration;
using ImmersiveGames.GameJam2025.Experience.Save.Progression;
using ImmersiveGames.GameJam2025.Experience.Save.Progression.Backends;
namespace ImmersiveGames.GameJam2025.Experience.Save.Bootstrap
{
    public static class SaveInstaller
    {
        private static bool _installed;
        private static SaveOrchestrationService _orchestrationService;

        public static void Install(BootstrapConfigAsset bootstrapConfig)
        {
            _ = bootstrapConfig;

            if (_installed)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPreferencesStateService>(out var preferencesState) || preferencesState == null)
            {
                throw new InvalidOperationException("[FATAL][Save] IPreferencesStateService obrigatorio ausente antes de instalar Save.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPreferencesSaveService>(out var preferencesSave) || preferencesSave == null)
            {
                throw new InvalidOperationException("[FATAL][Save] IPreferencesSaveService obrigatorio ausente antes de instalar Save.");
            }

            RegisterIfMissing<IProgressionBackend>(
                factory: () => new InMemoryProgressionBackend(),
                alreadyRegisteredMessage: "[Save][BOOT] IProgressionBackend already registered.",
                registeredMessage: "[Save][BOOT] IProgressionBackend registered (InMemoryProgressionBackend).");

            if (!DependencyManager.Provider.TryGetGlobal<IProgressionBackend>(out var progressionBackend) || progressionBackend == null)
            {
                throw new InvalidOperationException("[FATAL][Save] IProgressionBackend obrigatorio ausente antes de instalar Save.");
            }

            var progressionStateService = ResolveOrCreateProgressionService(progressionBackend);
            var progressionSaveService = progressionStateService as IProgressionSaveService
                ?? throw new InvalidOperationException("[FATAL][Save] ProgressionService nao implementa IProgressionSaveService.");

            InitializeProgressionSnapshot(progressionStateService, progressionSaveService);

            var requiredIdentity = new SaveIdentity(ProgressionSnapshot.BootstrapProfileId, ProgressionSnapshot.BootstrapSlotId);
            _orchestrationService = new SaveOrchestrationService(
                requiredIdentity,
                preferencesState,
                preferencesSave,
                progressionStateService,
                progressionSaveService);

            RegisterIfMissing<ISaveOrchestrationService>(
                factory: () => _orchestrationService,
                alreadyRegisteredMessage: "[Save][BOOT] ISaveOrchestrationService already registered.",
                registeredMessage: "[Save][BOOT] ISaveOrchestrationService registered.");

            _installed = true;

            DebugUtility.Log(typeof(SaveInstaller),
                $"[Save] Module installer concluded. identity={requiredIdentity}.",
                DebugUtility.Colors.Info);
        }

        private static T ResolveOrCreate<T>(
            string alreadyRegisteredMessage,
            string registeredMessage) where T : class, new()
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(SaveInstaller), alreadyRegisteredMessage, DebugUtility.Colors.Info);
                return existing;
            }

            var instance = new T();
            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(SaveInstaller), registeredMessage, DebugUtility.Colors.Info);
            return instance;
        }

        private static ProgressionService ResolveOrCreateProgressionService(IProgressionBackend backend)
        {
            if (backend == null)
            {
                throw new InvalidOperationException("[FATAL][Save] IProgressionBackend obrigatorio ausente para construir ProgressionService.");
            }

            if (DependencyManager.Provider.TryGetGlobal<ProgressionService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(SaveInstaller),
                    "[Save][BOOT] ProgressionService already registered.",
                    DebugUtility.Colors.Info);
                return existing;
            }

            var instance = new ProgressionService(backend);
            DependencyManager.Provider.RegisterGlobal(instance);
            DebugUtility.LogVerbose(typeof(SaveInstaller),
                "[Save][BOOT] ProgressionService registered.",
                DebugUtility.Colors.Info);
            return instance;
        }

        private static void InitializeProgressionSnapshot(
            IProgressionStateService progressionStateService,
            IProgressionSaveService progressionSaveService)
        {
            if (progressionStateService == null)
            {
                throw new ArgumentNullException(nameof(progressionStateService));
            }

            if (progressionSaveService == null)
            {
                throw new ArgumentNullException(nameof(progressionSaveService));
            }

            DebugUtility.LogVerbose(typeof(SaveInstaller),
                $"[Save] progression load requested. backend='{progressionSaveService.BackendId}' profile='{ProgressionSnapshot.BootstrapProfileId}' slot='{ProgressionSnapshot.BootstrapSlotId}'.",
                DebugUtility.Colors.Info);

            bool loaded = progressionSaveService.TryLoad(
                ProgressionSnapshot.BootstrapProfileId,
                ProgressionSnapshot.BootstrapSlotId,
                out var loadedSnapshot,
                out string loadReason);

            if (loaded && loadedSnapshot != null)
            {
                progressionStateService.SetCurrent(loadedSnapshot, "Save/BootstrapLoad");
                return;
            }

            var bootstrapSnapshot = new ProgressionSnapshot(
                ProgressionSnapshot.BootstrapProfileId,
                ProgressionSnapshot.BootstrapSlotId,
                new Dictionary<string, string>(),
                revision: 0);

            progressionStateService.SetCurrent(bootstrapSnapshot, "Save/BootstrapSeed");

            DebugUtility.LogVerbose(typeof(SaveInstaller),
                $"[Save] bootstrap kept seed progression. backend='{progressionSaveService.BackendId}' reason='{loadReason}'.",
                DebugUtility.Colors.Info);
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

