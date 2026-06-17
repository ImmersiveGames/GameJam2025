using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using PlayerSessionParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.SessionParticipationContext;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityParticipationRuntimeState
    {
        private ActivityParticipationContext _currentParticipationContext;
        private PlayerSessionParticipationContext _currentSessionParticipationContext;

        public ActivityParticipationContext CurrentParticipationContext => _currentParticipationContext;
        public PlayerSessionParticipationContext CurrentSessionParticipationContext => _currentSessionParticipationContext;

        public void StoreCurrentParticipationContext(ActivityParticipationContext context)
        {
            _currentParticipationContext = context;
        }

        public void StoreCurrentSessionParticipationContext(PlayerSessionParticipationContext context)
        {
            _currentSessionParticipationContext = context;
        }

        public void ClearCurrentParticipationContext()
        {
            _currentParticipationContext = default;
        }

        public void ClearCurrentSessionParticipationContext()
        {
            _currentSessionParticipationContext = default;
        }
    }
}
