using System;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Core.Logging;

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
                !session.IsValid)
            {
                return false;
            }

            PhaseDefinitionAsset phaseDefinitionRef = session.PhaseDefinitionRef;
            if (phaseDefinitionRef == null)
            {
                return false;
            }

            bool hasRunResultStage = phaseDefinitionRef.RunResultStage != null && phaseDefinitionRef.RunResultStage.hasRunResultStage;

            contract = new LevelStagePresentationContract(
                phaseDefinitionRef,
                session.LevelRef,
                session.LevelSignature,
                session.SelectionVersion,
                session.LocalContentId,
                session.HasIntroStage,
                hasRunResultStage);

            DebugUtility.Log<LevelLifecycleStagePresentationService>(
                $"[OBS][RunResultStage] LevelStagePresentationContractResolved rail='phase' phaseRef='{phaseDefinitionRef.name}' hasRunResultStage='{hasRunResultStage}' signature='{session.LevelSignature}'.",
                DebugUtility.Colors.Info);

            return contract.IsValid;
        }
    }

    [Obsolete("Compat alias only. Use LevelLifecycleStagePresentationService instead.")]
    public sealed class LevelStagePresentationService : LevelLifecycleStagePresentationService
    {
        public LevelStagePresentationService(ILevelIntroStageSessionService introStageSessionService)
            : base(introStageSessionService)
        {
        }
    }
}
