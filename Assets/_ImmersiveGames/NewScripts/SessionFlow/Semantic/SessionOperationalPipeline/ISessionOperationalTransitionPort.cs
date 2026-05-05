namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    public interface ISessionOperationalTransitionPort
    {
        void RequestRouteTransition(RequestRouteTransitionCommand command);
    }
}
