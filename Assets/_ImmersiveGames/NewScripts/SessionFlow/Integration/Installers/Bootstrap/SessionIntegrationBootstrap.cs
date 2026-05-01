using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Context;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.InputModes;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Installers.Bootstrap;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Installers.Bootstrap
{
    public static class SessionIntegrationBootstrap
    {
        private static bool _installerPhaseComposed;
        private static bool _runtimeComposed;
        private static GameplayParticipationInputModeBridge _participationInputModeBridge;
        private static IGameplayInteractionReadinessService _gameplayInteractionReadinessService;
        private static IPhaseEntryReadinessFactProducer _phaseEntryReadinessFactProducer;

        public static void ComposeInstallerPhase()
        {
            if (_installerPhaseComposed)
            {
                return;
            }

            _installerPhaseComposed = true;

            DebugUtility.Log(typeof(SessionIntegrationBootstrap),
                "[OBS][SessionIntegration][Operational] Installer phase no-op completed; runtime composition deferred to Navigation-backed bootstrap phase.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            if (_runtimeComposed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] BootstrapConfigAsset obrigatorio ausente para compor o runtime.");
            }

            SessionIntegrationSeamRuntimeComposition.EnsureComposed();
            SessionIntegrationContinuityRuntimeComposition.EnsureComposed(bootstrapConfig);
            SessionIntegrationBridgesRuntimeComposition.EnsureComposed(
                ref _participationInputModeBridge,
                ref _gameplayInteractionReadinessService,
                ref _phaseEntryReadinessFactProducer);
            SessionIntegrationOperationalHandoffRuntimeComposition.EnsureComposed();

            _runtimeComposed = true;

            DebugUtility.Log(typeof(SessionIntegrationBootstrap),
                "[OBS][SessionIntegration][Operational] Runtime composition concluida.",
                DebugUtility.Colors.Info);
        }
    }

    internal static class SessionIntegrationSeamRuntimeComposition
    {
        public static void EnsureComposed()
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationContextService>(out var seam) || seam == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] ISessionIntegrationContextService ausente no DI global antes de compor SessionIntegration runtime.");
            }

            if (seam is not SessionIntegrationContextService)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] ISessionIntegrationContextService precisa ser o seam canonico SessionIntegrationContextService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationInputModeEmitter>(out var emitter) || emitter == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] ISessionIntegrationInputModeEmitter ausente no DI global antes de compor SessionIntegration runtime.");
            }

            DebugUtility.LogVerbose(typeof(SessionIntegrationSeamRuntimeComposition),
                "[OBS][SessionIntegration][Core] SessionIntegration seam availability validated before runtime composition.",
                DebugUtility.Colors.Info);
        }
    }

    internal static class SessionIntegrationBridgesRuntimeComposition
    {
        public static void EnsureComposed(
            ref GameplayParticipationInputModeBridge participationInputModeBridge,
            ref IGameplayInteractionReadinessService gameplayInteractionReadinessService,
            ref IPhaseEntryReadinessFactProducer phaseEntryReadinessFactProducer)
        {
            EnsureParticipationInputModeBridge(ref participationInputModeBridge);
            EnsureGameplayInteractionReadinessService(ref gameplayInteractionReadinessService);
            EnsurePhaseEntryReadinessFactProducer(ref phaseEntryReadinessFactProducer);
        }

        private static void EnsureParticipationInputModeBridge(ref GameplayParticipationInputModeBridge participationInputModeBridge)
        {
            if (participationInputModeBridge == null)
            {
                if (DependencyManager.Provider.TryGetGlobal<GameplayParticipationInputModeBridge>(out var existing) && existing != null)
                {
                    participationInputModeBridge = existing;
                }
                else
                {
                    participationInputModeBridge = new GameplayParticipationInputModeBridge();
                    DependencyManager.Provider.RegisterGlobal(participationInputModeBridge);
                }
            }

            DebugUtility.LogVerbose(typeof(SessionIntegrationBridgesRuntimeComposition),
                "[OBS][SessionIntegration][InputModes] GameplayParticipationInputModeBridge composed in SessionIntegration runtime.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureGameplayInteractionReadinessService(ref IGameplayInteractionReadinessService gameplayInteractionReadinessService)
        {
            if (gameplayInteractionReadinessService == null)
            {
                if (DependencyManager.Provider.TryGetGlobal<IGameplayInteractionReadinessService>(out var existing) && existing != null)
                {
                    gameplayInteractionReadinessService = existing;
                }
                else
                {
                    if (!DependencyManager.Provider.TryGetGlobal<IActorsGameplayOperationalReadinessService>(out var actorsOperationalReadinessService) || actorsOperationalReadinessService == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IActorsGameplayOperationalReadinessService ausente no DI global antes de registrar GameplayInteractionReadinessService.");
                    }

                    if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationInputModeEmitter>(out var inputModeEmitter) || inputModeEmitter == null)
                    {
                        throw new InvalidOperationException("[FATAL][Config][SessionIntegration] ISessionIntegrationInputModeEmitter ausente no DI global antes de registrar GameplayInteractionReadinessService.");
                    }

                    gameplayInteractionReadinessService = new GameplayInteractionReadinessService(
                        actorsOperationalReadinessService,
                        inputModeEmitter);
                    DependencyManager.Provider.RegisterGlobal<IGameplayInteractionReadinessService>(gameplayInteractionReadinessService);
                }
            }

            DebugUtility.LogVerbose(typeof(SessionIntegrationBridgesRuntimeComposition),
                "[OBS][SessionIntegration][InputModes] GameplayInteractionReadinessService composed in SessionIntegration runtime.",
                DebugUtility.Colors.Info);
        }

        private static void EnsurePhaseEntryReadinessFactProducer(ref IPhaseEntryReadinessFactProducer phaseEntryReadinessFactProducer)
        {
            if (phaseEntryReadinessFactProducer == null)
            {
                if (DependencyManager.Provider.TryGetGlobal<IPhaseEntryReadinessFactProducer>(out var existing) && existing != null)
                {
                    phaseEntryReadinessFactProducer = existing;
                }
                else
                {
                    phaseEntryReadinessFactProducer = new PhaseEntryReadinessFactProducer();
                    DependencyManager.Provider.RegisterGlobal<IPhaseEntryReadinessFactProducer>(phaseEntryReadinessFactProducer);
                }
            }

            DebugUtility.LogVerbose(typeof(SessionIntegrationBridgesRuntimeComposition),
                "[OBS][SessionIntegration][PhaseEntryReadiness] PhaseEntryReadinessFactProducer composed in SessionIntegration runtime.",
                DebugUtility.Colors.Info);
        }
    }

    internal static class SessionIntegrationOperationalHandoffRuntimeComposition
    {
        public static void EnsureComposed()
        {
            SessionTransitionBootstrap.ComposeRuntime();
        }
    }

    internal static class SessionIntegrationContinuityRuntimeComposition
    {
        public static void EnsureComposed(BootstrapConfigAsset bootstrapConfig)
        {
            EnsureGameplaySessionFlowContinuityService(bootstrapConfig);
        }

        private static void EnsureGameplaySessionFlowContinuityService(BootstrapConfigAsset bootstrapConfig)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySessionFlowContinuityService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationNavigationHandoffService>(out var navigationHandoffService) || navigationHandoffService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] ISessionIntegrationNavigationHandoffService ausente no DI global antes de registrar o IGameplaySessionFlowContinuityService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContextService) || restartContextService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IRestartContextService ausente no DI global antes de registrar o IGameplaySessionFlowContinuityService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseResetOperationalHandoffService>(out var phaseResetOperationalHandoffService) || phaseResetOperationalHandoffService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IPhaseResetOperationalHandoffService ausente no DI global antes de registrar o IGameplaySessionFlowContinuityService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogNavigationService>(out var phaseCatalogNavigationService) || phaseCatalogNavigationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IPhaseCatalogNavigationService ausente no DI global antes de registrar o IGameplaySessionFlowContinuityService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<GameplayPhaseFlowService>(out var phaseFlowService) || phaseFlowService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] GameplayPhaseFlowService ausente no DI global antes de registrar o IGameplaySessionFlowContinuityService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneCompositionExecutor>(out var sceneCompositionExecutor) || sceneCompositionExecutor == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] ISceneCompositionExecutor ausente no DI global antes de registrar o IGameplaySessionFlowContinuityService.");
            }

            IPhaseResetExecutor phaseResetExecutor = new PhaseResetExecutor(restartContextService, phaseResetOperationalHandoffService);

            var service = new GameplaySessionFlowContinuityService(
                navigationHandoffService,
                restartContextService,
                phaseResetExecutor,
                phaseCatalogNavigationService,
                phaseFlowService,
                sceneCompositionExecutor);

            DependencyManager.Provider.RegisterGlobal<IGameplaySessionFlowContinuityService>(service);

            DebugUtility.LogVerbose(typeof(SessionIntegrationContinuityRuntimeComposition),
                "[OBS][SessionIntegration][Operational] IGameplaySessionFlowContinuityService registrado como continuity seam canonical.",
                DebugUtility.Colors.Info);
        }

    }
}

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.InputModes
{
    /// <summary>
    /// Observa readiness semantica de Participation sem liberar input operacional.
    /// </summary>
    public sealed class GameplayParticipationInputModeBridge : IDisposable
    {
        private readonly EventBinding<ParticipationSnapshotChangedEvent> _participationBinding;
        private bool _disposed;
        private string _lastProcessedSignature = string.Empty;

        public GameplayParticipationInputModeBridge()
        {
            _participationBinding = new EventBinding<ParticipationSnapshotChangedEvent>(OnParticipationChanged);
            EventBus<ParticipationSnapshotChangedEvent>.Register(_participationBinding);

            DebugUtility.LogVerbose<GameplayParticipationInputModeBridge>(
                "[OBS][SessionIntegration][InputModes] GameplayParticipationInputModeBridge registered.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<ParticipationSnapshotChangedEvent>.Unregister(_participationBinding);
        }

        private void OnParticipationChanged(ParticipationSnapshotChangedEvent evt)
        {
            if (_disposed || !evt.IsValid)
            {
                return;
            }

            if (evt.IsCleared)
            {
                _lastProcessedSignature = string.Empty;
                DebugUtility.LogVerbose<GameplayParticipationInputModeBridge>(
                    $"[OBS][SessionIntegration][InputModes] Participation cleared source='{evt.Source}' reason='{evt.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            ParticipationSnapshot snapshot = evt.Snapshot;
            string signature = snapshot.Signature.Value;
            if (!string.IsNullOrWhiteSpace(signature)
                && string.Equals(_lastProcessedSignature, signature, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<GameplayParticipationInputModeBridge>(
                    $"[OBS][SessionIntegration][InputModes] Participation duplicate ignored signature='{signature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastProcessedSignature = signature;

            if (!snapshot.Readiness.CanEnterGameplay)
            {
                DebugUtility.LogVerbose<GameplayParticipationInputModeBridge>(
                    $"[OBS][SessionIntegration][InputModes] Participation not ready readinessState='{snapshot.Readiness.State}' signature='{signature}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!snapshot.TryGetLocalBindingCandidate(out ParticipantSnapshot localParticipant))
            {
                DebugUtility.LogVerbose<GameplayParticipationInputModeBridge>(
                    $"[OBS][SessionIntegration][InputModes] Local participant missing signature='{signature}' readinessState='{snapshot.Readiness.State}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            LogSemanticReadiness(snapshot, localParticipant, signature);
        }

        private static void LogSemanticReadiness(
            ParticipationSnapshot snapshot,
            ParticipantSnapshot localParticipant,
            string signature)
        {
            DebugUtility.Log(typeof(GameplayParticipationInputModeBridge),
                $"[OBS][SessionIntegration][InputModes] ParticipationSemanticReady inputModeDeferred='true' owner='ActorsExecution' signature='{signature}' readinessState='{snapshot.Readiness.State}' localParticipantId='{localParticipant.ParticipantId}' bindingHint='{localParticipant.BindingHint}' reason='{BuildReason(snapshot, localParticipant)}'.",
                DebugUtility.Colors.Info);
        }

        private static string BuildReason(ParticipationSnapshot snapshot, ParticipantSnapshot participant)
        {
            return $"Participation/{snapshot.Readiness.State}/local={participant.ParticipantId}";
        }
    }

}
