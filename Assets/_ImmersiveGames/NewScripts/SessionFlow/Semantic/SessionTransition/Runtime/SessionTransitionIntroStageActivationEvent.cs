#nullable enable
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    public readonly struct SessionTransitionIntroStageActivationEvent : IEvent
    {
        public SessionTransitionIntroStageActivationEvent(
            SessionTransitionPhaseLocalEntryReadyEvent phaseLocalEntryReadyEvent,
            IntroStageSession session,
            string source,
            string reason)
        {
            PhaseLocalEntryReadyEvent = phaseLocalEntryReadyEvent;
            Session = session;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionTransitionPhaseLocalEntryReadyEvent PhaseLocalEntryReadyEvent { get; }
        public IntroStageSession Session { get; }
        public string Source { get; }
        public string Reason { get; }

        public PhaseEntryIdentity PhaseEntryIdentity => Session.PhaseEntryIdentity;
        public SceneRouteId RouteId => PhaseEntryIdentity.RouteId;
        public SceneRouteKind RouteKind => PhaseEntryIdentity.RouteKind;
        public string SessionSignature => Session.SessionSignature;
        public string PhaseSignature => Session.PhaseRuntimeSignature;
        public string CycleSignature => PhaseLocalEntryReadyEvent.CycleSignature;
        public bool HasCanonicalPayload => PhaseLocalEntryReadyEvent.HasCanonicalPayload && Session.IsValid && Session.PhaseEntryIdentity.IsValid;
        public bool IsExecute => Session.HasIntroStage;
        public bool IsSkipNoContent => !Session.HasIntroStage;

        public override string ToString()
        {
            return $"Source='{Source}', Reason='{Reason}', IsExecute='{IsExecute}', PhaseEntryIdentity='{PhaseEntryIdentity}', SessionSignature='{SessionSignature}', PhaseSignature='{PhaseSignature}', CycleSignature='{CycleSignature}'";
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
