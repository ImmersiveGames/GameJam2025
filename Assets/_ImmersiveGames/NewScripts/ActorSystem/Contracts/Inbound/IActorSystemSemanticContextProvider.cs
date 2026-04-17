namespace _ImmersiveGames.NewScripts.ActorSystem.Contracts.Inbound
{
    /// <summary>
    /// Inbound port for semantic context owned by SessionFlow.
    /// </summary>
    public interface IActorSystemSemanticContextProvider
    {
        bool TryGetCurrent(out ActorSystemSemanticContext context);
    }
}