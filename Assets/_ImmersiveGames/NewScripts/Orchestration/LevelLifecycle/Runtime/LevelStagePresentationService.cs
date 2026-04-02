using System;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public class LevelLifecycleStagePresentationService : ILevelStagePresentationService
    {
        private readonly ILevelIntroStageSessionService _introStageSessionService;

        public LevelLifecycleStagePresentationService(ILevelIntroStageSessionService introStageSessionService)
        {
            _introStageSessionService = introStageSessionService ?? throw new ArgumentNullException(nameof(introStageSessionService));
        }

        public bool TryGetCurrentContract(out LevelStagePresentationContract contract)
        {
            contract = default;

            if (!_introStageSessionService.TryGetCurrentSession(out _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime.LevelIntroStageSession session) ||
                !session.IsValid ||
                !session.HasLevelRef)
            {
                return false;
            }

            LevelDefinitionAsset levelRef = session.LevelRef;
            if (levelRef == null)
            {
                return false;
            }

            contract = new LevelStagePresentationContract(
                levelRef,
                session.LevelSignature,
                session.SelectionVersion,
                session.LocalContentId,
                session.HasIntroStage,
                levelRef.HasPostRunReactionHook);

            return contract.IsValid;
        }
    }

    [Obsolete("Use LevelLifecycleStagePresentationService instead.")]
    public sealed class LevelStagePresentationService : LevelLifecycleStagePresentationService
    {
        public LevelStagePresentationService(ILevelIntroStageSessionService introStageSessionService)
            : base(introStageSessionService)
        {
        }
    }
}
