using System;

namespace _ImmersiveGames.NewProject.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Representa uma entrada de registro no provider.
    /// </summary>
    public sealed class ServiceDescriptor
    {
        public Type ServiceType { get; }
        public ServiceScope Scope { get; }
        public Func<IScopedServiceProvider, object> Factory { get; }

        public ServiceDescriptor(Type serviceType, ServiceScope scope, Func<IScopedServiceProvider, object> factory)
        {
            ServiceType = serviceType;
            Scope = scope;
            Factory = factory;
        }
    }
}
