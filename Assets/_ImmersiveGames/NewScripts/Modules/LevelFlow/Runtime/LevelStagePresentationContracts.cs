using System;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public readonly struct LevelStagePresentationContract
    {
        public LevelStagePresentationContract(
            LevelDefinitionAsset levelRef,
            string levelSignature,
            int selectionVersion,
            bool hasIntroStage,
            bool hasPostGameReactionHook)
        {
            LevelRef = levelRef;
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            HasIntroStage = hasIntroStage;
            HasPostGameReactionHook = hasPostGameReactionHook;
        }

        public LevelDefinitionAsset LevelRef { get; }
        public string LevelSignature { get; }
        public int SelectionVersion { get; }
        public bool HasIntroStage { get; }
        public bool HasPostGameReactionHook { get; }

        public bool IsValid => LevelRef != null;
    }

    public interface ILevelStagePresentationService
    {
        bool TryGetCurrentContract(out LevelStagePresentationContract contract);
    }
}
