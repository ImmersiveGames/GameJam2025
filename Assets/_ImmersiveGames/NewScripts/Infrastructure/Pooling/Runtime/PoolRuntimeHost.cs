using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Pooling.Runtime
{
    /// <summary>
    /// Runtime host placeholder for pooling lifecycle ownership.
    /// Package B will define concrete scene/runtime behavior.
    /// </summary>
    public sealed class PoolRuntimeHost
    {
        public PoolRuntimeHost(Transform root)
        {
            Root = root;
        }

        public Transform Root { get; }
    }
}
