using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public interface ILevelMacroPrepareService
    {
        Task PrepareOrClearAsync(SceneRouteId macroRouteId, SceneRouteDefinitionAsset routeRef, string reason, CancellationToken ct = default);
    }
}
