using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Pooling.Contracts
{
    /// <summary>
    /// Optional base behaviour with no-op pool lifecycle hooks.
    /// </summary>
    public abstract class PooledBehaviour : MonoBehaviour, IPoolableObject
    {
        public virtual void OnPoolCreated() { }
        public virtual void OnPoolRent() { }
        public virtual void OnPoolReturn() { }
        public virtual void OnPoolDestroyed() { }
    }
}
