using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime
{
    /// <summary>
    /// Seam explícito entre LevelFlow operacional e conteúdo/definitions authoring-driven.
    /// </summary>
    public interface ILevelFlowContentService
    {
        LevelCollectionAsset ResolveLevelCollectionOrFail(SceneRouteDefinitionAsset routeAsset, SceneRouteId macroRouteId, string signature, string reason);

        GameplayContentManifest ResolveGameplayContentManifestOrFail(
            LevelDefinitionAsset levelRef,
            SceneRouteId macroRouteId,
            string signature,
            string reason);

        LevelDefinitionAsset ResolveSelectedLevelDefinitionOrFail(
            LevelCollectionAsset levelCollection,
            bool useSnapshot,
            GameplayStartSnapshot snapshot,
            SceneRouteId macroRouteId,
            SceneRouteKind routeKind,
            string signature,
            string reason);

        LevelDefinitionAsset ResolveNextLevelOrFail(GameplayStartSnapshot snapshot, string reason);

        string BuildLevelSignature(LevelDefinitionAsset levelRef, SceneRouteId routeId, string reason);

        string BuildLocalContentId(LevelDefinitionAsset levelRef, string contentId = null);
    }
}
