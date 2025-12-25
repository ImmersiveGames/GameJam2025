namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Spawn
{
    /// <summary>
    /// Tipos conhecidos de serviços de spawn do mundo.
    /// Mantido explícito para evitar reflection e facilitar expansão futura.
    /// </summary>
    public enum WorldSpawnServiceKind
    {
        DummyActor = 0,
        Player = 1
    }
}
