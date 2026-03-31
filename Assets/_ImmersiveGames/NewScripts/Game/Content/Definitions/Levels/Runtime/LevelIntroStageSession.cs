#nullable enable
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
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
            LevelDefinitionAsset levelRef,
            string localContentId,
            string reason,
            int selectionVersion,
            string levelSignature,
            GameObject? presenterPrefab,
            LevelIntroStageDisposition disposition)
        {
            LevelRef = levelRef;
            LocalContentId = string.IsNullOrWhiteSpace(localContentId) ? string.Empty : localContentId.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
            PresenterPrefab = presenterPrefab;
            Disposition = disposition;
        }

        public LevelDefinitionAsset LevelRef { get; }
        public string LocalContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string LevelSignature { get; }
        public GameObject? PresenterPrefab { get; }
        public LevelIntroStageDisposition Disposition { get; }

        public bool HasIntroStage => Disposition == LevelIntroStageDisposition.HasIntro;
        public bool HasLevelRef => LevelRef != null;
        public bool HasPresenterPrefab => PresenterPrefab != null;
        public bool IsValid => HasLevelRef;

        public static LevelIntroStageSession Empty => default;
    }
}
