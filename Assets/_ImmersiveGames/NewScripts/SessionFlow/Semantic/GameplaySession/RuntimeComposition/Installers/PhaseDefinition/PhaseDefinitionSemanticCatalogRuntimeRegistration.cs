using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.RuntimeState;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    internal static class PhaseDefinitionSemanticCatalogRuntimeRegistration
    {
        public static void RegisterAll(BootstrapConfigAsset bootstrapConfig)
        {
            RegisterPhaseDefinitionCatalog(bootstrapConfig);
            RegisterPhaseDefinitionResolver();
            RegisterPhaseCatalogRuntimeStateService();
            RegisterPhaseCatalogNavigationService();
        }

        private static void RegisterPhaseDefinitionCatalog(BootstrapConfigAsset bootstrapConfig)
        {
            var gameplayRouteRef = bootstrapConfig.NavigationCatalog.ResolveGameplayRouteRefOrFail();
            var catalogAsset = bootstrapConfig.NavigationCatalog.ResolveGameplayPhaseCatalogOrFail();
            if (catalogAsset == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] Phase catalog required when phase-enabled. Missing gameplay route phaseDefinitionCatalog.");
            }

            catalogAsset.ValidateOrFail();

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionCatalog>(out var existingCatalog) || existingCatalog == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPhaseDefinitionCatalog>(catalogAsset);

                DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticCatalogRuntimeRegistration),
                    $"[OBS][PhaseDefinition][Core] CatalogResolvedVia=Route routeId='{gameplayRouteRef.RouteId}' routeKind='{gameplayRouteRef.RouteKind}' asset='{catalogAsset.name}' phaseCount={catalogAsset.PhaseIds.Count}.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!ReferenceEquals(existingCatalog, catalogAsset))
            {
                string diAssetName = existingCatalog is UnityEngine.Object diObject ? diObject.name : existingCatalog.GetType().Name;
                throw new InvalidOperationException(
                    $"[FATAL][Config][PhaseDefinition] PhaseDefinitionCatalog mismatch: DI has {diAssetName} but gameplay route has {catalogAsset.name}.");
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
                DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticCatalogRuntimeRegistration),
                    "[OBS][PhaseDefinition][Core] Resolver registered in global DI.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!ReferenceEquals(existingResolver.Catalog, catalog))
            {
                string existingCatalogName = existingResolver.Catalog is UnityEngine.Object existingObject ? existingObject.name : existingResolver.Catalog.GetType().Name;
                string expectedCatalogName = catalog is UnityEngine.Object expectedObject ? expectedObject.name : catalog.GetType().Name;
                throw new InvalidOperationException(
                    $"[FATAL][Config][PhaseDefinition] PhaseDefinitionResolver mismatch: DI has catalog '{existingCatalogName}' but gameplay route has '{expectedCatalogName}'.");
            }
        }

        private static void RegisterPhaseCatalogRuntimeStateService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionCatalog>(out var catalog) || catalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IPhaseDefinitionCatalog missing from global DI before runtime state registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogRuntimeStateService>(out var existingStateService) || existingStateService == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPhaseCatalogRuntimeStateService>(new PhaseCatalogRuntimeStateService(catalog));
                DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticCatalogRuntimeRegistration),
                    "[OBS][PhaseDefinition][Core] Catalog runtime state service registered in global DI.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!ReferenceEquals(existingStateService.Catalog, catalog))
            {
                string existingCatalogName = existingStateService.Catalog is UnityEngine.Object existingObject ? existingObject.name : existingStateService.Catalog.GetType().Name;
                string expectedCatalogName = catalog is UnityEngine.Object expectedObject ? expectedObject.name : catalog.GetType().Name;
                throw new InvalidOperationException(
                    $"[FATAL][Config][PhaseDefinition] PhaseCatalogRuntimeStateService mismatch: DI has catalog '{existingCatalogName}' but gameplay route has '{expectedCatalogName}'.");
            }
        }

        private static void RegisterPhaseCatalogNavigationService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionCatalog>(out var catalog) || catalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IPhaseDefinitionCatalog missing from global DI before catalog navigation registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogRuntimeStateService>(out var runtimeStateService) || runtimeStateService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IPhaseCatalogRuntimeStateService missing from global DI before catalog navigation registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogNavigationService>(out var existingNavigationService) || existingNavigationService == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPhaseCatalogNavigationService>(new PhaseCatalogNavigationService(catalog, runtimeStateService));
                DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticCatalogRuntimeRegistration),
                    "[OBS][PhaseDefinition][Core] Catalog navigation service registered in global DI.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!ReferenceEquals(existingNavigationService.Catalog, catalog))
            {
                string existingCatalogName = existingNavigationService.Catalog is UnityEngine.Object existingObject ? existingObject.name : existingNavigationService.Catalog.GetType().Name;
                string expectedCatalogName = catalog is UnityEngine.Object expectedObject ? expectedObject.name : catalog.GetType().Name;
                throw new InvalidOperationException(
                    $"[FATAL][Config][PhaseDefinition] PhaseCatalogNavigationService mismatch: DI has catalog '{existingCatalogName}' but gameplay route has '{expectedCatalogName}'.");
            }
        }
    }
}
