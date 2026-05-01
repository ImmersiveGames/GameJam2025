#nullable enable
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime
{
    public readonly struct SessionTransitionGameplayInputModeCommandEvent : IEvent
    {
        public SessionTransitionGameplayInputModeCommandEvent(
            SessionTransitionPhaseLocalEntryReadyEvent phaseLocalEntryReadyEvent,
            string source,
            string reason)
        {
            PhaseLocalEntryReadyEvent = phaseLocalEntryReadyEvent;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionTransitionPhaseLocalEntryReadyEvent PhaseLocalEntryReadyEvent { get; }
        public PhaseEntryIdentity PhaseEntryIdentity => PhaseLocalEntryReadyEvent.PhaseEntryIdentity;
        public InputModeRequestKind Kind => InputModeRequestKind.Gameplay;
        public string Source { get; }
        public string Reason { get; }
        public string SessionSignature => PhaseLocalEntryReadyEvent.SessionSignature;
        public string PhaseSignature => PhaseLocalEntryReadyEvent.PhaseSignature;
        public string CycleSignature => PhaseLocalEntryReadyEvent.CycleSignature;
        public string ContextSignature => PhaseEntryIdentity.EntrySignature;
        public bool IsValid => PhaseLocalEntryReadyEvent.HasCanonicalPayload && PhaseEntryIdentity.IsValid;

        public override string ToString()
        {
            return $"Kind='{Kind}', Source='{Source}', Reason='{Reason}', PhaseEntryIdentity='{PhaseEntryIdentity}', SessionSignature='{SessionSignature}', PhaseSignature='{PhaseSignature}', CycleSignature='{CycleSignature}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
