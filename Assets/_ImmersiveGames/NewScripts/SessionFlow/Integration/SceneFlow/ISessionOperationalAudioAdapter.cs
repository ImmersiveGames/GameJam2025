using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    public interface ISessionOperationalAudioAdapter
    {
        void PlayRouteRevealAudio(SessionOperationalRouteCommand command);
    }
}
