using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Serviço responsável por resolver LevelId -> SceneRouteId + payload.
    /// </summary>
    public interface ILevelFlowService
    {
        bool TryResolve(LevelId levelId, out SceneRouteId routeId, out SceneTransitionPayload payload);
    }
}
