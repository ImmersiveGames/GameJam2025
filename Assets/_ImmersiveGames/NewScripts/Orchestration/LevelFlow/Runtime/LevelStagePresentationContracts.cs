using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime
{
    public readonly struct LevelStagePresentationContract
    {
        public LevelStagePresentationContract(
            LevelDefinitionAsset levelRef,
            string levelSignature,
            int selectionVersion,
            string localContentId,
            bool hasIntroStage,
            bool hasPostRunReactionHook)
        {
            LevelRef = levelRef;
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LocalContentId = string.IsNullOrWhiteSpace(localContentId) ? string.Empty : localContentId.Trim();
            HasIntroStage = hasIntroStage;
            HasPostRunReactionHook = hasPostRunReactionHook;
        }

        public LevelDefinitionAsset LevelRef { get; }
        public string LevelSignature { get; }
        public int SelectionVersion { get; }
        public string LocalContentId { get; }
        public bool HasIntroStage { get; }
        public bool HasPostRunReactionHook { get; }

        public bool IsValid => LevelRef != null;
    }

    public interface ILevelStagePresentationService
    {
        bool TryGetCurrentContract(out LevelStagePresentationContract contract);
    }
}

