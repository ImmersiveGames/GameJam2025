using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public static class FactoryRegistry
    {
        private static readonly Dictionary<FactoryType, IPoolableFactory> _factories = new Dictionary<FactoryType, IPoolableFactory>
        {
            { FactoryType.Default, new DefaultPoolableFactory() },
            { FactoryType.Custom, new CustomPoolableFactory() }
        };

        public static IPoolableFactory GetFactory(FactoryType type)
        {
            if (_factories.TryGetValue(type, out var factory))
            {
                return factory;
            }
            Debug.LogError($"No factory registered for FactoryType '{type}'");
            return null;
        }
    }
}