using _ImmersiveGames.NewScripts.Actors.Damage.Runtime;

namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    public interface IActorImpactDamageApplicationAdapter
    {
        bool IsConfigured { get; }

        bool TryApplyDamage(
            ActorImpactResult impactResult,
            float rawDamageAmount,
            string source,
            string reason,
            out ActorDamageSourceResult result);
    }
}
