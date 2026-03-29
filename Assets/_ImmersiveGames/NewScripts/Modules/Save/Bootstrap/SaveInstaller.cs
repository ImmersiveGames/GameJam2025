using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Modules.Preferences.Contracts;
using _ImmersiveGames.NewScripts.Modules.Save.Contracts;
using _ImmersiveGames.NewScripts.Modules.Save.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Save.Bootstrap
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
