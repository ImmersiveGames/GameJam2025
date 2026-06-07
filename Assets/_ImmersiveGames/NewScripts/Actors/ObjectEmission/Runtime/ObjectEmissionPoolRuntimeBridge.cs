using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ObjectEmissionPoolRuntimeBridge : MonoBehaviour
    {
        private const string HostObjectName = "NewScripts_ObjectEmissionPoolRuntime";

        private static ObjectEmissionPoolRuntimeBridge _instance;

        private IPoolService _poolService;

        public static ObjectEmissionPoolRuntimeBridge EnsureCreated(IPoolService poolService)
        {
            if (poolService == null)
            {
                throw new ArgumentNullException(nameof(poolService), "[FATAL][Config][ObjectEmission] IPoolService obrigatorio ausente para criar o runtime bridge.");
            }

            ObjectEmissionPoolRuntimeBridge[] existingHosts = FindObjectsByType<ObjectEmissionPoolRuntimeBridge>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (existingHosts != null && existingHosts.Length > 0)
            {
                ObjectEmissionPoolRuntimeBridge host = existingHosts[0];
                host.Initialize(poolService);
                DontDestroyOnLoad(host.gameObject);

                if (existingHosts.Length > 1)
                {
                    for (int i = 1; i < existingHosts.Length; i++)
                    {
                        ObjectEmissionPoolRuntimeBridge extraHost = existingHosts[i];
                        if (extraHost == null || extraHost == host)
                        {
                            continue;
                        }

                        Destroy(extraHost.gameObject);
                    }

                    DebugUtility.LogWarning(typeof(ObjectEmissionPoolRuntimeBridge),
                        $"[OBS][ObjectEmission][Bridge] multiple runtime hosts detected. kept='{host.name}' removed='{existingHosts.Length - 1}'.");
                }

                _instance = host;

                DebugUtility.LogVerbose(
                    typeof(ObjectEmissionPoolRuntimeBridge),
                    $"[OBS][ObjectEmission][Bridge] runtime host reused host='{host.name}'.",
                    DebugUtility.Colors.Info);
                return host;
            }

            GameObject runtimeObject = new(HostObjectName);
            ObjectEmissionPoolRuntimeBridge createdHost = runtimeObject.AddComponent<ObjectEmissionPoolRuntimeBridge>();
            createdHost.Initialize(poolService);
            DontDestroyOnLoad(runtimeObject);
            _instance = createdHost;

            DebugUtility.LogVerbose(
                typeof(ObjectEmissionPoolRuntimeBridge),
                $"[OBS][ObjectEmission][Bridge] runtime host created host='{runtimeObject.name}'.",
                DebugUtility.Colors.Info);

            return createdHost;
        }

        public static bool TryAttachEndpoint(ActorObjectEmitterEndpoint endpoint)
        {
            if (endpoint == null)
            {
                return false;
            }

            if (_instance == null || _instance._poolService == null)
            {
                return false;
            }

            _instance.AttachEndpoint(endpoint);
            return true;
        }

        private void Awake()
        {
            _instance = this;
        }

        private void OnEnable()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Initialize(IPoolService poolService)
        {
            _poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));
        }

        private void AttachEndpoint(ActorObjectEmitterEndpoint endpoint)
        {
            endpoint.PoolAdapter.AttachPoolService(_poolService);

            DebugUtility.Log(
                typeof(ObjectEmissionPoolRuntimeBridge),
                $"[OBS][ObjectEmission][Bridge] event='ObjectEmissionPoolServiceAttached' endpointId='{endpoint.EndpointId}' endpointType='{endpoint.GetType().Name}' adapterType='{endpoint.PoolAdapter.GetType().Name}'.",
                DebugUtility.Colors.Success);
        }
    }
}
