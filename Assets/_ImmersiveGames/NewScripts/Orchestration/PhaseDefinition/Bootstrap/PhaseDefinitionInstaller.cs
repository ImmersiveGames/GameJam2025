using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
using _ImmersiveGames.NewScripts.Orchestration.SessionIntegration.Runtime;
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
            RegisterPhaseCatalogRuntimeStateService();
            RegisterPhaseCatalogNavigationService();
            RegisterPhaseDefinitionSelectionService();
            RegisterRestartContextService();
            EnsureGameplayParticipationFlowOwner();
            EnsureGameplayPhaseFlowOwner();
            RegisterSessionIntegrationContextService();
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
                    "[OBS][SessionIntegration][Core] SessionIntegrationContextService registrado no DI global como seam operacional.",
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
                "[OBS][PhaseDefinition][PhaseFlow] GameplayPhaseFlowService registrado no DI global como owner explicito phase-side.",
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
                "[OBS][PhaseDefinition][Participation] GameplayParticipationFlowService registrado no DI global como owner explicito phase-side.",
                DebugUtility.Colors.Info);
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
                "[OBS][PhaseDefinition][PhaseFlow] PhaseContentSceneTransitionUnloadSupplementProvider registrado no DI global como seam operacional de Phase Content unload.",
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
                "[OBS][PhaseDefinition][PhaseFlow] PhaseContentSceneTransitionCompletionCleaner registrado no DI global como seam operacional de Phase Content cleanup.",
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

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public sealed class PhaseContentSceneTransitionUnloadSupplementProvider : _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime.ISceneTransitionUnloadSupplementProvider
    {
        public PhaseContentSceneTransitionUnloadSupplementProvider()
        {
            _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime.SceneTransitionUnloadSupplementRegistry.Register(this);
        }

        public IReadOnlyList<string> GetSupplementalScenesToUnload(_ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime.SceneTransitionContext context)
        {
            if (context.RouteKind == _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime.SceneRouteKind.Gameplay)
            {
                return Array.Empty<string>();
            }

            if (!PhaseContentSceneRuntimeApplier.HasActiveAppliedPhaseContent)
            {
                _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Log(typeof(PhaseContentSceneTransitionUnloadSupplementProvider),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentSupplementalUnloadSkipped routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='no_active_phase_content'.",
                    _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Colors.Info);
                return Array.Empty<string>();
            }

            IReadOnlyList<string> activeScenes = PhaseContentSceneRuntimeApplier.ActiveAppliedSceneNames;
            if (activeScenes == null || activeScenes.Count == 0)
            {
                _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Log(typeof(PhaseContentSceneTransitionUnloadSupplementProvider),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentSupplementalUnloadSkipped routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='empty_active_scene_list'.",
                    _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Colors.Info);
                return Array.Empty<string>();
            }

            _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Log(typeof(PhaseContentSceneTransitionUnloadSupplementProvider),
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentSupplementalUnloadProvided routeId='{context.RouteId}' routeKind='{context.RouteKind}' activeScenes=[{string.Join(",", activeScenes)}].",
                _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Colors.Info);

            return activeScenes;
        }
    }

    public sealed class PhaseContentSceneTransitionCompletionCleaner : IDisposable
    {
        private readonly _ImmersiveGames.NewScripts.Core.Events.EventBinding<_ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime.SceneTransitionCompletedEvent> _sceneTransitionCompletedBinding;
        private bool _disposed;

        public PhaseContentSceneTransitionCompletionCleaner()
        {
            _sceneTransitionCompletedBinding = new _ImmersiveGames.NewScripts.Core.Events.EventBinding<_ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime.SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);
            _ImmersiveGames.NewScripts.Core.Events.EventBus<_ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime.SceneTransitionCompletedEvent>.Register(_sceneTransitionCompletedBinding);
        }

        private static bool ShouldClearOnSceneTransitionCompleted(_ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime.SceneTransitionContext context)
        {
            return context.RouteKind != _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime.SceneRouteKind.Gameplay;
        }

        private void OnSceneTransitionCompleted(_ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime.SceneTransitionCompletedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime.SceneTransitionContext context = evt.context;
            if (!ShouldClearOnSceneTransitionCompleted(context))
            {
                return;
            }

            if (!PhaseContentSceneRuntimeApplier.HasActiveAppliedPhaseContent)
            {
                _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Log(typeof(PhaseContentSceneTransitionCompletionCleaner),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentClearedOnSceneTransitionCompleted routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='no_active_phase_content'.",
                    _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Colors.Info);
                return;
            }

            IReadOnlyList<string> activeScenes = PhaseContentSceneRuntimeApplier.ActiveAppliedSceneNames;
            if (activeScenes == null || activeScenes.Count == 0)
            {
                _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Log(typeof(PhaseContentSceneTransitionCompletionCleaner),
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentClearedOnSceneTransitionCompleted routeId='{context.RouteId}' routeKind='{context.RouteKind}' reason='empty_active_scene_list'.",
                    _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Colors.Info);
                return;
            }

            PhaseContentSceneRuntimeApplier.RecordCleared();

            _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Log(typeof(PhaseContentSceneTransitionCompletionCleaner),
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentClearedOnSceneTransitionCompleted routeId='{context.RouteId}' routeKind='{context.RouteKind}' clearedScenes=[{string.Join(",", activeScenes)}].",
                _ImmersiveGames.NewScripts.Core.Logging.DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _ImmersiveGames.NewScripts.Core.Events.EventBus<_ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime.SceneTransitionCompletedEvent>.Unregister(_sceneTransitionCompletedBinding);
        }
    }
}
