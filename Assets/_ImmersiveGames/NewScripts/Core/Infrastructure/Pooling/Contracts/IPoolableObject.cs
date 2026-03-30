namespace _ImmersiveGames.NewScripts.Core.Infrastructure.Pooling.Contracts
{
    /// <summary>
    /// Optional lifecycle hooks for pooled objects in the canonical pooling module.
    /// </summary>
    public interface IPoolableObject
    {
        void OnPoolCreated();
        void OnPoolRent();
        void OnPoolReturn();
        void OnPoolDestroyed();
    }
}
