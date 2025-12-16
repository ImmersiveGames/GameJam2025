namespace _ImmersiveGames.NewProject.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Escopos suportados pelo provider: Global, Scene ou Actor.
    /// </summary>
    public enum ServiceScope
    {
        Global = 0,
        Scene = 1,
        Actor = 2
    }
}
