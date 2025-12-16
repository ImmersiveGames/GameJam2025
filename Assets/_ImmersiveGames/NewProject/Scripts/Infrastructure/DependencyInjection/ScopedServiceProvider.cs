using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewProject.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Provider hierárquico simples para os escopos Global/Scene/Actor.
    /// </summary>
    public sealed class ScopedServiceProvider : IScopedServiceProvider
    {
        private readonly IReadOnlyList<ServiceDescriptor> _descriptors;
        private readonly Dictionary<Type, object> _cache = new();

        public ServiceScope Scope { get; }
        public string ActorId { get; }
        public ScopedServiceProvider Parent { get; }

        public ScopedServiceProvider(IReadOnlyList<ServiceDescriptor> descriptors, ServiceScope scope, ScopedServiceProvider parent, string actorId)
        {
            _descriptors = descriptors;
            Scope = scope;
            Parent = parent;
            ActorId = actorId;
        }

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            if (_cache.TryGetValue(type, out var instance))
            {
                return instance;
            }

            foreach (var descriptor in _descriptors)
            {
                if (descriptor.ServiceType == type && descriptor.Scope == Scope)
                {
                    var created = descriptor.Factory(this);
                    _cache[type] = created;
                    return created;
                }
            }

            if (Parent != null)
            {
                return Parent.Resolve(type);
            }

            throw new InvalidOperationException($"Service of type {type.Name} not registered for scope {Scope}.");
        }

        public IScopedServiceProvider CreateSceneScope()
        {
            return new ScopedServiceProvider(_descriptors, ServiceScope.Scene, this, null);
        }

        public IScopedServiceProvider CreateActorScope(string actorId)
        {
            return new ScopedServiceProvider(_descriptors, ServiceScope.Actor, this, actorId);
        }
    }
}
