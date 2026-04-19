using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Context;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.InputModes;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Installers.Bootstrap;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Installers.Bootstrap
{
    public static class SessionIntegrationBootstrap
    {
        private static bool _installerPhaseComposed;
        private static bool _runtimeComposed;
        private static GameplayParticipationInputModeBridge _participationInputModeBridge;

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
            SessionIntegrationBridgesRuntimeComposition.EnsureComposed(ref _participationInputModeBridge);
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

            DebugUtility.LogVerbose(typeof(SessionIntegrationSeamRuntimeComposition),
                "[OBS][SessionIntegration][Core] SessionIntegration seam availability validated before runtime composition.",
                DebugUtility.Colors.Info);
        }
    }

    internal static class SessionIntegrationBridgesRuntimeComposition
    {
        public static void EnsureComposed(ref GameplayParticipationInputModeBridge participationInputModeBridge)
        {
            if (participationInputModeBridge != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<GameplayParticipationInputModeBridge>(out var existing) && existing != null)
            {
                participationInputModeBridge = existing;
                return;
            }

            participationInputModeBridge = new GameplayParticipationInputModeBridge();
            DependencyManager.Provider.RegisterGlobal(participationInputModeBridge);

            DebugUtility.LogVerbose(typeof(SessionIntegrationBridgesRuntimeComposition),
                "[OBS][SessionIntegration][InputModes] GameplayParticipationInputModeBridge composed in SessionIntegration runtime.",
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
            EnsureGameplaySessionRunResetService();
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

            IPhaseResetExecutor phaseResetExecutor = new PhaseResetExecutor(restartContextService, phaseResetOperationalHandoffService);
            IPhaseDefinitionCatalog phaseDefinitionCatalog = ResolveOptionalPhaseDefinitionCatalog(bootstrapConfig);

            var service = new GameplaySessionFlowContinuityService(
                navigationHandoffService,
                restartContextService,
                phaseResetExecutor,
                phaseDefinitionCatalog);

            DependencyManager.Provider.RegisterGlobal<IGameplaySessionFlowContinuityService>(service);

            DebugUtility.LogVerbose(typeof(SessionIntegrationContinuityRuntimeComposition),
                "[OBS][SessionIntegration][Operational] IGameplaySessionFlowContinuityService registrado como continuity seam canonical.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureGameplaySessionRunResetService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySessionRunResetService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContextService) || restartContextService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IRestartContextService ausente no DI global antes de registrar o IGameplaySessionRunResetService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationNavigationHandoffService>(out var navigationHandoffService) || navigationHandoffService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] ISessionIntegrationNavigationHandoffService ausente no DI global antes de registrar o IGameplaySessionRunResetService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogRuntimeStateService>(out var phaseCatalogRuntimeStateService) || phaseCatalogRuntimeStateService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IPhaseCatalogRuntimeStateService ausente no DI global antes de registrar o IGameplaySessionRunResetService.");
            }

            var service = new GameplaySessionRunResetService(
                restartContextService,
                navigationHandoffService,
                phaseCatalogRuntimeStateService);

            DependencyManager.Provider.RegisterGlobal<IGameplaySessionRunResetService>(service);

            DebugUtility.LogVerbose(typeof(SessionIntegrationContinuityRuntimeComposition),
                "[OBS][SessionIntegration][Operational] IGameplaySessionRunResetService registrado como run-reset seam canonical.",
                DebugUtility.Colors.Info);
        }

        private static IPhaseDefinitionCatalog ResolveOptionalPhaseDefinitionCatalog(BootstrapConfigAsset bootstrapConfig)
        {
            if (bootstrapConfig?.NavigationCatalog is not GameNavigationCatalogAsset navigationCatalog)
            {
                return null;
            }

            if (!navigationCatalog.IsGameplayPhaseEnabledOrFail())
            {
                return null;
            }

            return navigationCatalog.ResolveGameplayPhaseCatalogOrFail();
        }
    }
}

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.InputModes
{
    /// <summary>
    /// Bridge semantico -> request de InputMode via seam oficial de SessionIntegration.
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

            PublishGameplayInputModeRequest(snapshot, localParticipant, signature);
        }

        private static void PublishGameplayInputModeRequest(
            ParticipationSnapshot snapshot,
            ParticipantSnapshot localParticipant,
            string signature)
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationContextService>(out var sessionIntegration) || sessionIntegration == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayParticipationInputModeBridge),
                    $"[FATAL][H1][SessionIntegration] ISessionIntegrationContextService indisponivel para InputMode gameplay. signature='{signature}' readinessState='{snapshot.Readiness.State}'.");
                return;
            }

            sessionIntegration.RequestGameplayInputMode(
                BuildReason(snapshot, localParticipant),
                "GameplayParticipation",
                signature);

            DebugUtility.Log(typeof(GameplayParticipationInputModeBridge),
                $"[OBS][SessionIntegration][InputModes] GameplayInputMode requested source='Participation' signature='{signature}' localParticipantId='{localParticipant.ParticipantId}' bindingHint='{localParticipant.BindingHint}'.",
                DebugUtility.Colors.Info);
        }

        private static string BuildReason(ParticipationSnapshot snapshot, ParticipantSnapshot participant)
        {
            return $"Participation/{snapshot.Readiness.State}/local={participant.ParticipantId}";
        }
    }
}

