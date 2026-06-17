using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts
{
    public readonly struct PoolScopeReleaseResult
    {
        public PoolScopeReleaseResult(
            PoolLifetimeScope scope,
            int releasedPoolCount,
            int activeObjectCountBeforeRelease,
            int inactiveObjectCountBeforeRelease,
            string reason)
        {
            Scope = scope;
            ReleasedPoolCount = releasedPoolCount < 0 ? 0 : releasedPoolCount;
            ActiveObjectCountBeforeRelease = activeObjectCountBeforeRelease < 0 ? 0 : activeObjectCountBeforeRelease;
            InactiveObjectCountBeforeRelease = inactiveObjectCountBeforeRelease < 0 ? 0 : inactiveObjectCountBeforeRelease;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public PoolLifetimeScope Scope { get; }
        public int ReleasedPoolCount { get; }
        public int ActiveObjectCountBeforeRelease { get; }
        public int InactiveObjectCountBeforeRelease { get; }
        public string Reason { get; }
        public bool ReleasedAny => ReleasedPoolCount > 0;
    }

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
        PoolScopeReleaseResult ReleasePoolsForScope(PoolLifetimeScope scope);
        void Shutdown();
    }
}
