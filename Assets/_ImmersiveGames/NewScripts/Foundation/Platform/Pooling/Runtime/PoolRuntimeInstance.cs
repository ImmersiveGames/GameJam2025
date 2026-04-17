using ImmersiveGames.GameJam2025.Infrastructure.Pooling.Config;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Infrastructure.Pooling.Runtime
{
    /// <summary>
    /// Runtime binding between a pooled instance and its canonical source pool.
    /// </summary>
    public sealed class PoolRuntimeInstance
    {
        public PoolRuntimeInstance(PoolDefinitionAsset definition, GameObject instance, GameObjectPool sourcePool)
        {
            Definition = definition;
            Instance = instance;
            SourcePool = sourcePool;
        }

        public PoolDefinitionAsset Definition { get; }
        public GameObject Instance { get; }
        public GameObjectPool SourcePool { get; }
        public bool IsRented { get; private set; }
        public int RentCount { get; private set; }

        public void MarkRented()
        {
            IsRented = true;
            RentCount++;
        }

        public void MarkReturned()
        {
            IsRented = false;
        }
    }
}

