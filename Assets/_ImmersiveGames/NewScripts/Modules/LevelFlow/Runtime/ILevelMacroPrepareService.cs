using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public interface ILevelMacroPrepareService
    {
        Task PrepareAsync(SceneRouteId macroRouteId, string reason, CancellationToken ct = default);
    }
}
