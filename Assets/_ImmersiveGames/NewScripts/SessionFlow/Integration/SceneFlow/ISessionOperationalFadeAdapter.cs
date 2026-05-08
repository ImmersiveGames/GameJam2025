using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    public interface ISessionOperationalFadeAdapter
    {
        Task FadeInAsync(SessionOperationalRouteCommand command);
        Task FadeOutAsync(SessionOperationalRouteCommand command);
    }
}
