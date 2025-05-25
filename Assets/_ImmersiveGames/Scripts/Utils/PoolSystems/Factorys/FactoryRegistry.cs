using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public static class FactoryRegistry
    {
        private static readonly Dictionary<FactoryType, IPoolableFactory> _factories = new Dictionary<FactoryType, IPoolableFactory>
        {
            { FactoryType.Default, new PoolableFactory() }
        };

        public static void RegisterFactory(FactoryType type, IPoolableFactory factory) => _factories[type] = factory;
        public static IPoolableFactory GetFactory(FactoryType type)
        {
            if (_factories.TryGetValue(type, out var factory)) return factory;
            DebugUtility.LogError(typeof(FactoryRegistry),$"Fábrica não encontrada para '{type}'.");
            return null;
        }
    }
    public enum FactoryType { Default, Custom }
}