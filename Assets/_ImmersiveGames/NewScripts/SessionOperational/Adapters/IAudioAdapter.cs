using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public interface IAudioAdapter
    {
        void PlayRouteRevealAudio(SessionOperationalRouteCommand command);
    }
}

