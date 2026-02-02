namespace _ImmersiveGames.NewScripts.Runtime.Actors
{
    /// <summary>
    /// Contrato complementar para expor o Kind de um ator sem alterar IActor.
    /// </summary>
    public interface IActorKindProvider
    {
        ActorKind Kind { get; }
    }
}
