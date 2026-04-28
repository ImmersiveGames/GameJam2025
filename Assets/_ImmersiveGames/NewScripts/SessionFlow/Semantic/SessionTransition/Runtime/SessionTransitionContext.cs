using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
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
    }

    public readonly struct SessionTransitionContext
    {
        private readonly string _contextSignature;
        private readonly string _sceneName;
        private readonly string _profile;
        private readonly string _reason;
        private readonly string _nextState;

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
        }

        private SessionTransitionContext(
            SessionTransitionOrigin origin,
            SessionTransitionIntentKind intentKind,
            string contextSignature,
            string sceneName,
            string profile,
            string reason,
            string nextState)
        {
            ResolvedSelection = default;
            Origin = origin;
            IntentKind = intentKind;
            _contextSignature = Normalize(contextSignature);
            _sceneName = Normalize(sceneName);
            _profile = Normalize(profile);
            _reason = Normalize(reason);
            _nextState = Normalize(nextState);
        }

        public RunContinuationSelection ResolvedSelection { get; }
        public SessionTransitionOrigin Origin { get; }
        public SessionTransitionIntentKind IntentKind { get; }
        public bool HasRunContinuationSelection =>
            (Origin == SessionTransitionOrigin.PostRunContinuation || Origin == SessionTransitionOrigin.PhaseNavigation) &&
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
                           !string.IsNullOrWhiteSpace(SceneName);
                }

                return HasRunContinuationSelection;
            }
        }

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
                string.IsNullOrWhiteSpace(nextState) ? sceneName : nextState);
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
            return $"Origin='{Origin}', Intent='{IntentKind}', LegacyContinuation='{ResolvedContinuation}', Reason='{Reason}', NextState='{NextState}'";
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
            string cycleSignature = "")
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
        public string EntrySignature => CycleSignature;
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
            !string.IsNullOrWhiteSpace(CycleSignature);
        public bool IsPhaseLocalEntry => Plan.EmitsPhaseLocalEntryReady;
    }
}
