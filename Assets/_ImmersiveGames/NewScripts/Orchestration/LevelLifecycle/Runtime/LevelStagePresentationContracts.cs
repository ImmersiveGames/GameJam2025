using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public readonly struct LevelStagePresentationContract
    {
        public LevelStagePresentationContract(
            PhaseDefinitionAsset phaseDefinitionRef,
            LevelDefinitionAsset levelRef,
            string levelSignature,
            int selectionVersion,
            string localContentId,
            bool hasIntroStage,
            bool hasRunResultStage)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            LevelRef = levelRef;
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LocalContentId = string.IsNullOrWhiteSpace(localContentId) ? string.Empty : localContentId.Trim();
            HasIntroStage = hasIntroStage;
            HasRunResultStage = hasRunResultStage;
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public LevelDefinitionAsset LevelRef { get; }
        public string LevelSignature { get; }
        public int SelectionVersion { get; }
        public string LocalContentId { get; }
        public bool HasIntroStage { get; }
        public bool HasRunResultStage { get; }

        public bool IsValid => PhaseDefinitionRef != null || LevelRef != null;
    }

    public interface ILevelStagePresentationService
    {
        bool TryGetCurrentContract(out LevelStagePresentationContract contract);
    }
}
