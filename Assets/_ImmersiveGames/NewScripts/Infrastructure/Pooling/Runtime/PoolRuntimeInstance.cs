using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Pooling.Runtime
{
    /// <summary>
    /// Runtime placeholder for binding pooled instances to canonical definitions.
    /// </summary>
    public sealed class PoolRuntimeInstance
    {
        public PoolRuntimeInstance(PoolDefinitionAsset definition, GameObject instance)
        {
            Definition = definition;
            Instance = instance;
        }

        public PoolDefinitionAsset Definition { get; }
        public GameObject Instance { get; }
    }
}
