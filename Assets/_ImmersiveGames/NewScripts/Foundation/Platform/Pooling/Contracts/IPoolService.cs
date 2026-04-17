using ImmersiveGames.GameJam2025.Infrastructure.Pooling.Config;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Infrastructure.Pooling.Contracts
{
    /// <summary>
    /// Canonical global pooling service contract.
    /// Identity is always the PoolDefinitionAsset reference.
    /// </summary>
    public interface IPoolService
    {
        bool IsBootstrapped { get; }

        void EnsureRegistered(PoolDefinitionAsset definition);
        void Prewarm(PoolDefinitionAsset definition);
        GameObject Rent(PoolDefinitionAsset definition, Transform parent = null);
        void Return(PoolDefinitionAsset definition, GameObject instance);
        void Shutdown();
    }
}

