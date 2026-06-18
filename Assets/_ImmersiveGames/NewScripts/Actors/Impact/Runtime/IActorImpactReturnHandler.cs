namespace _ImmersiveGames.NewScripts.Actors.Impact.Runtime
{
    public interface IActorImpactReturnHandler
    {
        bool IsConfigured { get; }

        bool TryRequestReturn(
            ActorImpactResult impactResult,
            string source,
            string reason,
            out string outcomeReason);
    }
}
