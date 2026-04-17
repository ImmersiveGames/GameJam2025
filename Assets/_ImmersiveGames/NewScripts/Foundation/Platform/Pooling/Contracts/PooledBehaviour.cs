using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts
{
    /// <summary>
    /// Optional base behaviour with predictable, domain-agnostic pool lifecycle state.
    /// </summary>
    public abstract class PooledBehaviour : MonoBehaviour, IPoolableObject
    {
        public bool IsCurrentlyRented { get; private set; }
        public int RentCount { get; private set; }

        public virtual void OnPoolCreated()
        {
            IsCurrentlyRented = false;
            OnAfterPoolCreated();
        }

        public virtual void OnPoolRent()
        {
            IsCurrentlyRented = true;
            RentCount++;
            OnAfterPoolRent();
        }

        public virtual void OnPoolReturn()
        {
            IsCurrentlyRented = false;
            OnAfterPoolReturn();
        }

        public virtual void OnPoolDestroyed()
        {
            IsCurrentlyRented = false;
            OnAfterPoolDestroyed();
        }

        protected virtual void OnAfterPoolCreated() { }
        protected virtual void OnAfterPoolRent() { }
        protected virtual void OnAfterPoolReturn() { }
        protected virtual void OnAfterPoolDestroyed() { }
    }
}

