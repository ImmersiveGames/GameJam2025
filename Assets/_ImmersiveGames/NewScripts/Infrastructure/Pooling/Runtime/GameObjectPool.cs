using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Config;

namespace _ImmersiveGames.NewScripts.Infrastructure.Pooling.Runtime
{
    /// <summary>
    /// Runtime placeholder for the pool core. Package B implements operational behavior.
    /// </summary>
    public sealed class GameObjectPool
    {
        public GameObjectPool(PoolDefinitionAsset definition)
        {
            Definition = definition;
        }

        public PoolDefinitionAsset Definition { get; }
    }
}
