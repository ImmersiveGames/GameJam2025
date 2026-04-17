using System;
using _ImmersiveGames.NewScripts.ActorSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorSystem.Contracts.Outbound;
using _ImmersiveGames.NewScripts.ActorSystem.Integration.GameplayRuntime;
using _ImmersiveGames.NewScripts.ActorSystem.Integration.SessionFlow;
using _ImmersiveGames.NewScripts.ActorSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;

namespace _ImmersiveGames.NewScripts.ActorSystem.Integration.Bootstrap
{
    public static class ActorSystemBootstrap
    {
        private static bool _installerComposed;
        private static bool _runtimeComposed;
        private static ActorSystemParticipationRefreshBridge _refreshBridge;

        public static void ComposeInstallerPhase()
        {
            if (_installerComposed)
            {
                return;
            }

            EnsureRequiredDependenciesOrFail();
            EnsureInboundContextProvider();
            EnsureOutboundPresencePort();
            EnsureReadModelService();

            _installerComposed = true;
            DebugUtility.Log(typeof(ActorSystemBootstrap),
                "[OBS][ActorSystem] Installer phase concluida.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime()
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(ActorSystemBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            EnsureRefreshBridge();
            PrimeReadModel();

            _runtimeComposed = true;
            DebugUtility.Log(typeof(ActorSystemBootstrap),
                "[OBS][ActorSystem] Runtime composition concluida.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureRequiredDependenciesOrFail()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var participationService) || participationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorSystem] IGameplayParticipationFlowService ausente no DI global antes da composicao do ActorSystem.");
            }
        }

        private static void EnsureInboundContextProvider()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorSystemSemanticContextProvider>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var participationService) || participationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorSystem] IGameplayParticipationFlowService ausente no DI global para registrar IActorSystemSemanticContextProvider.");
            }

            var contextProvider = new SessionFlowParticipationContextAdapter(participationService);
            DependencyManager.Provider.RegisterGlobal<IActorSystemSemanticContextProvider>(contextProvider);

            DebugUtility.LogVerbose(typeof(ActorSystemBootstrap),
                "[OBS][ActorSystem] Inbound context provider registrado (source=SessionFlow/Participation).",
                DebugUtility.Colors.Info);
        }

        private static void EnsureOutboundPresencePort()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorPresenceReadPort>(out var existing) && existing != null)
            {
                return;
            }

            var readPort = new GameplayRuntimeActorPresenceReadAdapter(DependencyManager.Provider);
            DependencyManager.Provider.RegisterGlobal<IActorPresenceReadPort>(readPort);

            DebugUtility.LogVerbose(typeof(ActorSystemBootstrap),
                "[OBS][ActorSystem] Outbound presence read port registrado (source=GameplayRuntime/ActorRegistry read-only).",
                DebugUtility.Colors.Info);
        }

        private static void EnsureReadModelService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorSystemReadModelService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorSystemSemanticContextProvider>(out var contextProvider) || contextProvider == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorSystem] IActorSystemSemanticContextProvider ausente no DI global antes de registrar IActorSystemReadModelService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorPresenceReadPort>(out var presenceReadPort) || presenceReadPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorSystem] IActorPresenceReadPort ausente no DI global antes de registrar IActorSystemReadModelService.");
            }

            var readModelService = new ActorSystemReadModelService(contextProvider, presenceReadPort);
            DependencyManager.Provider.RegisterGlobal<IActorSystemReadModelService>(readModelService);
        }

        private static void EnsureRefreshBridge()
        {
            if (_refreshBridge != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<ActorSystemParticipationRefreshBridge>(out var existingBridge) && existingBridge != null)
            {
                _refreshBridge = existingBridge;
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorSystemReadModelService>(out var readModelService) || readModelService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorSystem] IActorSystemReadModelService ausente no DI global antes de registrar ActorSystemParticipationRefreshBridge.");
            }

            _refreshBridge = new ActorSystemParticipationRefreshBridge(readModelService);
            DependencyManager.Provider.RegisterGlobal(_refreshBridge);
        }

        private static void PrimeReadModel()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IActorSystemReadModelService>(out var readModelService) || readModelService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorSystem] IActorSystemReadModelService ausente no DI global na fase runtime.");
            }

            var snapshot = readModelService.Refresh();
            DebugUtility.Log(typeof(ActorSystemBootstrap),
                $"[OBS][ActorSystem] Prime refresh concluido relevantActorId='{(string.IsNullOrWhiteSpace(snapshot.RelevantActorId) ? "<none>" : snapshot.RelevantActorId)}' runtimeActorCount='{snapshot.RuntimeActorCount}' reason='{snapshot.Reason}'.",
                DebugUtility.Colors.Info);
        }
    }
}
