using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityParticipationRuntimeState
    {
        private ActivityParticipationContext _currentParticipationContext;

        public ActivityParticipationContext CurrentParticipationContext => _currentParticipationContext;

        public void StoreCurrentParticipationContext(ActivityParticipationContext context)
        {
            _currentParticipationContext = context;
        }

        public void ClearCurrentParticipationContext()
        {
            _currentParticipationContext = default;
        }
    }
}
