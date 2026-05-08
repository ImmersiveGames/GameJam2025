using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;

namespace _ImmersiveGames.NewScripts.SessionOperational.Integration.Base11Sandbox
{
    public interface ISessionOperationalFadeAdapter
    {
        Task FadeInAsync(SessionOperationalRouteCommand command);
        Task FadeOutAsync(SessionOperationalRouteCommand command);
    }
}

