using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public interface ISceneCompositionAdapter
    {
        Task<SessionOperationalRouteCompletedFact> ApplyOperationalRouteAsync(SessionOperationalRouteCommand command);
    }
}
