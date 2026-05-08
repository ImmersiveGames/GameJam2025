using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
namespace _ImmersiveGames.NewScripts.SessionOperational.Integration
{
    public interface ISessionOperationalAudioAdapter
    {
        void PlayRouteRevealAudio(SessionOperationalRouteCommand command);
    }
}

