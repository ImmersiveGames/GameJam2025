#nullable enable
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime
{
    public readonly struct IntroStageSession
    {
        public IntroStageSession(
            PhaseDefinitionAsset? phaseDefinitionRef,
            string localContentId,
            string reason,
            int selectionVersion,
            string sessionSignature,
            GameObject? introPresenterPrefab,
            bool hasIntroStage,
            bool hasRunResultStage)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            LocalContentId = string.IsNullOrWhiteSpace(localContentId) ? string.Empty : localContentId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            SessionSignature = string.IsNullOrWhiteSpace(sessionSignature) ? string.Empty : sessionSignature.Trim();
            IntroPresenterPrefab = introPresenterPrefab;
            HasIntroStage = hasIntroStage;
            HasRunResultStage = hasRunResultStage;
        }

        public PhaseDefinitionAsset? PhaseDefinitionRef { get; }
        public string LocalContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string SessionSignature { get; }
        public GameObject? IntroPresenterPrefab { get; }
        public GameObject? PresenterPrefab => IntroPresenterPrefab;
        public bool HasIntroStage { get; }
        public bool HasRunResultStage { get; }

        public bool HasPhaseDefinitionRef => PhaseDefinitionRef != null;
        public bool HasPresenterPrefab => IntroPresenterPrefab != null;
        public bool IsValid => HasPhaseDefinitionRef;

        public static IntroStageSession Empty => default;
    }
}
