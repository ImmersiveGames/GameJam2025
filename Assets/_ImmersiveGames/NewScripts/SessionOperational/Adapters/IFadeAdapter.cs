using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public interface IFadeAdapter
    {
        Task FadeInAsync(SessionOperationalRouteCommand command);
        Task FadeOutAsync(SessionOperationalRouteCommand command);
    }
}

