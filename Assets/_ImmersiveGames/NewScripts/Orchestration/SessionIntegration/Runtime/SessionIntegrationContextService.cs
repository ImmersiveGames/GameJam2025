using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.InputModes.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.SessionIntegration.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionIntegrationContextService : ISessionIntegrationContextService
    {
        public SessionIntegrationContextService(
            IGameplaySessionContextService sessionContextService,
            IGameplayPhaseRuntimeService phaseRuntimeService,
            IGameplayParticipationFlowService participationService)
        {
            SessionContextService = sessionContextService ?? throw new ArgumentNullException(nameof(sessionContextService));
            PhaseRuntimeService = phaseRuntimeService ?? throw new ArgumentNullException(nameof(phaseRuntimeService));
            ParticipationService = participationService ?? throw new ArgumentNullException(nameof(participationService));

            DebugUtility.LogVerbose<SessionIntegrationContextService>(
                "[OBS][GameplaySessionFlow][SessionIntegration] SessionIntegrationContextService registrado como seam operacional de integracao de sessao.",
                DebugUtility.Colors.Info);
        }

        public IGameplaySessionContextService SessionContextService { get; }
        public IGameplayPhaseRuntimeService PhaseRuntimeService { get; }
        public IGameplayParticipationFlowService ParticipationService { get; }

        public SessionIntegrationContextSnapshot Current => ComposeCurrentSnapshot();

        public bool TryGetCurrent(out SessionIntegrationContextSnapshot snapshot)
        {
            snapshot = ComposeCurrentSnapshot();
            return snapshot.HasAnyContext;
        }

        public bool TryGetCurrentSessionContext(out GameplaySessionContextSnapshot snapshot)
        {
            return SessionContextService.TryGetCurrent(out snapshot);
        }

        public bool TryGetCurrentPhaseRuntime(out GameplayPhaseRuntimeSnapshot snapshot)
        {
            return PhaseRuntimeService.TryGetCurrent(out snapshot);
        }

        public bool TryGetCurrentParticipation(out ParticipationSnapshot snapshot)
        {
            return ParticipationService.TryGetCurrent(out snapshot);
        }

        public void RequestGameplayInputMode(string reason, string semanticSource, string contextSignature = "")
        {
            PublishInputModeRequest(
                InputModeRequestKind.Gameplay,
                reason,
                semanticSource,
                contextSignature);
        }

        public void RequestFrontendMenuInputMode(string reason, string semanticSource, string contextSignature = "")
        {
            PublishInputModeRequest(
                InputModeRequestKind.FrontendMenu,
                reason,
                semanticSource,
                contextSignature);
        }

        public void RequestPauseOverlayInputMode(string reason, string semanticSource, string contextSignature = "")
        {
            PublishInputModeRequest(
                InputModeRequestKind.PauseOverlay,
                reason,
                semanticSource,
                contextSignature);
        }

        public void Clear(string reason = null)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "SessionIntegration/Clear" : reason.Trim();

            SessionContextService.Clear(normalizedReason);

            if (!ReferenceEquals(SessionContextService, PhaseRuntimeService))
            {
                PhaseRuntimeService.Clear(normalizedReason);
            }

            if (!ReferenceEquals(SessionContextService, ParticipationService) && !ReferenceEquals(PhaseRuntimeService, ParticipationService))
            {
                ParticipationService.Clear(normalizedReason);
            }

            DebugUtility.LogVerbose<SessionIntegrationContextService>(
                $"[OBS][GameplaySessionFlow][SessionIntegration] SessionIntegrationContextCleared reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);
        }

        private SessionIntegrationContextSnapshot ComposeCurrentSnapshot()
        {
            TryGetCurrentSessionContext(out var sessionContext);
            TryGetCurrentPhaseRuntime(out var phaseRuntime);
            TryGetCurrentParticipation(out var participation);

            return new SessionIntegrationContextSnapshot(sessionContext, phaseRuntime, participation);
        }

        private static void PublishInputModeRequest(
            InputModeRequestKind kind,
            string reason,
            string semanticSource,
            string contextSignature)
        {
            if (kind == InputModeRequestKind.Unspecified)
            {
                HardFailFastH1.Trigger(typeof(SessionIntegrationContextService),
                    "[FATAL][H1][InputModes] Unspecified InputModeRequestKind is not supported by SessionIntegration.");
                return;
            }

            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            string normalizedSemanticSource = string.IsNullOrWhiteSpace(semanticSource) ? "<none>" : semanticSource.Trim();
            string normalizedContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();

            DebugUtility.Log(typeof(SessionIntegrationContextService),
                $"[OBS][SessionIntegration][InputModes] Request kind='{kind}' reason='{normalizedReason}' semanticSource='{normalizedSemanticSource}' contextSignature='{normalizedContextSignature}'.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(
                    kind,
                    normalizedReason,
                    "SessionIntegration",
                    normalizedContextSignature));
        }
    }
}
