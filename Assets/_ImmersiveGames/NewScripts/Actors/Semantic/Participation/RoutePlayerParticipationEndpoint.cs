using System;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.Actors.Semantic.Participation
{
    public interface IRoutePlayerParticipationEndpoint
    {
        PlayerParticipationResult Execute(
            PlayerParticipationPlan plan,
            IReadOnlyList<PlayerMaterializationRecord> materializationRecords = null);
    }

    public sealed class RoutePlayerParticipationEndpoint : IRoutePlayerParticipationEndpoint
    {
        public PlayerParticipationResult Execute(
            PlayerParticipationPlan plan,
            IReadOnlyList<PlayerMaterializationRecord> materializationRecords = null)
        {
            if (!plan.IsValid)
            {
                throw new InvalidOperationException("PlayerParticipationPlan is invalid.");
            }

            return PlayerParticipationStage.Execute(plan, materializationRecords);
        }
    }
}
