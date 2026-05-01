using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    /// <summary>
    /// Origin explícita da transição de SessionTransition.
    /// Distingue entrada inicial de continuidade de run anterior.
    /// </summary>
    public enum SessionTransitionOrigin
    {
        Unknown = 0,              /// Origem não classificada (inválida para execução)
        InitialEntry = 1,         /// Entrada inicial (Menu → Gameplay, SceneFlow → primeira fase)
        PostRunContinuation = 2,  /// Continuidade de run anterior (RunContinuationSelection)
        PhaseNavigation = 3,      /// Navegação intra-phase (AdvancePhase)
    }

    /// <summary>
    /// Intenção local de SessionTransition.
    /// InitialEntry é contrato próprio desta camada e não depende de RunContinuationKind.
    /// </summary>
    public enum SessionTransitionIntentKind
    {
        Unknown = 0,
        InitialEntry = 1,
        AdvancePhase = 2,
        RestartCurrentPhase = 3,
        ExitToMenu = 4,
        TerminateRun = 5,
        RestartFromFirstPhase = 6,
        PhaseOrdinalNavigation = 7,
    }

    public readonly struct SessionTransitionContext
    {
        private readonly string _contextSignature;
        private readonly string _sceneName;
        private readonly string _profile;
        private readonly string _reason;
        private readonly string _nextState;
        private readonly SceneRouteId _routeId;
        private readonly SceneRouteKind _routeKind;
        private readonly SceneRouteDefinitionAsset _routeRef;
        private readonly PhaseOrdinalNavigationKind _ordinalNavigationKind;
        private readonly string _ordinalNavigationTargetPhaseId;

        public SessionTransitionContext(RunContinuationSelection resolvedSelection)
            : this(resolvedSelection, SessionTransitionOrigin.PostRunContinuation)
        {
        }

        public SessionTransitionContext(
            RunContinuationSelection resolvedSelection,
            SessionTransitionOrigin origin)
        {
            ResolvedSelection = resolvedSelection;
            Origin = origin;
            IntentKind = MapContinuationKind(resolvedSelection.SelectedContinuation);
            _contextSignature = string.Empty;
            _sceneName = string.Empty;
            _profile = string.Empty;
            _reason = string.Empty;
            _nextState = string.Empty;
            _routeId = default;
            _routeKind = SceneRouteKind.Unspecified;
            _routeRef = null;
            _ordinalNavigationKind = PhaseOrdinalNavigationKind.Unknown;
            _ordinalNavigationTargetPhaseId = string.Empty;
        }

        private SessionTransitionContext(
            SessionTransitionOrigin origin,
            SessionTransitionIntentKind intentKind,
            string contextSignature,
            string sceneName,
            string profile,
            string reason,
            string nextState)
            : this(
                origin,
                intentKind,
                contextSignature,
                sceneName,
                profile,
                reason,
                nextState,
                default,
                SceneRouteKind.Unspecified,
                null,
                PhaseOrdinalNavigationKind.Unknown,
                string.Empty)
        {
        }

        private SessionTransitionContext(
            SessionTransitionOrigin origin,
            SessionTransitionIntentKind intentKind,
            string contextSignature,
            string sceneName,
            string profile,
            string reason,
            string nextState,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            SceneRouteDefinitionAsset routeRef,
            PhaseOrdinalNavigationKind ordinalNavigationKind,
            string ordinalNavigationTargetPhaseId)
        {
            ResolvedSelection = default;
            Origin = origin;
            IntentKind = intentKind;
            _contextSignature = Normalize(contextSignature);
            _sceneName = Normalize(sceneName);
            _profile = Normalize(profile);
            _reason = Normalize(reason);
            _nextState = Normalize(nextState);
            _routeId = routeId;
            _routeKind = routeKind;
            _routeRef = routeRef;
            _ordinalNavigationKind = ordinalNavigationKind;
            _ordinalNavigationTargetPhaseId = Normalize(ordinalNavigationTargetPhaseId);
        }

        public RunContinuationSelection ResolvedSelection { get; }
        public SessionTransitionOrigin Origin { get; }
        public SessionTransitionIntentKind IntentKind { get; }
        public bool HasRunContinuationSelection =>
            Origin == SessionTransitionOrigin.PostRunContinuation &&
            ResolvedSelection.IsValid;

        public RunContinuationContext ContinuationContext
        {
            get
            {
                if (!HasRunContinuationSelection)
                {
                    HardFailFastH1.Trigger(typeof(SessionTransitionContext),
                        $"[FATAL][H1][SessionTransition] ContinuationContext acessado em contexto que nao usa RunContinuationSelection. origin='{Origin}' intent='{IntentKind}' reason='{Reason}'.");
                    return default;
                }

                return ResolvedSelection.ContinuationContext;
            }
        }

        public RunContinuationKind ResolvedContinuation => HasRunContinuationSelection
            ? ResolvedSelection.SelectedContinuation
            : RunContinuationKind.Unknown;

        public RunDecisionCompletion Completion => HasRunContinuationSelection
            ? ResolvedSelection.Completion
            : default;

        public string ContextSignature => HasRunContinuationSelection
            ? ResolvedSelection.ContinuationContext.Signature
            : _contextSignature;

        public string SceneName => HasRunContinuationSelection
            ? ResolvedSelection.ContinuationContext.SceneName
            : _sceneName;

        public string Profile => HasRunContinuationSelection
            ? ResolvedSelection.ContinuationContext.Profile
            : _profile;

        public string Reason => HasRunContinuationSelection
            ? ResolvedSelection.Reason
            : _reason;

        public string NextState => HasRunContinuationSelection
            ? ResolvedSelection.NextState
            : _nextState;
        public SceneRouteId RouteId => _routeId;
        public SceneRouteKind RouteKind => _routeKind;
        public SceneRouteDefinitionAsset RouteRef => _routeRef;
        public bool IsGameplayInitialEntry => Origin == SessionTransitionOrigin.InitialEntry && IntentKind == SessionTransitionIntentKind.InitialEntry;
        public bool IsGameplayReentry => Origin == SessionTransitionOrigin.PhaseNavigation && IntentKind == SessionTransitionIntentKind.PhaseOrdinalNavigation;

        public bool IsValid
        {
            get
            {
                if (Origin == SessionTransitionOrigin.Unknown || IntentKind == SessionTransitionIntentKind.Unknown)
                {
                    return false;
                }

                if (Origin == SessionTransitionOrigin.InitialEntry)
                {
                    return IntentKind == SessionTransitionIntentKind.InitialEntry &&
                           !ResolvedSelection.IsValid &&
                           !string.IsNullOrWhiteSpace(ContextSignature) &&
                           !string.IsNullOrWhiteSpace(SceneName) &&
                           RouteId.IsValid &&
                           RouteRef != null &&
                           RouteKind == SceneRouteKind.Gameplay;
                }

                if (Origin == SessionTransitionOrigin.PhaseNavigation)
                {
                    return IntentKind == SessionTransitionIntentKind.PhaseOrdinalNavigation &&
                           !ResolvedSelection.IsValid &&
                           !string.IsNullOrWhiteSpace(ContextSignature) &&
                           !string.IsNullOrWhiteSpace(SceneName) &&
                           OrdinalNavigationKind != PhaseOrdinalNavigationKind.Unknown;
                }

                return HasRunContinuationSelection;
            }
        }

        public PhaseOrdinalNavigationKind OrdinalNavigationKind => _ordinalNavigationKind;
        public string OrdinalNavigationTargetPhaseId => _ordinalNavigationTargetPhaseId;

        public static SessionTransitionContext CreateInitialEntry(
            string contextSignature,
            string sceneName,
            string profile,
            string reason,
            string nextState)
        {
            if (string.IsNullOrWhiteSpace(contextSignature))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionContext),
                    "[FATAL][H1][SessionTransition] InitialEntry sem contextSignature tipada.");
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionContext),
                    "[FATAL][H1][SessionTransition] InitialEntry sem sceneName tipada.");
            }

            return new SessionTransitionContext(
                SessionTransitionOrigin.InitialEntry,
                SessionTransitionIntentKind.InitialEntry,
                contextSignature,
                sceneName,
                profile,
                reason,
                string.IsNullOrWhiteSpace(nextState) ? sceneName : nextState,
                default,
                SceneRouteKind.Unspecified,
                null,
                PhaseOrdinalNavigationKind.Unknown,
                string.Empty);
        }

        public static SessionTransitionContext CreateInitialEntry(SceneTransitionContext context)
        {
            if (!context.RouteId.IsValid || context.RouteRef == null || context.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionContext),
                    $"[FATAL][H1][SessionTransition] InitialEntry adapter recebeu SceneTransitionContext invalido. routeId='{context.RouteId}' routeKind='{context.RouteKind}' gameplayEntryKind='{context.GameplayEntryKind}' reason='{Normalize(context.Reason)}'.");
            }

            if (!context.IsGameplayInitialEntry)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionContext),
                    $"[FATAL][H1][SessionTransition] InitialEntry adapter requer payload GameplayInitialEntry. routeId='{context.RouteId}' gameplayEntryKind='{context.GameplayEntryKind}' reason='{Normalize(context.Reason)}'.");
            }

            string contextSignature = Normalize(context.ContextSignature);
            if (string.IsNullOrWhiteSpace(contextSignature))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionContext),
                    $"[FATAL][H1][SessionTransition] InitialEntry adapter recebeu contextSignature vazio. routeId='{context.RouteId}' reason='{Normalize(context.Reason)}'.");
            }

            string sceneName = Normalize(context.TargetActiveScene);
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                sceneName = Normalize(context.RouteId.Value);
            }

            return CreateInitialEntry(
                contextSignature,
                sceneName,
                context.TransitionProfileName,
                context.Reason,
                sceneName,
                context.RouteId,
                context.RouteKind,
                context.RouteRef);
        }

        private static SessionTransitionContext CreateInitialEntry(
            string contextSignature,
            string sceneName,
            string profile,
            string reason,
            string nextState,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            SceneRouteDefinitionAsset routeRef)
        {
            return new SessionTransitionContext(
                SessionTransitionOrigin.InitialEntry,
                SessionTransitionIntentKind.InitialEntry,
                contextSignature,
                sceneName,
                profile,
                reason,
                nextState,
                routeId,
                routeKind,
                routeRef,
                PhaseOrdinalNavigationKind.Unknown,
                string.Empty);
        }

        public static SessionTransitionContext CreatePhaseOrdinalNavigation(
            string contextSignature,
            string sceneName,
            string profile,
            string reason,
            string nextState,
            PhaseOrdinalNavigationKind ordinalNavigationKind,
            string targetPhaseId)
        {
            if (string.IsNullOrWhiteSpace(contextSignature))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionContext),
                    "[FATAL][H1][SessionTransition] PhaseOrdinalNavigation sem contextSignature tipada.");
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionContext),
                    "[FATAL][H1][SessionTransition] PhaseOrdinalNavigation sem sceneName tipada.");
            }

            if (ordinalNavigationKind == PhaseOrdinalNavigationKind.Unknown)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionContext),
                    "[FATAL][H1][SessionTransition] PhaseOrdinalNavigation sem kind ordinal tipado.");
            }

            return new SessionTransitionContext(
                SessionTransitionOrigin.PhaseNavigation,
                SessionTransitionIntentKind.PhaseOrdinalNavigation,
                contextSignature,
                sceneName,
                profile,
                reason,
                string.IsNullOrWhiteSpace(nextState) ? sceneName : nextState,
                default,
                SceneRouteKind.Unspecified,
                null,
                ordinalNavigationKind,
                targetPhaseId);
        }

        public static SessionTransitionIntentKind MapContinuationKind(RunContinuationKind continuationKind)
        {
            return continuationKind switch
            {
                RunContinuationKind.AdvancePhase => SessionTransitionIntentKind.AdvancePhase,
                RunContinuationKind.RestartCurrentPhase => SessionTransitionIntentKind.RestartCurrentPhase,
                RunContinuationKind.RestartFromFirstPhase => SessionTransitionIntentKind.RestartFromFirstPhase,
                RunContinuationKind.ExitToMenu => SessionTransitionIntentKind.ExitToMenu,
                RunContinuationKind.TerminateRun => SessionTransitionIntentKind.TerminateRun,
                _ => SessionTransitionIntentKind.Unknown,
            };
        }

        public override string ToString()
        {
            return $"Origin='{Origin}', Intent='{IntentKind}', RunContinuation='{ResolvedContinuation}', OrdinalNavigation='{OrdinalNavigationKind}', OrdinalTarget='{OrdinalNavigationTargetPhaseId}', Reason='{Reason}', NextState='{NextState}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionTransitionPhaseLocalEntryReadyEvent : IEvent
    {
        public SessionTransitionPhaseLocalEntryReadyEvent(
            SessionTransitionPlan plan,
            string source,
            SceneRouteId routeId = default,
            SceneRouteKind routeKind = SceneRouteKind.Unspecified,
            string sceneName = "",
            string reason = "",
            string sessionSignature = "",
            string phaseSignature = "",
            string participationSignature = "",
            string actorSetRef = "",
            string cycleSignature = "",
            PhaseEntryIdentity phaseEntryIdentity = default)
        {
            Plan = plan;
            Source = string.IsNullOrWhiteSpace(source) ? string.Empty : source.Trim();
            RouteId = routeId;
            RouteKind = routeKind;
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SessionSignature = string.IsNullOrWhiteSpace(sessionSignature) ? string.Empty : sessionSignature.Trim();
            PhaseSignature = string.IsNullOrWhiteSpace(phaseSignature) ? string.Empty : phaseSignature.Trim();
            ParticipationSignature = string.IsNullOrWhiteSpace(participationSignature) ? string.Empty : participationSignature.Trim();
            ActorSetRef = string.IsNullOrWhiteSpace(actorSetRef) ? string.Empty : actorSetRef.Trim();
            CycleSignature = string.IsNullOrWhiteSpace(cycleSignature) ? string.Empty : cycleSignature.Trim();
            PhaseEntryIdentity = phaseEntryIdentity;
        }

        public SessionTransitionPlan Plan { get; }
        public string Source { get; }
        public SceneRouteId RouteId { get; }
        public SceneRouteKind RouteKind { get; }
        public string SceneName { get; }
        public string Reason { get; }
        public string SessionSignature { get; }
        public string PhaseSignature { get; }
        public string ParticipationSignature { get; }
        public string ActorSetRef { get; }
        public string CycleSignature { get; }
        public PhaseEntryIdentity PhaseEntryIdentity { get; }
        public string EntrySignature => PhaseEntryIdentity.EntrySignature;
        public SessionTransitionContext Context => Plan.Context;
        public bool IsValid => Plan.IsValid;
        public bool HasCanonicalPayload =>
            RouteId.IsValid &&
            RouteKind != SceneRouteKind.Unspecified &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            !string.IsNullOrWhiteSpace(Reason) &&
            !string.IsNullOrWhiteSpace(SessionSignature) &&
            !string.IsNullOrWhiteSpace(PhaseSignature) &&
            !string.IsNullOrWhiteSpace(ParticipationSignature) &&
            !string.IsNullOrWhiteSpace(ActorSetRef) &&
            !string.IsNullOrWhiteSpace(CycleSignature) &&
            PhaseEntryIdentity.IsValid;
        public bool IsPhaseLocalEntry => HasCanonicalPayload;
    }
}
