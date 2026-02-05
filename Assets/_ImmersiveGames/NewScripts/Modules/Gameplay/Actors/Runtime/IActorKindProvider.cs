namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Runtime
{
    /// <summary>
    /// Contrato complementar para expor o Kind de um ator sem alterar IActor.
    /// </summary>
    public interface IActorKindProvider
    {
        ActorKind Kind { get; }
    }
}
