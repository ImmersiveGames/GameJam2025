using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    public readonly struct SessionTransitionContext
    {
        public SessionTransitionContext(RunContinuationSelection resolvedSelection)
        {
            ResolvedSelection = resolvedSelection;
        }

        public RunContinuationSelection ResolvedSelection { get; }
        public RunContinuationContext ContinuationContext => ResolvedSelection.ContinuationContext;
        public RunContinuationKind ResolvedContinuation => ResolvedSelection.SelectedContinuation;
        public RunDecisionCompletion Completion => ResolvedSelection.Completion;
        public string Reason => ResolvedSelection.Reason;
        public string NextState => ResolvedSelection.NextState;
        public bool IsValid => ResolvedSelection.IsValid;

        public override string ToString()
        {
            return $"Continuation='{ResolvedContinuation}', Reason='{Reason}', NextState='{NextState}'";
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

