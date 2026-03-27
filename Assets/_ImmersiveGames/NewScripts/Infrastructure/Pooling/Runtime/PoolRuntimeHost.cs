using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Pooling.Runtime
{
    /// <summary>
    /// Runtime owner for pooled objects of a canonical pool definition.
    /// </summary>
    public sealed class PoolRuntimeHost
    {
        public PoolRuntimeHost(string hostName, Transform globalRoot)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new ArgumentException("hostName is required.", nameof(hostName));
            }

            var hostObject = new GameObject(hostName);
            Root = hostObject.transform;
            Root.SetParent(globalRoot, false);

            var availableObject = new GameObject("Available");
            AvailableRoot = availableObject.transform;
            AvailableRoot.SetParent(Root, false);
        }

        public Transform Root { get; }
        public Transform AvailableRoot { get; }

        public void AttachAsAvailable(GameObject instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            instance.transform.SetParent(AvailableRoot, false);
        }

        public void AttachAsRented(GameObject instance, Transform parent)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            instance.transform.SetParent(parent, false);
        }

        public void Cleanup()
        {
            if (Root != null)
            {
                UnityEngine.Object.Destroy(Root.gameObject);
            }
        }
    }
}
