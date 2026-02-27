using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Serviço responsável por resolver LevelId -> MacroRouteId(SceneRouteId) + contentId + payload.
    /// </summary>
    public interface ILevelFlowService
    {
        bool TryResolve(LevelId levelId, out SceneRouteId macroRouteId, out string contentId, out SceneTransitionPayload payload);
        bool TryResolveLevelId(SceneRouteId routeId, out LevelId levelId);
    }
}
