using System;
using _ImmersiveGames.NewScripts.ActorsSystem.Integration.SessionFlow;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;

namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.Bootstrap
{
    public static class GameplayRuntimeBootstrap
    {
        private static bool _runtimeComposed;
        private static ActorsMaterializationExecutionRuntimeBridge _actorsExecutionRuntimeBridge;
        private static SessionTransitionPhaseLocalEntryReadyMaterializationBridge _sessionTransitionPhaseLocalEntryReadyMaterializationBridge;
        private static ActorsOperationalMaterializationHandoffBridge _actorsOperationalMaterializationHandoffBridge;

        public static void ComposeRuntime()
        {
            if (_runtimeComposed)
            {
                return;
            }

            EnsureActorsMaterializationExecutionBridge();
            EnsureSessionTransitionPhaseLocalEntryReadyMaterializationBridge();
            EnsureActorsOperationalMaterializationHandoffBridge();

            _runtimeComposed = true;
            DebugUtility.Log(typeof(GameplayRuntimeBootstrap),
                "[Gameplay] Runtime composition concluida (actors phase-runtime-materialized execution bridge + session-transition phase-local-entry-ready bridge).",
                DebugUtility.Colors.Info);
        }

        private static void EnsureActorsMaterializationExecutionBridge()
        {
            if (_actorsExecutionRuntimeBridge != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsMaterializationExecutionPolicyService>(out var executionPolicyService) || executionPolicyService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Gameplay] IActorsMaterializationExecutionPolicyService ausente para compor executor operacional de actors.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<SessionFlowActorsSemanticPortsAdapter>(out var semanticPortsAdapter) || semanticPortsAdapter == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Gameplay] SessionFlowActorsSemanticPortsAdapter ausente para compor executor operacional de actors.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsEnsembleService>(out var ensembleService) || ensembleService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Gameplay] IActorsEnsembleService ausente para compor executor operacional de actors.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsPresenceService>(out var presenceService) || presenceService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Gameplay] IActorsPresenceService ausente para compor executor operacional de actors.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsMaterializationPlanService>(out var planService) || planService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Gameplay] IActorsMaterializationPlanService ausente para compor executor operacional de actors.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsMaterializationOperationalExecutor>(out var executor) || executor == null)
            {
                if (!DependencyManager.Provider.TryGetGlobal<IActorsMaterializationExecutionCycleContext>(out var cycleContext) || cycleContext == null)
                {
                    cycleContext = new ActorsMaterializationExecutionCycleContext();
                    DependencyManager.Provider.RegisterGlobal<IActorsMaterializationExecutionCycleContext>(cycleContext);
                }

                IWorldSpawnServiceRegistryReadPortProvider spawnRegistryReadPortProvider = new WorldSpawnServiceRegistryReadPortProvider(DependencyManager.Provider);
                executor = new ActorsMaterializationOperationalExecutor(
                    executionPolicyService,
                    semanticPortsAdapter,
                    ensembleService,
                    presenceService,
                    planService,
                    spawnRegistryReadPortProvider,
                    cycleContext);
                DependencyManager.Provider.RegisterGlobal<IActorsMaterializationOperationalExecutor>(executor);
            }

            if (DependencyManager.Provider.TryGetGlobal<ActorsMaterializationExecutionRuntimeBridge>(out var existingBridge) && existingBridge != null)
            {
                _actorsExecutionRuntimeBridge = existingBridge;
                return;
            }

            _actorsExecutionRuntimeBridge = new ActorsMaterializationExecutionRuntimeBridge(executor);
            DependencyManager.Provider.RegisterGlobal(_actorsExecutionRuntimeBridge);
        }

        private static void EnsureSessionTransitionPhaseLocalEntryReadyMaterializationBridge()
        {
            if (_sessionTransitionPhaseLocalEntryReadyMaterializationBridge != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsMaterializationOperationalExecutor>(out var executor) || executor == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Gameplay] IActorsMaterializationOperationalExecutor ausente para compor bridge de SessionTransitionPhaseLocalEntryReady.");
            }

            if (DependencyManager.Provider.TryGetGlobal<SessionTransitionPhaseLocalEntryReadyMaterializationBridge>(out var existingBridge) && existingBridge != null)
            {
                _sessionTransitionPhaseLocalEntryReadyMaterializationBridge = existingBridge;
                return;
            }

            _sessionTransitionPhaseLocalEntryReadyMaterializationBridge = new SessionTransitionPhaseLocalEntryReadyMaterializationBridge(executor);
            DependencyManager.Provider.RegisterGlobal(_sessionTransitionPhaseLocalEntryReadyMaterializationBridge);
        }

        private static void EnsureActorsOperationalMaterializationHandoffBridge()
        {
            if (_actorsOperationalMaterializationHandoffBridge != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorsMaterializationExecutionCycleContext>(out var cycleContext) || cycleContext == null)
            {
                cycleContext = new ActorsMaterializationExecutionCycleContext();
                DependencyManager.Provider.RegisterGlobal<IActorsMaterializationExecutionCycleContext>(cycleContext);
            }

            if (DependencyManager.Provider.TryGetGlobal<ActorsOperationalMaterializationHandoffBridge>(out var existingBridge) && existingBridge != null)
            {
                _actorsOperationalMaterializationHandoffBridge = existingBridge;
                return;
            }

            _actorsOperationalMaterializationHandoffBridge = new ActorsOperationalMaterializationHandoffBridge(cycleContext);
            DependencyManager.Provider.RegisterGlobal(_actorsOperationalMaterializationHandoffBridge);
        }
    }
}
