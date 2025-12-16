namespace _ImmersiveGames.NewProject.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Constrói o provider raiz a partir dos registros disponíveis.
    /// </summary>
    public static class ServiceBuilder
    {
        public static ScopedServiceProvider Build(ServiceCollection collection)
        {
            return new ScopedServiceProvider(collection.Descriptors, ServiceScope.Global, null, null);
        }
    }
}
