#nullable enable
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition;
using UnityEngine;

namespace ImmersiveGames.GameJam2025.Orchestration.GameLoop.IntroStage.Runtime
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
            string? entrySignature = null)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            LocalContentId = string.IsNullOrWhiteSpace(localContentId) ? string.Empty : localContentId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            PhaseLocalEntrySequence = phaseLocalEntrySequence < 0 ? 0 : phaseLocalEntrySequence;
            SessionSignature = string.IsNullOrWhiteSpace(sessionSignature) ? string.Empty : sessionSignature.Trim();
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
        public string EntrySignature { get; }
        public bool HasIntroStage { get; }

        public bool HasPhaseDefinitionRef => PhaseDefinitionRef != null;
        public bool IsValid => HasPhaseDefinitionRef;

        public static IntroStageSession Empty => default;
    }
}

