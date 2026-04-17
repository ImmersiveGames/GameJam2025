using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Context;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.RuntimeState;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
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
            RegisterPhaseCatalogRuntimeStateService();
            RegisterPhaseCatalogNavigationService();
            RegisterPhaseDefinitionSelectionService();
            RegisterRestartContextService();
            EnsureGameplayParticipationFlowOwner();
            EnsureGameplayPhaseFlowOwner();
            RegisterSessionIntegrationContextService();
            RegisterGameplayPhasePlayerParticipationCompatibilityService();
            RegisterPhaseContentUnloadSupplementProvider();
            RegisterPhaseContentCompletionCleaner();
            RegisterPhaseNextPhaseSelectionService();
            RegisterPhaseNextPhaseCompositionService();

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

        private static void RegisterPhaseCatalogRuntimeStateService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionCatalog>(out var catalog) || catalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IPhaseDefinitionCatalog missing from global DI before runtime state registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogRuntimeStateService>(out var existingStateService) || existingStateService == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPhaseCatalogRuntimeStateService>(new PhaseCatalogRuntimeStateService(catalog));
                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
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
                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
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

        private static void RegisterPhaseDefinitionSelectionService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionSelectionService>(out var existingSelectionService) || existingSelectionService == null)
            {
                if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogRuntimeStateService>(out var runtimeStateService) || runtimeStateService == null)
                {
                    throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IPhaseCatalogRuntimeStateService missing from global DI before selection service registration.");
                }

                var selectionService = new PhaseDefinitionSelectionService(runtimeStateService);
                DependencyManager.Provider.RegisterGlobal<IPhaseDefinitionSelectionService>(selectionService);

                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    $"[OBS][PhaseDefinition][Core] Selection service registered initialPhaseId='{selectionService.SelectedPhaseDefinitionId}' asset='{selectionService.Current.name}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogRuntimeStateService>(out var runtimeStateServiceRef) || runtimeStateServiceRef == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IPhaseCatalogRuntimeStateService missing from global DI while validating selection service registration.");
            }

            PhaseDefinitionAsset committedPhaseDefinitionRef = runtimeStateServiceRef.CurrentCommitted;
            if (!ReferenceEquals(existingSelectionService.Current, committedPhaseDefinitionRef))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][PhaseDefinition] Selection service mismatch: DI has phaseAsset='{existingSelectionService.Current?.name ?? "<none>"}' but runtime committed phase is '{committedPhaseDefinitionRef.name}'.");
            }
        }

        private static void RegisterPhaseNextPhaseSelectionService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseSelectionService>(out var existingService) || existingService == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPhaseNextPhaseSelectionService>(new PhaseNextPhaseSelectionService());
                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][Core] NextPhase selection service registered in global DI.",
                    DebugUtility.Colors.Info);
            }
        }

        private static void RegisterPhaseNextPhaseCompositionService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseCompositionService>(out var existingService) || existingService == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPhaseNextPhaseCompositionService>(new PhaseNextPhaseCompositionService());
                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][Core] NextPhase composition service registered in global DI.",
                    DebugUtility.Colors.Info);
            }
        }

        private static void RegisterSessionIntegrationContextService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameplaySessionContextService>(out var sessionContextService) || sessionContextService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameplaySessionContextService missing from global DI before session integration registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhaseRuntimeService>(out var phaseRuntimeService) || phaseRuntimeService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameplayPhaseRuntimeService missing from global DI before session integration registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var participationService) || participationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameplayParticipationFlowService missing from global DI before session integration registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationContextService>(out var existingService) || existingService == null)
            {
                DependencyManager.Provider.RegisterGlobal<ISessionIntegrationContextService>(
                    new SessionIntegrationContextService(sessionContextService, phaseRuntimeService, participationService));

                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    "[OBS][SessionIntegration][Core] seam='SessionIntegration' executor='SessionIntegrationContextService' role='canonical-session-integration-seam'.",
                    DebugUtility.Colors.Info);
                GameplaySessionFlowCompletionGateComposer.ComposeOrValidate();
                return;
            }

            if (!ReferenceEquals(existingService.SessionContextService, sessionContextService) ||
                !ReferenceEquals(existingService.PhaseRuntimeService, phaseRuntimeService) ||
                !ReferenceEquals(existingService.ParticipationService, participationService))
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][SessionIntegration] SessionIntegrationContextService mismatch between DI binding and phase-side owners.");
            }

            GameplaySessionFlowCompletionGateComposer.ComposeOrValidate();
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
                "[OBS][PhaseDefinition][PhaseFlow] owner='GameplayPhaseFlowService' executor='GameplayPhaseFlowService' role='semantic-phase-flow-owner'.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureGameplayParticipationFlowOwner()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var existingOwner) && existingOwner != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][Participation] GameplayParticipationFlowService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            new GameplayParticipationFlowService();

            DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                "[OBS][PhaseDefinition][Participation] owner='GameplayParticipationFlowService' executor='GameplayParticipationFlowService' role='semantic-roster-owner'.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterGameplayPhasePlayerParticipationCompatibilityService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var participationFlowService) || participationFlowService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IGameplayParticipationFlowService missing from global DI before legacy participation compatibility registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhaseRuntimeService>(out var phaseRuntimeService) || phaseRuntimeService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IGameplayPhaseRuntimeService missing from global DI before legacy participation compatibility registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhasePlayerParticipationService>(out var existingService) || existingService == null)
            {
                var compatibilityService = new GameplayPhasePlayerParticipationService(participationFlowService, phaseRuntimeService);
                DependencyManager.Provider.RegisterGlobal<IGameplayPhasePlayerParticipationService>(compatibilityService);

                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][ParticipationLegacy] compat='residual' executor='GameplayPhasePlayerParticipationService' role='legacy-participation-projection'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (existingService is GameplayPhasePlayerParticipationService)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][ParticipationLegacy] compat='residual' executor='GameplayPhasePlayerParticipationService' role='legacy-participation-projection'.",
                    DebugUtility.Colors.Info);
                return;
            }

            throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IGameplayPhasePlayerParticipationService mismatch: global DI already contains a non-residual implementation.");
        }

        private static void RegisterPhaseContentUnloadSupplementProvider()
        {
            if (DependencyManager.Provider.TryGetGlobal<PhaseContentSceneTransitionUnloadSupplementProvider>(out var existingProvider) && existingProvider != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][PhaseFlow] PhaseContentSceneTransitionUnloadSupplementProvider ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var provider = new PhaseContentSceneTransitionUnloadSupplementProvider();
            DependencyManager.Provider.RegisterGlobal(provider);

            DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                "[OBS][PhaseDefinition][PhaseFlow] executor='PhaseContentSceneTransitionUnloadSupplementProvider' role='phase-content-unload-supplement'.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPhaseContentCompletionCleaner()
        {
            if (DependencyManager.Provider.TryGetGlobal<PhaseContentSceneTransitionCompletionCleaner>(out var existingCleaner) && existingCleaner != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][PhaseFlow] PhaseContentSceneTransitionCompletionCleaner ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var cleaner = new PhaseContentSceneTransitionCompletionCleaner();
            DependencyManager.Provider.RegisterGlobal(cleaner);

            DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                "[OBS][PhaseDefinition][PhaseFlow] executor='PhaseContentSceneTransitionCompletionCleaner' role='phase-content-cleanup'.",
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

    public sealed class PhaseContentSceneTransitionUnloadSupplementProvider : ISceneTransitionUnloadSupplementProvider
    {
        public PhaseContentSceneTransitionUnloadSupplementProvider()
        {
            SceneTransitionUnloadSupplementRegistry.Register(this);
        }

        public IReadOnlyList<string> GetSupplementalScenesToUnload(SceneTransitionContext context)
        {
            if (context.RouteKind == SceneRouteKind.Gameplay)
            {
                return Array.Empty<string>();
            }

            if (!PhaseContentSceneRuntimeApplier.HasActiveAppliedPhaseContent)
            {
                DebugUtility.Log(typeof(PhaseContentSceneTransitionUnloadSupplementProvider),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentSupplementalUnloadSkipped routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='no_active_phase_content'.",
                    DebugUtility.Colors.Info);
                return Array.Empty<string>();
            }

            IReadOnlyList<string> activeScenes = PhaseContentSceneRuntimeApplier.ActiveAppliedSceneNames;
            if (activeScenes == null || activeScenes.Count == 0)
            {
                DebugUtility.Log(typeof(PhaseContentSceneTransitionUnloadSupplementProvider),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentSupplementalUnloadSkipped routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='empty_active_scene_list'.",
                    DebugUtility.Colors.Info);
                return Array.Empty<string>();
            }

            DebugUtility.Log(typeof(PhaseContentSceneTransitionUnloadSupplementProvider),
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentSupplementalUnloadProvided routeId='{context.RouteId}' routeKind='{context.RouteKind}' activeScenes=[{string.Join(",", activeScenes)}].",
                DebugUtility.Colors.Info);

            return activeScenes;
        }
    }

    public sealed class PhaseContentSceneTransitionCompletionCleaner : IDisposable
    {
        private readonly EventBinding<SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;
        private bool _disposed;

        public PhaseContentSceneTransitionCompletionCleaner()
        {
            _sceneTransitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);
            EventBus<SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);
        }

        private static bool ShouldClearOnSceneTransitionCompleted(SceneTransitionContext context)
        {
            return context.RouteKind != SceneRouteKind.Gameplay;
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            SceneTransitionContext context = evt.context;
            if (!ShouldClearOnSceneTransitionCompleted(context))
            {
                return;
            }

            if (!PhaseContentSceneRuntimeApplier.HasActiveAppliedPhaseContent)
            {
                DebugUtility.Log(typeof(PhaseContentSceneTransitionCompletionCleaner),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentClearedOnSceneTransitionCompleted routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='no_active_phase_content'.",
                    DebugUtility.Colors.Info);
                return;
            }

            IReadOnlyList<string> activeScenes = PhaseContentSceneRuntimeApplier.ActiveAppliedSceneNames;
            if (activeScenes == null || activeScenes.Count == 0)
            {
                DebugUtility.Log(typeof(PhaseContentSceneTransitionCompletionCleaner),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentClearedOnSceneTransitionCompleted routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='empty_active_scene_list'.",
                    DebugUtility.Colors.Info);
                return;
            }

            PhaseContentSceneRuntimeApplier.RecordCleared();

            DebugUtility.Log(typeof(PhaseContentSceneTransitionCompletionCleaner),
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentClearedOnSceneTransitionCompleted routeId='{context.RouteId}' routeKind='{context.RouteKind}' clearedScenes=[{string.Join(",", activeScenes)}].",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);
        }
    }
}