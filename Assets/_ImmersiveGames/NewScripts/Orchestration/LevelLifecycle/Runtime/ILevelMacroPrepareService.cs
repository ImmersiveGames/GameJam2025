using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public interface ILevelMacroPrepareService
    {
        Task PrepareOrClearAsync(SceneRouteId macroRouteId, SceneRouteDefinitionAsset routeRef, string reason, CancellationToken ct = default);
    }
}
