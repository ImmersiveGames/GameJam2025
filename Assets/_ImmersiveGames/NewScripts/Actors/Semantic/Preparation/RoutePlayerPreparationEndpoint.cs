using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Actors.Semantic.Preparation
{
    public interface IRoutePlayerPreparationEndpoint
    {
        PlayerPreparationResult Execute(
            PlayerPreparationPlan plan,
            IReadOnlyList<PlayerMaterializationRecord> materializationRecords = null);
    }

    public sealed class RoutePlayerPreparationEndpoint : IRoutePlayerPreparationEndpoint
    {
        public PlayerPreparationResult Execute(
            PlayerPreparationPlan plan,
            IReadOnlyList<PlayerMaterializationRecord> materializationRecords = null)
        {
            if (!plan.IsValid)
            {
                throw new InvalidOperationException("PlayerPreparationPlan is invalid.");
            }

            return PlayerPreparationStage.Execute(plan, materializationRecords);
        }
    }
}
