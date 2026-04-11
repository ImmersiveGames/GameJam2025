using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
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

            bool phaseEnabled = ResolveGameplayPhaseEnablementOrFail(bootstrapConfig);

            if (!phaseEnabled)
            {
                _installed = true;

                DebugUtility.Log(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][Core] Gameplay route/context phase-disabled; installer no-op.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterPhaseDefinitionCatalog(bootstrapConfig);
            RegisterPhaseDefinitionResolver();
            RegisterPhaseDefinitionSelectionService();
            RegisterPhaseNextPhaseService();
            RegisterRestartContextService();
            EnsureGameplayPhaseFlowOwner();

            _installed = true;

            DebugUtility.Log(typeof(PhaseDefinitionInstaller),
                "[PhaseDefinition][Core] Module installer concluido.",
                DebugUtility.Colors.Info);
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

                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
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
                    $"[FATAL][Config][PhaseDefinition] PhaseDefinitionResolver mismatch: DI has catalog '{existingCatalogName}' but gameplay route has '{expectedCatalogName}'.");
            }
        }

        private static void RegisterPhaseDefinitionSelectionService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionSelectionService>(out var existingSelectionService) || existingSelectionService == null)
            {
                if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionCatalog>(out var catalog) || catalog == null)
                {
                    throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IPhaseDefinitionCatalog missing from global DI before selection service registration.");
                }

                var selectionService = new PhaseDefinitionSelectionService(catalog);
                DependencyManager.Provider.RegisterGlobal<IPhaseDefinitionSelectionService>(selectionService);

                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    $"[OBS][PhaseDefinition][Core] Selection service registered initialPhaseId='{selectionService.SelectedPhaseDefinitionId}' asset='{selectionService.Current.name}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionCatalog>(out var catalogRef) || catalogRef == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IPhaseDefinitionCatalog missing from global DI while validating selection service registration.");
            }

            PhaseDefinitionAsset initialPhaseDefinitionRef = catalogRef.ResolveInitialOrFail();
            if (!ReferenceEquals(existingSelectionService.Current, initialPhaseDefinitionRef))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][PhaseDefinition] Selection service mismatch: DI has phaseAsset='{existingSelectionService.Current?.name ?? "<none>"}' but catalog initial phase is '{initialPhaseDefinitionRef.name}'.");
            }
        }

        private static void RegisterPhaseNextPhaseService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseService>(out var existingService) && existingService != null)
            {
                return;
            }

            var service = new PhaseNextPhaseService();
            DependencyManager.Provider.RegisterGlobal<IPhaseNextPhaseService>(service);

            DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                "[OBS][PhaseDefinition][Core] NextPhase service registered in global DI.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterRestartContextService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var existingService) && existingService != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][Core] IRestartContextService ja registrado no DI global como owner canonical phase-side.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IRestartContextService>(new RestartContextService());

            DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                "[OBS][PhaseDefinition][Core] RestartContextService registrado no DI global como owner canonical phase-side.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureGameplayPhaseFlowOwner()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameplayPhaseFlowService>(out var existingOwner) && existingOwner != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][PhaseFlow] GameplayPhaseFlowService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            new GameplayPhaseFlowService();

            DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                "[OBS][PhaseDefinition][PhaseFlow] GameplayPhaseFlowService registrado no DI global como owner explicito phase-side.",
                DebugUtility.Colors.Info);
        }

        private static bool ResolveGameplayPhaseEnablementOrFail(BootstrapConfigAsset bootstrapConfig)
        {
            if (bootstrapConfig.NavigationCatalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] GameNavigationCatalog obrigatorio ausente para resolver phase-enabled/phase-disabled.");
            }

            bool phaseEnabled = bootstrapConfig.NavigationCatalog.IsGameplayPhaseEnabledOrFail();
            DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                $"[OBS][PhaseDefinition][Core] route-driven phase enablement resolved phaseEnabled={phaseEnabled}.",
                DebugUtility.Colors.Info);
            return phaseEnabled;
        }
    }
}
