#nullable enable
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime
{
    public enum LevelIntroStageDisposition
    {
        NoIntro = 0,
        HasIntro = 1
    }

    public readonly struct LevelIntroStageSession
    {
        public LevelIntroStageSession(
            PhaseDefinitionAsset? phaseDefinitionRef,
            LevelDefinitionAsset? compatLevelRef,
            string localContentId,
            string reason,
            int selectionVersion,
            string levelSignature,
            GameObject? introPresenterPrefab,
            LevelIntroStageDisposition disposition)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            CompatLevelRef = compatLevelRef;
            LocalContentId = string.IsNullOrWhiteSpace(localContentId) ? string.Empty : localContentId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
            IntroPresenterPrefab = introPresenterPrefab;
            Disposition = disposition;
        }

        public LevelIntroStageSession(
            LevelDefinitionAsset? levelRef,
            string localContentId,
            string reason,
            int selectionVersion,
            string levelSignature,
            GameObject? presenterPrefab,
            LevelIntroStageDisposition disposition)
            : this(null, levelRef, localContentId, reason, selectionVersion, levelSignature, presenterPrefab, disposition)
        {
        }

        public PhaseDefinitionAsset? PhaseDefinitionRef { get; }
        public LevelDefinitionAsset? CompatLevelRef { get; }
        public LevelDefinitionAsset? LevelRef => CompatLevelRef;
        public string LocalContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string LevelSignature { get; }
        public GameObject? IntroPresenterPrefab { get; }
        public GameObject? PresenterPrefab => IntroPresenterPrefab;
        public LevelIntroStageDisposition Disposition { get; }

        public bool HasIntroStage => Disposition == LevelIntroStageDisposition.HasIntro;
        public bool HasPhaseDefinitionRef => PhaseDefinitionRef != null;
        public bool HasLevelRef => CompatLevelRef != null;
        public bool HasPresenterPrefab => IntroPresenterPrefab != null;
        public bool IsValid => HasPhaseDefinitionRef || HasLevelRef;

        public static LevelIntroStageSession Empty => default;
    }
}
