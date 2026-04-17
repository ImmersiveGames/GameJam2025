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
                "[OBS][GameplaySessionFlow][SessionIntegration] seam='SessionIntegration' executor='SessionIntegrationContextService' role='canonical-session-integration-seam'.",
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
                $"[OBS][GameplaySessionFlow][SessionIntegration] SessionIntegrationContextCleared seam='SessionIntegration' executor='SessionIntegrationContextService' reason='{normalizedReason}'.",
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
                $"[OBS][SessionIntegration][InputModes] requestPublisher='SessionIntegrationContextService' seam='SessionIntegration' target='InputModeCoordinator' kind='{kind}' reason='{normalizedReason}' semanticSource='{normalizedSemanticSource}' contextSignature='{normalizedContextSignature}'.",
                DebugUtility.Colors.Info);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(
                    kind,
                    normalizedReason,
                    "SessionIntegration",
                    normalizedContextSignature));
        }
    }

    /// <summary>
    /// Eixos futuros que devem crescer acima do baseline sem reintroduzir ownership semantico nele.
    /// </summary>
    public enum SessionIntegrationExtensionPointKind
    {
        Actors = 0,
        BindersAndInteractions = 1,
        SessionTransitionExpansion = 2,
        SemanticBlocksAboveBaseline = 3,
    }

    /// <summary>
    /// Contrato minimo para nomear pontos de crescimento futuros do seam de SessionIntegration.
    /// Nao executa nada; apenas explicita a topologia de extensao esperada.
    /// </summary>
    public readonly struct SessionIntegrationExtensionPoint
    {
        public SessionIntegrationExtensionPoint(
            SessionIntegrationExtensionPointKind kind,
            string anchorModule,
            string entryPoint,
            string description)
        {
            Kind = kind;
            AnchorModule = string.IsNullOrWhiteSpace(anchorModule) ? string.Empty : anchorModule.Trim();
            EntryPoint = string.IsNullOrWhiteSpace(entryPoint) ? string.Empty : entryPoint.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        }

        public SessionIntegrationExtensionPointKind Kind { get; }
        public string AnchorModule { get; }
        public string EntryPoint { get; }
        public string Description { get; }

        public override string ToString()
        {
            return $"Kind='{Kind}', AnchorModule='{AnchorModule}', EntryPoint='{EntryPoint}', Description='{Description}'";
        }
    }

    public static class SessionIntegrationExtensionPoints
    {
        public static SessionIntegrationExtensionPoint Actors =>
            new(
                SessionIntegrationExtensionPointKind.Actors,
                anchorModule: "Game/Gameplay/Actors",
                entryPoint: "SessionIntegration",
                description: "Future actor growth should consume the session seam before touching baseline wiring.");

        public static SessionIntegrationExtensionPoint BindersAndInteractions =>
            new(
                SessionIntegrationExtensionPointKind.BindersAndInteractions,
                anchorModule: "Experience/Gameplay and Experience/Frontend",
                entryPoint: "SessionIntegration",
                description: "Future binders/interactions should enter as thin adapters that consume the session seam.");

        public static SessionIntegrationExtensionPoint SessionTransitionExpansion =>
            new(
                SessionIntegrationExtensionPointKind.SessionTransitionExpansion,
                anchorModule: "Orchestration/SessionTransition",
                entryPoint: "SessionIntegration",
                description: "Future session-transition axes should stay declarative above the baseline and reuse the existing transition vocabulary.");

        public static SessionIntegrationExtensionPoint SemanticBlocksAboveBaseline =>
            new(
                SessionIntegrationExtensionPointKind.SemanticBlocksAboveBaseline,
                anchorModule: "Orchestration/GameplaySessionFlow",
                entryPoint: "SessionIntegration",
                description: "New semantic blocks should enter through a composed seam, not through bootstrap opportunism or baseline ownership.");
    }
}
