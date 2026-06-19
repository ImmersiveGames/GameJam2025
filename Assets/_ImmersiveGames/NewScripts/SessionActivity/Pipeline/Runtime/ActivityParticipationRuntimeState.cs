using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using PlayerSessionParticipationContext = _ImmersiveGames.NewScripts.PlayerParticipation.Contracts.SessionParticipationContext;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityParticipationRuntimeState
    {

        public ActivityParticipationContext CurrentParticipationContext { get; private set; }
        public PlayerSessionParticipationContext CurrentSessionParticipationContext { get; private set; }

        public void StoreCurrentParticipationContext(ActivityParticipationContext context)
        {
            CurrentParticipationContext = context;
        }

        public void StoreCurrentSessionParticipationContext(PlayerSessionParticipationContext context)
        {
            CurrentSessionParticipationContext = context;
        }

        public void ClearCurrentParticipationContext()
        {
            CurrentParticipationContext = default;
        }

        public void ClearCurrentSessionParticipationContext()
        {
            CurrentSessionParticipationContext = default;
        }
    }
}
