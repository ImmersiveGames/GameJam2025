using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Authoring;
using _ImmersiveGames.NewScripts.ActorsSystem.Integration.GameplayRuntime;
using _ImmersiveGames.NewScripts.ActorsSystem.Integration.OperationalBinding;
using _ImmersiveGames.NewScripts.ActorsSystem.Integration.SessionFlow;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Integration.Bootstrap
{
    public static class ActorsSystemBootstrap
    {
        private const string ActorSpecsCatalogResourcesPath = "ActorsSystem/ActorSpecsCatalog";
        private const string ActorSetCatalogResourcesPath = "ActorsSystem/ActorSetCatalog";

        private static bool _installerComposed;
        private static bool _runtimeComposed;
        private static ActorsParticipantRuntimeMappingSpawnBridge _participantRuntimeMappingSpawnBridge;
        private static ActorsOperationalBindingUnityBridge _operationalBindingUnityBridge;

        public static void ComposeInstallerPhase()
        {
            if (_installerComposed)
            {
                return;
            }

            EnsureRequiredDependenciesOrFail();
            EnsureActorSpecsCatalogService();
            EnsureActorSetSelectionService();
            EnsureDefinitionsPort();
            EnsureSemanticParticipationInPort();
            EnsureRuntimeObservationInPort();
            EnsureIdentityRolePolicy();
            EnsureEnsembleService();
            EnsureParticipantRuntimeMappingBoundary();
            EnsurePresenceService();
            EnsureOperationalBindingBoundary();
            EnsureMaterializationPlanService();
            EnsureMaterializationExecutionPolicyService();
            EnsureRegistryBoundary();

            _installerComposed = true;
            DebugUtility.Log(typeof(ActorsSystemBootstrap),
                "[OBS][ActorsSystem] Installer phase concluida (slice2).",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime()
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(ActorsSystemBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            PrimeEnsemble();
            PrimePresenceAndRegistry();
            PrimeMaterializationPlan();
            PrimeMaterializationExecutionPolicy();
            EnsureParticipantRuntimeMappingSpawnBridge();
            EnsureOperationalBindingUnityBridge();
            _runtimeComposed = true;

            DebugUtility.Log(typeof(ActorsSystemBootstrap),
                "[OBS][ActorsSystem] Runtime composition concluida (slice2).",
                DebugUtility.Colors.Info);
        }

        private static void EnsureRequiredDependenciesOrFail()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var participationService) || participationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IGameplayParticipationFlowService ausente antes da composicao do ActorsSystem slice2.");
            }
        }

        private static void EnsureDefinitionsPort()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorsDefinitionsPort>(out var existing) && existing != null)
            {
                return;
            }

            EnsureSessionFlowAdapter();
            if (!DependencyManager.Provider.TryGetGlobal<SessionFlowActorsSemanticPortsAdapter>(out var adapter) || adapter == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] SessionFlowActorsSemanticPortsAdapter ausente para registrar IActorsDefinitionsPort.");
            }

            DependencyManager.Provider.RegisterGlobal<IActorsDefinitionsPort>(adapter);
        }

        private static void EnsureSemanticParticipationInPort()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorsSemanticParticipationInPort>(out var existing) && existing != null)
            {
                return;
            }

            EnsureSessionFlowAdapter();
            if (!DependencyManager.Provider.TryGetGlobal<SessionFlowActorsSemanticPortsAdapter>(out var adapter) || adapter == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] SessionFlowActorsSemanticPortsAdapter ausente para registrar IActorsSemanticParticipationInPort.");
            }

            DependencyManager.Provider.RegisterGlobal<IActorsSemanticParticipationInPort>(adapter);
        }

        private static void EnsureRuntimeObservationInPort()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorsRuntimeObservationInPort>(out var existing) && existing != null)
            {
                return;
            }

            var adapter = new GameplayRuntimeActorsRuntimeObservationInAdapter(DependencyManager.Provider);
            DependencyManager.Provider.RegisterGlobal<IActorsRuntimeObservationInPort>(adapter);
        }

        private static void EnsureIdentityRolePolicy()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorsIdentityRolePolicy>(out var existing) && existing != null)
            {
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IActorsIdentityRolePolicy>(new ActorsDefaultIdentityRolePolicy());
        }

        private static void EnsureEnsembleService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorsEnsembleService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsDefinitionsPort>(out var definitionsPort) || definitionsPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsDefinitionsPort ausente para registrar IActorsEnsembleService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsSemanticParticipationInPort>(out var semanticPort) || semanticPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsSemanticParticipationInPort ausente para registrar IActorsEnsembleService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsRuntimeObservationInPort>(out var runtimePort) || runtimePort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsRuntimeObservationInPort ausente para registrar IActorsEnsembleService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsIdentityRolePolicy>(out var policy) || policy == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsIdentityRolePolicy ausente para registrar IActorsEnsembleService.");
            }

            var service = new ActorsEnsembleService(definitionsPort, semanticPort, runtimePort, policy);
            DependencyManager.Provider.RegisterGlobal<IActorsEnsembleService>(service);
        }

        private static void EnsurePresenceService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorsPresenceService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsEnsembleService>(out var ensembleService) || ensembleService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsEnsembleService ausente para registrar IActorsPresenceService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsRuntimeObservationInPort>(out var runtimeObservationPort) || runtimeObservationPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsRuntimeObservationInPort ausente para registrar IActorsPresenceService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsMaterializedContinuationQueryPort>(out var continuationQueryPort) || continuationQueryPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsMaterializedContinuationQueryPort ausente para registrar IActorsPresenceService.");
            }

            DependencyManager.Provider.RegisterGlobal<IActorsPresenceService>(
                new ActorsPresenceService(ensembleService, runtimeObservationPort, continuationQueryPort));
        }

        private static void EnsureRegistryBoundary()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorsRegistryBoundary>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsPresenceService>(out var presenceService) || presenceService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsPresenceService ausente para registrar IActorsRegistryBoundary.");
            }

            DependencyManager.Provider.RegisterGlobal<IActorsRegistryBoundary>(
                new ActorsRegistryBoundaryService(presenceService));
        }

        private static void EnsureMaterializationPlanService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorsMaterializationPlanService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsEnsembleService>(out var ensembleService) || ensembleService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsEnsembleService ausente para registrar IActorsMaterializationPlanService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsPresenceService>(out var presenceService) || presenceService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsPresenceService ausente para registrar IActorsMaterializationPlanService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsRuntimeObservationInPort>(out var runtimeObservationPort) || runtimeObservationPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsRuntimeObservationInPort ausente para registrar IActorsMaterializationPlanService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsOperationalBindingQueryPort>(out var operationalBindingQueryPort) || operationalBindingQueryPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsOperationalBindingQueryPort ausente para registrar IActorsMaterializationPlanService.");
            }

            DependencyManager.Provider.RegisterGlobal<IActorsMaterializationPlanService>(
                new ActorsMaterializationPlanService(
                    ensembleService,
                    presenceService,
                    runtimeObservationPort,
                    operationalBindingQueryPort));
        }

        private static void EnsureOperationalBindingBoundary()
        {
            bool hasInPort = DependencyManager.Provider.TryGetGlobal<IActorsOperationalBindingInPort>(out var existingInPort) && existingInPort != null;
            bool hasQueryPort = DependencyManager.Provider.TryGetGlobal<IActorsOperationalBindingQueryPort>(out var existingQueryPort) && existingQueryPort != null;
            if (hasInPort && hasQueryPort)
            {
                return;
            }

            var service = new ActorsOperationalBindingService();

            DependencyManager.Provider.RegisterGlobal<IActorsOperationalBindingInPort>(service);
            DependencyManager.Provider.RegisterGlobal<IActorsOperationalBindingQueryPort>(service);
        }

        private static void EnsureMaterializationExecutionPolicyService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorsMaterializationExecutionPolicyService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsMaterializationPlanService>(out var materializationPlanService) || materializationPlanService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsMaterializationPlanService ausente para registrar IActorsMaterializationExecutionPolicyService.");
            }

            DependencyManager.Provider.RegisterGlobal<IActorsMaterializationExecutionPolicyService>(
                new ActorsMaterializationExecutionPolicyService(materializationPlanService));
        }

        private static void EnsureParticipantRuntimeMappingBoundary()
        {
            bool hasInPort = DependencyManager.Provider.TryGetGlobal<IActorsParticipantRuntimeMappingInPort>(out var existingInPort) && existingInPort != null;
            bool hasQueryPort = DependencyManager.Provider.TryGetGlobal<IActorsParticipantRuntimeMappingQueryPort>(out var existingQueryPort) && existingQueryPort != null;
            if (hasInPort && hasQueryPort)
            {
                return;
            }

            var service = new ActorsParticipantRuntimeMappingService();
            DependencyManager.Provider.RegisterGlobal<IActorsParticipantRuntimeMappingInPort>(service);
            DependencyManager.Provider.RegisterGlobal<IActorsParticipantRuntimeMappingQueryPort>(service);
            DependencyManager.Provider.RegisterGlobal<IActorsMaterializedContinuationQueryPort>(service);
        }

        private static void EnsureSessionFlowAdapter()
        {
            if (DependencyManager.Provider.TryGetGlobal<SessionFlowActorsSemanticPortsAdapter>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var participationService) || participationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IGameplayParticipationFlowService ausente para registrar SessionFlowActorsSemanticPortsAdapter.");
            }

            // ✅ Explicit dependency injection: resolver as dependências ANTES de criar o adapter
            if (!DependencyManager.Provider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out var routeActorSetContext) || routeActorSetContext == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] ISceneFlowRouteActorSetRefContext ausente (deve ser composto em SceneFlowBootstrap.EnsureRouteActorSetRefContext) antes de registrar SessionFlowActorsSemanticPortsAdapter.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorSetSelectionService>(out var actorSetSelectionService) || actorSetSelectionService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorSetSelectionService ausente para registrar SessionFlowActorsSemanticPortsAdapter.");
            }

            // ✅ Passar todas as dependências explicitamente
            DependencyManager.Provider.RegisterGlobal(new SessionFlowActorsSemanticPortsAdapter(
                participationService,
                routeActorSetContext,
                actorSetSelectionService));
        }

        private static void EnsureActorSpecsCatalogService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorSpecCatalogService>(out var existing) && existing != null)
            {
                return;
            }

            var catalog = Resources.Load<ActorSpecsCatalogAsset>(ActorSpecsCatalogResourcesPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Missing required ActorSpecsCatalogAsset at Resources path '{ActorSpecsCatalogResourcesPath}'.");
            }

            DependencyManager.Provider.RegisterGlobal<IActorSpecCatalogService>(new ActorSpecsCatalogService(catalog));
        }

        private static void EnsureActorSetSelectionService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorSetSelectionService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorSpecCatalogService>(out var actorSpecCatalogService) || actorSpecCatalogService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorSpecCatalogService ausente para registrar IActorSetSelectionService.");
            }

            var catalog = Resources.Load<ActorSetCatalogAsset>(ActorSetCatalogResourcesPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Missing required ActorSetCatalogAsset at Resources path '{ActorSetCatalogResourcesPath}'.");
            }

            DependencyManager.Provider.RegisterGlobal<IActorSetSelectionService>(new ActorSetSelectionService(catalog, actorSpecCatalogService));
        }

        private static void EnsureOperationalBindingUnityBridge()
        {
            if (_operationalBindingUnityBridge != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsSemanticParticipationInPort>(out var semanticPort) || semanticPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsSemanticParticipationInPort ausente para registrar ActorsOperationalBindingUnityBridge.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsParticipantRuntimeMappingQueryPort>(out var participantRuntimeMappingQueryPort) || participantRuntimeMappingQueryPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsParticipantRuntimeMappingQueryPort ausente para registrar ActorsOperationalBindingUnityBridge.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsOperationalBindingInPort>(out var bindingInPort) || bindingInPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsOperationalBindingInPort ausente para registrar ActorsOperationalBindingUnityBridge.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsOperationalBindingQueryPort>(out var queryPort) || queryPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsOperationalBindingQueryPort ausente para registrar ActorsOperationalBindingUnityBridge.");
            }

            _operationalBindingUnityBridge = new ActorsOperationalBindingUnityBridge(semanticPort, participantRuntimeMappingQueryPort, bindingInPort, queryPort);
            DependencyManager.Provider.RegisterGlobal(_operationalBindingUnityBridge);
        }

        private static void EnsureParticipantRuntimeMappingSpawnBridge()
        {
            if (_participantRuntimeMappingSpawnBridge != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsParticipantRuntimeMappingInPort>(out var mappingInPort) || mappingInPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsParticipantRuntimeMappingInPort ausente para registrar ActorsParticipantRuntimeMappingSpawnBridge.");
            }

            _participantRuntimeMappingSpawnBridge = new ActorsParticipantRuntimeMappingSpawnBridge(mappingInPort);
            DependencyManager.Provider.RegisterGlobal(_participantRuntimeMappingSpawnBridge);
        }

        private static void PrimeEnsemble()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IActorsEnsembleService>(out var ensembleService) || ensembleService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsEnsembleService ausente durante prime runtime.");
            }

            var snapshot = ensembleService.Refresh();
            DebugUtility.Log(typeof(ActorsSystemBootstrap),
                $"[OBS][ActorsSystem] Prime refresh concluido count='{snapshot.Count}' reason='{snapshot.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void PrimePresenceAndRegistry()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IActorsPresenceService>(out var presenceService) || presenceService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsPresenceService ausente durante prime runtime.");
            }

            var presence = presenceService.Refresh();
            DebugUtility.Log(typeof(ActorsSystemBootstrap),
                $"[OBS][ActorsSystem] Prime presence concluido expected='{presence.ExpectedCount}' materialized='{presence.MaterializedCount}' absent='{presence.AbsentCount}' inconsistent='{presence.InconsistentCount}' runtimeOrphan='{presence.RuntimeOrphanCount}' reason='{presence.Reason}'.",
                DebugUtility.Colors.Info);

            if (!DependencyManager.Provider.TryGetGlobal<IActorsRegistryBoundary>(out var registryBoundary) || registryBoundary == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsRegistryBoundary ausente durante prime runtime.");
            }

            var entries = new List<ActorsRegistryEntry>(presence.Count);
            registryBoundary.TryGetAll(entries);
            DebugUtility.Log(typeof(ActorsSystemBootstrap),
                $"[OBS][ActorsSystem] Prime registry boundary concluido count='{entries.Count}'.",
                DebugUtility.Colors.Info);
        }

        private static void PrimeMaterializationPlan()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IActorsMaterializationPlanService>(out var materializationPlanService) || materializationPlanService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsMaterializationPlanService ausente durante prime runtime.");
            }

            ActorsMaterializationPlanSnapshot snapshot = materializationPlanService.Refresh();
            DebugUtility.Log(typeof(ActorsSystemBootstrap),
                $"[OBS][ActorsSystem] Prime materialization plan concluido count='{snapshot.Count}' keep='{snapshot.KeepCount}' materialize='{snapshot.MaterializeCount}' rematerialize='{snapshot.RematerializeCount}' observeWithoutAction='{snapshot.ObserveWithoutActionCount}' inconsistent='{snapshot.InconsistentCount}' excessOrphan='{snapshot.ExcessOrphanCount}' reason='{snapshot.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void PrimeMaterializationExecutionPolicy()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IActorsMaterializationExecutionPolicyService>(out var executionPolicyService) || executionPolicyService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorsMaterializationExecutionPolicyService ausente durante prime runtime.");
            }

            ActorsMaterializationExecutionSnapshot snapshot = executionPolicyService.Refresh();
            DebugUtility.Log(typeof(ActorsSystemBootstrap),
                $"[OBS][ActorsSystem] Prime materialization execution policy concluido count='{snapshot.Count}' noActionStable='{snapshot.NoActionStableCount}' noActionObserve='{snapshot.NoActionObserveCount}' requestMaterialize='{snapshot.RequestMaterializeCount}' requestRematerialize='{snapshot.RequestRematerializeCount}' flagInconsistent='{snapshot.FlagInconsistentCount}' orphanTolerated='{snapshot.FlagRuntimeOrphanToleratedCount}' orphanProblematic='{snapshot.FlagRuntimeOrphanProblematicCount}' reason='{snapshot.Reason}'.",
                DebugUtility.Colors.Info);
        }
    }
}
