using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime
{
    /// <summary>
    /// Seam explicito entre o runtime ativo de LevelLifecycle e o authoring-driven content do level.
    /// O namespace e o folder permanecem historicos por compatibilidade.
    /// </summary>
    public interface ILevelFlowContentService
    {
        PhaseDefinitionAsset.PhaseSwapBlock ResolvePhaseSwapOrFail(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            string signature,
            string reason);

        PhaseDefinitionAsset ResolveNextPhaseOrFail(GameplayStartSnapshot snapshot, string reason);

        string BuildPhaseSwapSignature(PhaseDefinitionAsset phaseDefinitionRef, SceneRouteId routeId, string reason);

        string BuildPhaseSwapContentId(PhaseDefinitionAsset phaseDefinitionRef, string contentId = null);
    }
}
