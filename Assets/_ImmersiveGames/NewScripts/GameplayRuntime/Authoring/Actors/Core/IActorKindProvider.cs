namespace _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core
{
    /// <summary>
    /// Contrato complementar para expor o Kind de um ator sem alterar IActor.
    /// </summary>
    public interface IActorKindProvider
    {
        ActorKind Kind { get; }
    }
}

