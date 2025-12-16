using System;

namespace _ImmersiveGames.NewProject.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Provider de serviços com suporte a escopos hierárquicos.
    /// </summary>
    public interface IScopedServiceProvider
    {
        T Resolve<T>();
        object Resolve(Type type);
        IScopedServiceProvider CreateSceneScope();
        IScopedServiceProvider CreateActorScope(string actorId);
    }
}
