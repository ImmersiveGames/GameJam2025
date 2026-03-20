using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Pooling.Runtime
{
    /// <summary>
    /// Runtime placeholder for optional auto-return handling.
    /// Package B will implement the timing behavior.
    /// </summary>
    public sealed class PoolAutoReturnTracker
    {
        public void Track(PoolDefinitionAsset definition, GameObject instance) { }
        public void Untrack(GameObject instance) { }
        public void Clear() { }
    }
}
