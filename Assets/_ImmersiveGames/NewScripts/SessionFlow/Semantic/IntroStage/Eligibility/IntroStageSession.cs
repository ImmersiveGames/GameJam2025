#nullable enable
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility
{
    public readonly struct IntroStageSession
    {
        public IntroStageSession(
            PhaseDefinitionAsset? phaseDefinitionRef,
            string localContentId,
            string reason,
            int selectionVersion,
            int phaseLocalEntrySequence,
            string sessionSignature,
            bool hasIntroStage = false,
            string? entrySignature = null,
            string? phaseRuntimeSignature = null)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            LocalContentId = string.IsNullOrWhiteSpace(localContentId) ? string.Empty : localContentId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            PhaseLocalEntrySequence = phaseLocalEntrySequence < 0 ? 0 : phaseLocalEntrySequence;
            SessionSignature = string.IsNullOrWhiteSpace(sessionSignature) ? string.Empty : sessionSignature.Trim();
            PhaseRuntimeSignature = string.IsNullOrWhiteSpace(phaseRuntimeSignature) ? string.Empty : phaseRuntimeSignature.Trim();
            EntrySignature = string.IsNullOrWhiteSpace(entrySignature)
                ? $"{SessionSignature}|entry:{PhaseLocalEntrySequence}"
                : entrySignature.Trim();
            HasIntroStage = hasIntroStage;
        }

        public PhaseDefinitionAsset? PhaseDefinitionRef { get; }
        public string LocalContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public int PhaseLocalEntrySequence { get; }
        public string SessionSignature { get; }
        public string PhaseRuntimeSignature { get; }
        public string EntrySignature { get; }
        public bool HasIntroStage { get; }

        public bool HasPhaseDefinitionRef => PhaseDefinitionRef != null;
        public bool IsValid => HasPhaseDefinitionRef;

        public static IntroStageSession Empty => default;
    }
}

