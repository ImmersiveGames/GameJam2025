using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Semantic
{
    public interface IActorsIdentityRolePolicy
    {
        ActorIdentityRoleSet Resolve(ActorsEnsembleInput input);
    }

    public interface IActorsEnsembleService
    {
        ActorsEnsembleSnapshot Current { get; }
        bool TryGetCurrent(out ActorsEnsembleSnapshot snapshot);
        ActorsEnsembleSnapshot Refresh();
        void Clear(string reason = null);
    }

    public interface IActorsPresenceService
    {
        ActorsPresenceSnapshot Current { get; }
        bool TryGetCurrent(out ActorsPresenceSnapshot snapshot);
        ActorsPresenceSnapshot Refresh();
        void Clear(string reason = null);
    }

    public interface IActorsRegistryBoundary
    {
        bool TryGetByAxisActorId(AxisActorId axisActorId, out ActorsRegistryEntry entry);
        bool TryGetAll(List<ActorsRegistryEntry> target);
    }

    public interface IActorsMaterializationPlanService
    {
        ActorsMaterializationPlanSnapshot Current { get; }
        bool TryGetCurrent(out ActorsMaterializationPlanSnapshot snapshot);
        ActorsMaterializationPlanSnapshot Refresh();
        void Clear(string reason = null);
    }

    public interface IActorsMaterializationExecutionPolicyService
    {
        ActorsMaterializationExecutionSnapshot Current { get; }
        bool TryGetCurrent(out ActorsMaterializationExecutionSnapshot snapshot);
        ActorsMaterializationExecutionSnapshot Refresh();
        void Clear(string reason = null);
    }
}
