using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Bootstrap
{
    public static class PhaseDefinitionInstaller
    {
        private static bool _installed;

        public static void Install(BootstrapConfigAsset bootstrapConfig)
        {
            if (_installed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] BootstrapConfigAsset obrigatorio ausente para instalar PhaseDefinition.");
            }

            RegisterPhaseDefinitionCatalog(bootstrapConfig);
            RegisterPhaseDefinitionResolver();
            RegisterPhaseDefinitionSelectionService(bootstrapConfig);

            _installed = true;

            DebugUtility.Log(typeof(PhaseDefinitionInstaller),
                "[PhaseDefinition][Core] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPhaseDefinitionCatalog(BootstrapConfigAsset bootstrapConfig)
        {
            var catalogAsset = bootstrapConfig.PhaseDefinitionCatalog;
            if (catalogAsset == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] Missing required BootstrapConfigAsset.phaseDefinitionCatalog.");
            }

            catalogAsset.ValidateOrFail();

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionCatalog>(out var existingCatalog) || existingCatalog == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPhaseDefinitionCatalog>(catalogAsset);

                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    $"[OBS][PhaseDefinition][Core] CatalogResolvedVia=BootstrapConfig field=phaseDefinitionCatalog asset={catalogAsset.name} phaseCount={catalogAsset.PhaseIds.Count}.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!ReferenceEquals(existingCatalog, catalogAsset))
            {
                string diAssetName = existingCatalog is UnityEngine.Object diObject ? diObject.name : existingCatalog.GetType().Name;
                throw new InvalidOperationException(
                    $"[FATAL][Config][PhaseDefinition] PhaseDefinitionCatalog mismatch: DI has {diAssetName} but BootstrapConfig has {catalogAsset.name}.");
            }
        }

        private static void RegisterPhaseDefinitionResolver()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionCatalog>(out var catalog) || catalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IPhaseDefinitionCatalog missing from global DI before resolver registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionResolver>(out var existingResolver) || existingResolver == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPhaseDefinitionResolver>(new PhaseDefinitionResolver(catalog));
                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][Core] Resolver registered in global DI.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!ReferenceEquals(existingResolver.Catalog, catalog))
            {
                string existingCatalogName = existingResolver.Catalog is UnityEngine.Object existingObject ? existingObject.name : existingResolver.Catalog.GetType().Name;
                string expectedCatalogName = catalog is UnityEngine.Object expectedObject ? expectedObject.name : catalog.GetType().Name;
                throw new InvalidOperationException(
                    $"[FATAL][Config][PhaseDefinition] PhaseDefinitionResolver mismatch: DI has catalog '{existingCatalogName}' but BootstrapConfig has '{expectedCatalogName}'.");
            }
        }

        private static void RegisterPhaseDefinitionSelectionService(BootstrapConfigAsset bootstrapConfig)
        {
            PhaseDefinitionAsset selectedPhaseDefinitionRef = bootstrapConfig.SelectedPhaseDefinitionRef;
            if (selectedPhaseDefinitionRef == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] BootstrapConfigAsset.selectedPhaseDefinitionRef is required.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionSelectionService>(out var existingSelectionService) || existingSelectionService == null)
            {
                var selectionService = new PhaseDefinitionSelectionService(selectedPhaseDefinitionRef);
                DependencyManager.Provider.RegisterGlobal<IPhaseDefinitionSelectionService>(selectionService);

                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    $"[OBS][PhaseDefinition][Core] Selection service registered phaseId='{selectionService.SelectedPhaseDefinitionId}' asset='{selectedPhaseDefinitionRef.name}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!ReferenceEquals(existingSelectionService.Current, selectedPhaseDefinitionRef))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][PhaseDefinition] Selection service mismatch: DI has phaseAsset='{existingSelectionService.Current?.name ?? "<none>"}' but BootstrapConfig has '{selectedPhaseDefinitionRef.name}'.");
            }
        }
    }
}
