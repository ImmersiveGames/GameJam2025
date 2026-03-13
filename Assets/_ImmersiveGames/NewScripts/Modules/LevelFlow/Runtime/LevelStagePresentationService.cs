using System;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public sealed class LevelStagePresentationService : ILevelStagePresentationService
    {
        private readonly IRestartContextService _restartContextService;

        public LevelStagePresentationService(IRestartContextService restartContextService)
        {
            _restartContextService = restartContextService ?? throw new ArgumentNullException(nameof(restartContextService));
        }

        public bool TryGetCurrentContract(out LevelStagePresentationContract contract)
        {
            contract = default;

            if (!_restartContextService.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelRef)
            {
                return false;
            }

            LevelDefinitionAsset levelRef = snapshot.LevelRef;
            if (levelRef == null)
            {
                return false;
            }

            contract = new LevelStagePresentationContract(
                levelRef,
                snapshot.LevelSignature,
                snapshot.SelectionVersion,
                levelRef.HasIntroStage,
                levelRef.HasPostGameReactionHook);

            return contract.IsValid;
        }
    }
}
