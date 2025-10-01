using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bridges
{

    public class EntityResourceBridge : MonoBehaviour
    {
        [SerializeField] private ResourceInstanceConfig[] resourceInstances = Array.Empty<ResourceInstanceConfig>();

        private IActor _actor;
        private ResourceSystemService _service;
        private IActorResourceOrchestrator _orchestrator;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                DebugUtility.LogWarning<ActorResourceOrchestratorService>($"No IActor found on {gameObject.name}. Disabling.");
                enabled = false;
                return;
            }

            _service = new ResourceSystemService(_actor.ActorId, resourceInstances);
            DependencyManager.Instance.RegisterForObject(_actor.ActorId, _service);

            if (!DependencyManager.Instance.TryGetGlobal(out _orchestrator))
            {
                var orchestrator = new ActorResourceOrchestratorService();
                DependencyManager.Instance.RegisterGlobal<IActorResourceOrchestrator>(orchestrator);
                _orchestrator = orchestrator;
            }

            _orchestrator.RegisterActor(_service);
        }

        private void OnDestroy()
        {
            _orchestrator?.UnregisterActor(_actor.ActorId);

            DependencyManager.Instance.ClearObjectServices(_actor.ActorId);
            _service?.Dispose();
            _service = null;
        }

        [ContextMenu("Debug Print Resources")]
        private void DebugPrintResources()
        {
            IReadOnlyDictionary<ResourceType, IResourceValue> all = _service.GetAll();
            DebugUtility.LogVerbose<EntityResourceBridge>($"{_actor.ActorId} resources count: {all.Count}");
            foreach (KeyValuePair<ResourceType, IResourceValue> kv in all)
                DebugUtility.LogVerbose<EntityResourceBridge>($"  - {kv.Key}: {kv.Value.GetCurrentValue()}/{kv.Value.GetMaxValue()}");
        }

        public ResourceSystemService GetService() => _service;
    }
}