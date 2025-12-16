using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewProject.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Representa a lista de registros a serem usados para construir providers.
    /// </summary>
    public sealed class ServiceCollection
    {
        private readonly List<ServiceDescriptor> _descriptors = new();

        public ServiceCollection RegisterSingleton<TService>(Func<IScopedServiceProvider, TService> factory, ServiceScope scope = ServiceScope.Global)
        {
            _descriptors.Add(new ServiceDescriptor(typeof(TService), scope, provider => factory(provider)));
            return this;
        }

        public IReadOnlyList<ServiceDescriptor> Descriptors => _descriptors;
    }
}
